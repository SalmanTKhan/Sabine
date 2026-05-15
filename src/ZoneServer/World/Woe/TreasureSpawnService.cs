using System;
using System.Collections.Generic;
using System.Threading;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;
using Yggdrasil.Logging;

namespace Sabine.Zone.World.Woe
{
	/// <summary>
	/// Daily castle-treasure spawner. Polls on a 30-second timer and,
	/// when a castle's day-of-year bit hasn't yet been flipped on
	/// <see cref="Castle.TreasureFlags"/>, spawns N treasure NPCs on
	/// the castle map. The flag is then set so the same day doesn't
	/// double-spawn until the bit is cleared (e.g. on ownership
	/// transfer; see <see cref="CastleManager.TransferOwner"/>).
	/// </summary>
	/// <remarks>
	/// Mirrors eAthena's per-castle <c>OnAgitInit</c> + treasure
	/// spawn loop in <c>npc/guild/*.txt</c>. Real eA reads each
	/// treasure box's drop pool from a per-castle table; we use a
	/// minimal placeholder pool for now (TODO marker in the file).
	/// </remarks>
	public class TreasureSpawnService
	{
		// Per-castle drop tables (eAthena `npc/guild/<map>_data.txt`)
		// are still deferred; today every box rolls from the shared
		// TREASURE_BOX1..8 pool defined in monsters.txt. The data file
		// exposes 8 distinct ids so future per-castle table assignment
		// can wire one box id per castle without churning the spawner.
		private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

		// rA `mob_db.txt` ids 1324..1331; we rotate through them so each
		// spawn pass paints a mix on the map.
		private static readonly int[] TreasureBoxIds =
		{
			Sabine.Shared.Const.MonsterId.TREASURE_BOX1,
			Sabine.Shared.Const.MonsterId.TREASURE_BOX2,
			Sabine.Shared.Const.MonsterId.TREASURE_BOX3,
			Sabine.Shared.Const.MonsterId.TREASURE_BOX4,
			Sabine.Shared.Const.MonsterId.TREASURE_BOX5,
			Sabine.Shared.Const.MonsterId.TREASURE_BOX6,
			Sabine.Shared.Const.MonsterId.TREASURE_BOX7,
			Sabine.Shared.Const.MonsterId.TREASURE_BOX8,
		};

		private readonly object _syncLock = new();
		private readonly Dictionary<string, List<Monster>> _spawned =
			new(StringComparer.OrdinalIgnoreCase);
		private Timer _timer;
		private bool _attached;

		/// <summary>
		/// Wires to WoE end so the day-of-year cleanup runs on every
		/// agit close. Idempotent.
		/// </summary>
		public void Attach(WoeService woe)
		{
			if (_attached || woe == null)
				return;
			_attached = true;
			woe.Ended += this.OnWoeEnded;
		}

		/// <summary>Starts the 30-second poll timer. Idempotent.</summary>
		public void StartPolling()
		{
			lock (_syncLock)
			{
				if (_timer != null)
					return;
				_timer = new Timer(this.Tick, null, TimeSpan.Zero, PollInterval);
			}
		}

		/// <summary>Stops the poll timer. Used in tests / shutdown paths.</summary>
		public void StopPolling()
		{
			lock (_syncLock)
			{
				_timer?.Dispose();
				_timer = null;
			}
		}

		private void OnWoeEnded()
		{
			// Cleanup any lingering treasure mobs at WoE end so they
			// don't leak across agit windows.
			try
			{
				List<KeyValuePair<string, List<Monster>>> snapshot;
				lock (_syncLock)
				{
					snapshot = new List<KeyValuePair<string, List<Monster>>>(_spawned);
					_spawned.Clear();
				}
				foreach (var pair in snapshot)
				{
					foreach (var m in pair.Value)
					{
						try { m.Map?.RemoveNpc(m); }
						catch { /* ignore */ }
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("TreasureSpawnService.OnWoeEnded: {0}", ex);
			}
		}

		private void Tick(object _)
		{
			try
			{
				var castles = ZoneServer.Instance.World.Castles.All;
				if (castles == null || castles.Count == 0)
					return;

				var manager = ZoneServer.Instance.World.Castles;
				var maps = ZoneServer.Instance.World.Maps;
				var dayBit = DateTime.UtcNow.DayOfYear & 0x07; // pack 0..7 into the 8-bit slot

				foreach (var castle in castles)
				{
					// Treasures only matter once a guild owns the castle.
					if (castle.OwnerGuildId <= 0)
						continue;

					var bit = 1 << dayBit;
					if ((castle.TreasureFlags & bit) != 0)
						continue;

					if (!maps.TryGetByStringId(castle.MapStringId, out var map))
						continue;

					var count = Math.Clamp(4 + (castle.Economy / 4), 4, 16);
					var spawned = new List<Monster>();

					// 1-HP treasure boxes drop their inventory pool on the
					// first hit (see monsters.txt rows 1324..1331). The
					// box id comes from the castle's CastleData.TreasureBoxId
					// — that maps the castle to one of four regional drop
					// pools (ores / gems / orient / royal). Castles with
					// no assigned id fall back to the shared 1324..1331
					// rotation.
					var castleBoxId = castle.Template?.TreasureBoxId ?? 0;
					for (var i = 0; i < count; i++)
					{
						try
						{
							var boxId = castleBoxId > 0
								? castleBoxId
								: TreasureBoxIds[i % TreasureBoxIds.Length];
							var monster = new Monster((IdentityId)boxId);
							var pos = map.GetRandomWalkablePosition();
							monster.Warp(map.Id, pos);
							spawned.Add(monster);
						}
						catch (Exception ex)
						{
							Log.Warning("TreasureSpawnService: spawn failed on '{0}': {1}",
								castle.MapStringId, ex.Message);
						}
					}

					lock (_syncLock)
					{
						if (!_spawned.TryGetValue(castle.MapStringId, out var list))
							_spawned[castle.MapStringId] = list = new List<Monster>();
						list.AddRange(spawned);
					}

					// Persist the day bit so we don't re-spawn until
					// the day-of-year rolls.
					manager.SetData(castle.MapStringId, 10 + dayBit, 1);

					Log.Info("TreasureSpawnService: spawned {0} treasure(s) on '{1}'.",
						spawned.Count, castle.MapStringId);
				}
			}
			catch (Exception ex)
			{
				Log.Error("TreasureSpawnService.Tick: {0}", ex);
			}
		}
	}
}
