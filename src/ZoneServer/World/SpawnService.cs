using System;
using System.Collections.Generic;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;
using Yggdrasil.Logging;

namespace Sabine.Zone.World
{
	/// <summary>
	/// One-shot monster spawning utilities used by converted scripts.
	/// Mirrors rAthena's <c>monster</c>, <c>killmonster</c>, and
	/// <c>mobcount</c> commands.
	/// </summary>
	/// <remarks>
	/// This is intentionally separate from
	/// <see cref="Sabine.Zone.World.Spawning.Spawners"/>: those are
	/// long-lived map spawners that respawn on death; the service
	/// here spawns a one-shot batch and tracks them so they can be
	/// reaped (<see cref="KillMonsters"/>) or counted
	/// (<see cref="MobCount"/>) by label.
	/// </remarks>
	public static class SpawnService
	{
		// Key: "mapStringId|label" — labels namespaced per map so the
		// same OnLabel can be reused across maps without aliasing.
		private static readonly Dictionary<string, List<Monster>> _byLabel = new();
		private static readonly object _syncLock = new();

		private static string MakeKey(string mapStringId, string label)
			=> mapStringId + "|" + (label ?? string.Empty);

		/// <summary>
		/// Spawns <paramref name="count"/> monsters of
		/// <paramref name="monsterId"/> at (x, y) on the given map.
		/// Each monster is tagged with <paramref name="label"/>; on
		/// death, <paramref name="onDeath"/> fires (if set) and the
		/// monster is removed from the label's tracking list.
		/// </summary>
		/// <returns>The list of spawned monsters (empty if the map is unknown).</returns>
		public static List<Monster> SpawnMonster(string mapStringId, int x, int y, string label, int monsterId, int count, Action<Monster> onDeath = null)
		{
			var spawned = new List<Monster>();

			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(mapStringId, out var map))
			{
				Log.Warning("SpawnService.SpawnMonster: unknown map '{0}'.", mapStringId);
				return spawned;
			}

			var key = MakeKey(mapStringId, label);

			for (var i = 0; i < count; ++i)
			{
				var monster = new Monster((IdentityId)monsterId);

				if (monster.Data?.AiName != null)
					monster.AttachAi(monster.Data.AiName);

				monster.Killed += m =>
				{
					RemoveFromLabel(key, m);
					if (onDeath != null)
					{
						try { onDeath(m); }
						catch (Exception ex) { Log.Error("SpawnService.onDeath: {0}", ex); }
					}
				};

				monster.Warp(map.Id, new Position(x, y));
				spawned.Add(monster);
			}

			lock (_syncLock)
			{
				if (!_byLabel.TryGetValue(key, out var list))
					_byLabel[key] = list = new List<Monster>();
				list.AddRange(spawned);
			}

			return spawned;
		}

		/// <summary>
		/// Kills (and removes) all live monsters spawned via
		/// <see cref="SpawnMonster"/> on <paramref name="mapStringId"/>
		/// with the matching <paramref name="label"/>. Mirrors
		/// rAthena's <c>killmonster</c>. Returns the number killed.
		/// </summary>
		public static int KillMonsters(string mapStringId, string label)
		{
			var key = MakeKey(mapStringId, label);
			Monster[] snapshot;

			lock (_syncLock)
			{
				if (!_byLabel.TryGetValue(key, out var list) || list.Count == 0)
					return 0;
				snapshot = list.ToArray();
				list.Clear();
			}

			var killed = 0;
			foreach (var monster in snapshot)
			{
				try
				{
					if (monster.Map != null && monster.Map != Sabine.Zone.World.Maps.Map.Limbo)
					{
						monster.Map.RemoveNpc(monster);
						killed++;
					}
				}
				catch (Exception ex)
				{
					Log.Warning("SpawnService.KillMonsters: failed to remove monster: {0}", ex.Message);
				}
			}

			return killed;
		}

		/// <summary>
		/// Returns the live monster count for a (map, label) pair.
		/// Mirrors rAthena's <c>mobcount</c>.
		/// </summary>
		public static int MobCount(string mapStringId, string label)
		{
			var key = MakeKey(mapStringId, label);
			lock (_syncLock)
			{
				if (!_byLabel.TryGetValue(key, out var list))
					return 0;
				return list.Count;
			}
		}

		private static void RemoveFromLabel(string key, Monster monster)
		{
			lock (_syncLock)
			{
				if (_byLabel.TryGetValue(key, out var list))
					list.Remove(monster);
			}
		}
	}
}
