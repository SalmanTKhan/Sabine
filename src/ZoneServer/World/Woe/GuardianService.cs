using System;
using System.Collections.Generic;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;
using Yggdrasil.Logging;

namespace Sabine.Zone.World.Woe
{
	/// <summary>
	/// Spawns and despawns the per-castle guardian mobs that defend
	/// each castle while WoE is active. Subscribes to
	/// <see cref="WoeService.Started"/> / <see cref="WoeService.Ended"/>
	/// so spawning is fully driven by the lifecycle events; nothing
	/// else needs to touch the guardians directly.
	/// </summary>
	/// <remarks>
	/// Mirrors eAthena's <c>guardian_data</c> registry. Real eA stores
	/// per-slot defense investment and per-guardian HP scaling; we
	/// keep it simple here: one mob per defense tier (clamped 1..8),
	/// rotated through a small id pool so each castle has a mix of
	/// archers / knights / soldiers.
	/// </remarks>
	public class GuardianService
	{
		private readonly object _syncLock = new();
		private readonly Dictionary<string, List<Monster>> _spawned =
			new(StringComparer.OrdinalIgnoreCase);
		private bool _attached;

		/// <summary>
		/// Wires the start / end handlers to the global WoE lifecycle.
		/// Idempotent — re-calling no-ops.
		/// </summary>
		public void Attach(WoeService woe)
		{
			if (_attached || woe == null)
				return;
			_attached = true;
			woe.Started += this.OnWoeStarted;
			woe.Ended += this.OnWoeEnded;
		}

		private void OnWoeStarted()
		{
			try
			{
				this.SpawnAll();
			}
			catch (Exception ex)
			{
				Log.Error("GuardianService.OnWoeStarted: {0}", ex);
			}
		}

		private void OnWoeEnded()
		{
			try
			{
				this.DespawnAll();
			}
			catch (Exception ex)
			{
				Log.Error("GuardianService.OnWoeEnded: {0}", ex);
			}
		}

		private void SpawnAll()
		{
			var castles = ZoneServer.Instance.World.Castles.All;
			var maps = ZoneServer.Instance.World.Maps;

			foreach (var castle in castles)
			{
				if (!maps.TryGetByStringId(castle.MapStringId, out var map))
					continue;

				var count = Math.Clamp(castle.Defense / 20, 1, 8);
				var spawned = new List<Monster>();

				// eAthena defense scaling: each defense investment level
				// adds 1% to guardian HP, capping around +100% at level
				// 100. Matches the rA `mob_guardian` HP curve closely
				// enough for the alpha-era data we have today.
				var defenseScale = 1.0 + (castle.Defense / 100.0);

				for (var i = 0; i < count; i++)
				{
					var monsterId = PickGuardianId(i);
					try
					{
						var monster = new Monster((IdentityId)monsterId);
						if (monster.Data.AiName != null)
							monster.AttachAi(monster.Data.AiName);

						// Apply HP scaling after the base stats are
						// copied from MonsterData; the setter clamps Hp
						// so we reorder writes to seat the new max first.
						var scaledMax = (int)(monster.Parameters.HpMax * defenseScale);
						monster.Parameters.HpMax = scaledMax;
						monster.Parameters.Hp = scaledMax;

						var pos = map.GetRandomWalkablePosition();
						monster.Warp(map.Id, pos);
						spawned.Add(monster);
					}
					catch (Exception ex)
					{
						Log.Warning("GuardianService: failed to spawn guardian {0} on '{1}': {2}",
							monsterId, castle.MapStringId, ex.Message);
					}
				}

				lock (_syncLock)
					_spawned[castle.MapStringId] = spawned;

				Log.Info("GuardianService: spawned {0} guardian(s) on '{1}'.",
					spawned.Count, castle.MapStringId);
			}
		}

		private void DespawnAll()
		{
			List<KeyValuePair<string, List<Monster>>> snapshot;
			lock (_syncLock)
			{
				snapshot = new List<KeyValuePair<string, List<Monster>>>(_spawned);
				_spawned.Clear();
			}

			foreach (var pair in snapshot)
			{
				foreach (var monster in pair.Value)
				{
					try
					{
						monster.Map?.RemoveNpc(monster);
					}
					catch (Exception ex)
					{
						Log.Warning("GuardianService.DespawnAll: {0}", ex.Message);
					}
				}
			}
		}

		// Rotate through the three guardian tiers so each castle gets
		// a mix. Real eA picks per-slot from castle data; we round-
		// robin until per-slot config exists.
		private static int PickGuardianId(int slot)
		{
			return (slot % 3) switch
			{
				0 => MonsterId.SOLDIER_GUARDIAN,
				1 => MonsterId.ARCHER_GUARDIAN,
				_ => MonsterId.KNIGHT_GUARDIAN,
			};
		}
	}
}
