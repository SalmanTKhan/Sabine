using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sabine.Shared.Const;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// One quest's persistent state. Mirrors the eAthena quest_log row
	/// shape (quest_id / state / time / count1-3) but stays in-memory;
	/// persistence rides character vars for v1, so adding fields here
	/// doesn't require a schema migration.
	/// </summary>
	public class QuestEntry
	{
		public int Id { get; set; }
		public QuestState State { get; set; }

		/// <summary>UTC timestamp the quest was accepted; default(DateTime) when not set.</summary>
		public DateTime AcceptedAtUtc { get; set; }

		/// <summary>
		/// Objective progress counts. eAthena allots three slots
		/// (count1 / count2 / count3) per quest_log row — typically
		/// "mob kills per target type". Sabine keeps the same shape so
		/// scripts that target eA semantics translate cleanly.
		/// </summary>
		public int[] Counts { get; set; } = new int[3];
	}

	/// <summary>
	/// Tracks the state of a player's quests. Each quest is a
	/// <see cref="QuestEntry"/>; the collection is materialised from
	/// the player's permanent var container on load and re-serialised on
	/// save. A future SQL <c>quest_log</c> table can swap in by replacing
	/// <see cref="LoadAll"/> / <see cref="SaveAll"/>; the script-facing
	/// surface (Add / Complete / Erase / GetStatus / GetCount /
	/// IncrementCount / List*) stays unchanged.
	/// </summary>
	public class QuestList
	{
		private const string KeyPrefix = "__quest_state_";
		private const string ListKey = "__quest_list";

		private readonly Dictionary<int, QuestEntry> _entries = new();
		private bool _loaded;

		public PlayerCharacter Character { get; }

		public QuestList(PlayerCharacter character)
		{
			this.Character = character;
		}

		private void EnsureLoaded()
		{
			if (_loaded) return;
			this.LoadAll();
			_loaded = true;
		}

		// ---- Read API -----------------------------------------------------

		public QuestState GetStatus(int questId)
		{
			this.EnsureLoaded();
			return _entries.TryGetValue(questId, out var e) ? e.State : QuestState.Inactive;
		}

		public bool IsActive(int questId) => this.GetStatus(questId) == QuestState.Active;
		public bool IsComplete(int questId) => this.GetStatus(questId) == QuestState.Complete;

		/// <summary>Returns the current count for an objective slot (0-2). 0 when unset.</summary>
		public int GetCount(int questId, int slot)
		{
			this.EnsureLoaded();
			if (slot < 0 || slot > 2) return 0;
			return _entries.TryGetValue(questId, out var e) ? e.Counts[slot] : 0;
		}

		/// <summary>UTC timestamp the quest was accepted. <c>default(DateTime)</c> when not set.</summary>
		public DateTime GetAcceptedAt(int questId)
		{
			this.EnsureLoaded();
			return _entries.TryGetValue(questId, out var e) ? e.AcceptedAtUtc : default;
		}

		/// <summary>All entries, including erased / completed; useful for the quest log UI.</summary>
		public IReadOnlyList<QuestEntry> ListAll()
		{
			this.EnsureLoaded();
			return _entries.Values.ToList();
		}

		/// <summary>All currently-active entries — the typical UI rendering target.</summary>
		public IReadOnlyList<QuestEntry> ListActive()
		{
			this.EnsureLoaded();
			return _entries.Values.Where(e => e.State == QuestState.Active).ToList();
		}

		// ---- Mutation API -------------------------------------------------

		public void Add(int questId)
		{
			this.EnsureLoaded();
			if (!_entries.TryGetValue(questId, out var e))
				_entries[questId] = e = new QuestEntry { Id = questId };
			e.State = QuestState.Active;
			e.AcceptedAtUtc = DateTime.UtcNow;
			this.SaveAll();
		}

		public void Complete(int questId)
		{
			this.EnsureLoaded();
			if (!_entries.TryGetValue(questId, out var e))
				_entries[questId] = e = new QuestEntry { Id = questId };
			e.State = QuestState.Complete;
			this.SaveAll();
		}

		public void Erase(int questId)
		{
			this.EnsureLoaded();
			_entries.Remove(questId);
			this.SaveAll();
		}

		/// <summary>Increments an objective slot. Caps at int.MaxValue / 2 to leave headroom.</summary>
		public void IncrementCount(int questId, int slot, int delta = 1)
		{
			this.EnsureLoaded();
			if (slot < 0 || slot > 2) return;
			if (!_entries.TryGetValue(questId, out var e))
				_entries[questId] = e = new QuestEntry { Id = questId, State = QuestState.Active };
			e.Counts[slot] = Math.Min(int.MaxValue / 2, e.Counts[slot] + delta);
			this.SaveAll();
		}

		// ---- Persistence (Vars-backed; swap to SQL when consumers warrant) -

		private void LoadAll()
		{
			_entries.Clear();
			var packed = this.Character.Vars.Perm.GetString(ListKey, "");
			if (string.IsNullOrEmpty(packed)) return;

			foreach (var part in packed.Split('|'))
			{
				if (string.IsNullOrEmpty(part)) continue;
				var fields = part.Split(',');
				if (fields.Length < 6) continue;
				if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)) continue;
				if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var state)) continue;
				long.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var acceptedTicks);
				int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var c0);
				int.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var c1);
				int.TryParse(fields[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var c2);

				_entries[id] = new QuestEntry
				{
					Id = id,
					State = (QuestState)state,
					AcceptedAtUtc = acceptedTicks > 0 ? new DateTime(acceptedTicks, DateTimeKind.Utc) : default,
					Counts = new[] { c0, c1, c2 },
				};
			}
		}

		private void SaveAll()
		{
			if (_entries.Count == 0)
			{
				this.Character.Vars.Perm.Set(ListKey, "");
				return;
			}

			var parts = _entries.Values.Select(e =>
				string.Join(",",
					e.Id.ToString(CultureInfo.InvariantCulture),
					((int)e.State).ToString(CultureInfo.InvariantCulture),
					e.AcceptedAtUtc.Ticks.ToString(CultureInfo.InvariantCulture),
					e.Counts[0].ToString(CultureInfo.InvariantCulture),
					e.Counts[1].ToString(CultureInfo.InvariantCulture),
					e.Counts[2].ToString(CultureInfo.InvariantCulture)));

			this.Character.Vars.Perm.Set(ListKey, string.Join("|", parts));
		}
	}
}
