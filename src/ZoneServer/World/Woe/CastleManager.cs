using System;
using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.Scripting;
using Yggdrasil.Logging;

namespace Sabine.Zone.World.Woe
{
	/// <summary>
	/// Active castle registry. State is loaded from
	/// <c>castle_state</c> at startup and persisted on every
	/// <see cref="SetData"/> mutation. Static template data
	/// (display name, on-break event) lives in
	/// <c>ZoneServer.Instance.Data.Castles</c>.
	/// </summary>
	public class CastleManager
	{
		private readonly object _syncLock = new();
		private readonly Dictionary<string, Castle> _castles =
			new(StringComparer.OrdinalIgnoreCase);

		// Per-castle OnRecvCastle hooks. eAthena scripts register
		// these via the statement form `GetCastleData "<map>",0,"<event>"`
		// — semantically the receive-castle event handler. We
		// dispatch via `ScriptEvents.RaiseAsync` when the owner
		// guild changes (see `SetData` below).
		private readonly Dictionary<string, string> _onRecvCastleHooks =
			new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Loads every persisted castle row at startup. Castles
		/// listed in `system/data/castles.txt` but without a row
		/// get a default (unoccupied) runtime entry on first
		/// access — see <see cref="GetOrCreate"/>.
		/// </summary>
		public void LoadAll()
		{
			lock (_syncLock)
			{
				_castles.Clear();
				var rows = ZoneServer.Instance.Database.LoadAllCastles();
				var templates = ZoneServer.Instance.Data.Castles.Entries;
				foreach (var row in rows)
				{
					if (string.IsNullOrEmpty(row.MapStringId))
						continue;
					if (!templates.TryGetValue(row.MapStringId, out var template))
					{
						Log.Warning("CastleManager: stored castle '{0}' has no matching template; skipping.",
							row.MapStringId);
						continue;
					}
					var castle = new Castle(template)
					{
						OwnerGuildId = row.OwnerGuildId,
						Economy = row.Economy,
						Defense = row.Defense,
						TriggerE = row.TriggerE,
						TriggerD = row.TriggerD,
						TreasureFlags = row.TreasureFlags,
					};
					_castles[castle.MapStringId] = castle;
				}
				Log.Info("CastleManager: loaded {0} castle row(s).", _castles.Count);
			}
		}

		/// <summary>
		/// Returns the runtime <see cref="Castle"/> for the given
		/// map id, creating an empty entry on first access if the
		/// map matches a template. Returns null when the map is
		/// not a registered castle.
		/// </summary>
		public Castle GetOrCreate(string mapStringId)
		{
			if (string.IsNullOrEmpty(mapStringId))
				return null;
			lock (_syncLock)
			{
				if (_castles.TryGetValue(mapStringId, out var existing))
					return existing;
				if (!ZoneServer.Instance.Data.Castles.Entries.TryGetValue(
						mapStringId, out var template))
					return null;
				var castle = new Castle(template);
				_castles[mapStringId] = castle;
				return castle;
			}
		}

		/// <summary>
		/// Resolves a castle by map id without auto-creating an
		/// empty entry. Useful for read-only paths that want to
		/// distinguish "untouched" from "explicitly-empty" castles.
		/// </summary>
		public Castle Get(string mapStringId)
		{
			lock (_syncLock)
				return _castles.TryGetValue(mapStringId, out var c) ? c : null;
		}

		/// <summary>
		/// Reads <see cref="Castle.GetData"/>. Returns 0 when the
		/// map isn't a registered castle.
		/// </summary>
		public int GetData(string mapStringId, int dataType)
		{
			var castle = this.GetOrCreate(mapStringId);
			return castle?.GetData(dataType) ?? 0;
		}

		/// <summary>
		/// Writes <see cref="Castle.SetData"/> and persists the
		/// updated row. Returns true on success. When the owner
		/// guild (data type 1) changes from one value to another,
		/// the registered <c>OnRecvCastle</c> hook for the map (if
		/// any) is fired so the script's per-castle bookkeeping
		/// runs.
		/// </summary>
		public bool SetData(string mapStringId, int dataType, int value)
		{
			var castle = this.GetOrCreate(mapStringId);
			if (castle == null)
				return false;
			var prevOwner = castle.OwnerGuildId;
			if (!castle.SetData(dataType, value))
				return false;
			ZoneServer.Instance.Database.UpsertCastle(castle.ToRow());

			if (dataType == 1 && prevOwner != value)
				_ = this.RaiseOnRecvCastle(mapStringId);

			return true;
		}

		/// <summary>
		/// Registers the OnRecvCastle hook for a castle. Called by
		/// the converted scripts via the statement-form
		/// <c>GetCastleData "&lt;map&gt;", 0, "&lt;event&gt;"</c>.
		/// The hook is fired by <see cref="SetData"/> whenever the
		/// owner guild id transitions to a new value.
		/// </summary>
		public void RegisterOnRecvCastle(string mapStringId, string eventName)
		{
			if (string.IsNullOrEmpty(mapStringId) || string.IsNullOrEmpty(eventName))
				return;
			lock (_syncLock)
				_onRecvCastleHooks[mapStringId] = eventName;
		}

		private async System.Threading.Tasks.Task RaiseOnRecvCastle(string mapStringId)
		{
			string hook;
			lock (_syncLock)
			{
				if (!_onRecvCastleHooks.TryGetValue(mapStringId, out hook))
					return;
			}
			try
			{
				await ScriptEvents.RaiseAsync(hook);
			}
			catch (Exception ex)
			{
				Log.Error("CastleManager.RaiseOnRecvCastle({0}): {1}", mapStringId, ex);
			}
		}

		/// <summary>
		/// Resolves a castle by template id. Linear scan; the castle
		/// table is small (≤30 entries) so the cost is negligible.
		/// Returns null when no castle with that id exists.
		/// </summary>
		public Castle GetById(int castleId)
		{
			lock (_syncLock)
				return _castles.Values.FirstOrDefault(c => c.Template.Id == castleId);
		}

		/// <summary>
		/// Transfers castle ownership to <paramref name="newGuildId"/>
		/// and persists the row. Mirrors eAthena's WoE on-break
		/// transition where the breaking guild becomes the new owner;
		/// resets daily investment / treasure flags so the new owner
		/// starts the day with a clean slate.
		/// </summary>
		public bool TransferOwner(int castleId, int newGuildId)
		{
			Castle castle;
			lock (_syncLock)
				castle = _castles.Values.FirstOrDefault(c => c.Template.Id == castleId);
			if (castle == null)
				return false;
			return this.TransferOwner(castle, newGuildId);
		}

		/// <summary>
		/// Castle-instance overload of <see cref="TransferOwner(int, int)"/>.
		/// </summary>
		public bool TransferOwner(Castle castle, int newGuildId)
		{
			if (castle == null)
				return false;

			var prevOwner = castle.OwnerGuildId;
			castle.OwnerGuildId = newGuildId;
			castle.TriggerE = 0;
			castle.TriggerD = 0;
			castle.TreasureFlags = 0;
			ZoneServer.Instance.Database.UpsertCastle(castle.ToRow());

			if (prevOwner != newGuildId)
				_ = this.RaiseOnRecvCastle(castle.MapStringId);

			return true;
		}

		/// <summary>
		/// Returns a snapshot of every active castle. Cosmetic /
		/// admin use; runtime queries should prefer
		/// <see cref="Get"/> / <see cref="GetData"/>.
		/// </summary>
		public IReadOnlyList<Castle> All
		{
			get { lock (_syncLock) return _castles.Values.ToArray(); }
		}
	}
}
