using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sabine.Shared;
using Sabine.Zone.Network;
using Sabine.Zone.Scripting;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps;
using Sabine.Zone.World;
using Yggdrasil.Logging;

namespace Sabine.Zone.World.Instances
{
	/// <summary>
	/// Tracks the active instances on the world. Allocates ids,
	/// instantiates clone maps, registers them with
	/// <see cref="MapManager"/>, and tears them down on destroy.
	/// </summary>
	/// <remarks>
	/// Implements <see cref="IUpdateable"/> so the world heartbeat
	/// can sweep expired instances on every tick — see
	/// <see cref="Update"/>.
	/// </remarks>
	public class InstanceManager : IUpdateable
	{
		private readonly object _syncLock = new();
		private readonly Dictionary<int, Instance> _instances = new();

		/// <summary>
		/// Base offset for synthesized instance map ids. Chosen well
		/// above the highest static <c>MapsDb</c> entry (typically
		/// under 10000) so runtime-allocated instance map ids can
		/// never collide with statically-loaded maps.
		/// </summary>
		private const int InstanceMapIdBase = int.MaxValue / 2;

		// Counter for synthesizing instance map ids; advances from
		// InstanceMapIdBase on every Create().
		private int _nextMapId = InstanceMapIdBase;
		private int _nextInstanceId;

		/// <summary>
		/// Allocates a new instance, scoped to the given owner. Either
		/// or both of <paramref name="ownerCharId"/> and
		/// <paramref name="ownerPartyId"/> may be set; the lookup
		/// helpers in <see cref="TryFindByOwner"/> consult them in
		/// guild → party → char order, matching the rAthena precedence.
		/// </summary>
		public Instance Create(string templateName, InstanceMode mode, int ownerId,
			int ownerCharId = 0, int ownerPartyId = 0, int ownerGuildId = 0,
			int ownerClanId = 0, string entryMap = "", int entryX = 0, int entryY = 0)
		{
			var id = Interlocked.Increment(ref _nextInstanceId);
			var instance = new Instance(id, templateName, mode, ownerId,
				ownerCharId, ownerPartyId, ownerGuildId, ownerClanId,
				entryMap, entryX, entryY);

			lock (_syncLock)
				_instances[id] = instance;

			NotifyOwner(instance, state: 1); // 1 = subscription created
			return instance;
		}

		/// <summary>
		/// Resolves an instance by id, or null when no such instance
		/// exists (or it has been disposed).
		/// </summary>
		public Instance Get(int instanceId)
		{
			lock (_syncLock)
				return _instances.TryGetValue(instanceId, out var inst) && !inst.IsDisposed
					? inst
					: null;
		}

		/// <summary>
		/// Tries to resolve an instance owned by the given player —
		/// guild-scoped first (any of the player's guild instances),
		/// then party-scoped, then char-scoped. Returns false when
		/// the player has no active instance.
		/// </summary>
		public bool TryFindByOwner(PlayerCharacter player, InstanceMode mode, out Instance instance)
		{
			instance = null;
			if (player == null)
				return false;

			var ownerId = GetOwnerId(player, mode);
			if (mode != InstanceMode.None && ownerId == 0)
				return false;

			lock (_syncLock)
			{
				foreach (var inst in _instances.Values)
				{
					if (inst.IsDisposed)
						continue;
					if (inst.Mode == mode && inst.OwnerId == ownerId)
					{
						instance = inst;
						return true;
					}
				}
			}

			return false;
		}

		public bool TryFindByOwner(PlayerCharacter player, out Instance instance)
		{
			foreach (var mode in new[] { InstanceMode.Guild, InstanceMode.Party, InstanceMode.Character, InstanceMode.Clan })
			{
				if (this.TryFindByOwner(player, mode, out instance))
					return true;
			}

			instance = null;
			return false;
		}

		public static int GetOwnerId(PlayerCharacter player, InstanceMode mode)
		{
			if (player == null)
				return 0;

			return mode switch
			{
				InstanceMode.Character => player.Id,
				InstanceMode.Party => player.Party?.Id ?? 0,
				InstanceMode.Guild => player.Guild?.Id ?? 0,
				InstanceMode.Clan => Groups.ClanManager.Instance.GetByMember(player.Id)?.Id ?? 0,
				_ => 0,
			};
		}

		/// <summary>
		/// Clones the source map, registers the clone with
		/// <see cref="MapManager"/>, and attaches it to the instance.
		/// Returns the new instance map's string id (e.g.
		/// `"orcs_lab@5"`) on success, or null if the source map
		/// isn't loaded on this server.
		/// </summary>
		public string AttachMap(Instance instance, string sourceMapStringId)
		{
			if (instance == null || instance.IsDisposed)
				return null;

			var maps = ZoneServer.Instance.World.Maps;
			if (!maps.TryGetByStringId(sourceMapStringId, out var source))
			{
				Log.Warning("InstanceManager.AttachMap: source map '{0}' not loaded.",
					sourceMapStringId);
				return null;
			}

			var instanceStringId = instance.GetInstanceMapName(sourceMapStringId);
			var newId = Interlocked.Increment(ref _nextMapId);
			var clone = Map.CreateInstanceClone(source, newId, instanceStringId, instance.Id);

			maps.Add(clone);
			instance.AttachMap(sourceMapStringId, clone);
			this.CloneStaticNpcs(source, clone, instance);

			NotifyOwnerMapAttached(instance, sourceMapStringId);
			return instanceStringId;
		}

		private void CloneStaticNpcs(Map source, Map clone, Instance instance)
		{
			foreach (var sourceNpc in source.GetAllNpcs())
			{
				if (sourceNpc is Monster)
					continue;

				var cloneNpc = sourceNpc.CreateInstanceClone($"{sourceNpc.Name}#{instance.Id}", clone.Id);
				clone.AddNpc(cloneNpc);
				_ = ScriptEvents.RaiseAsync($"{cloneNpc.Name}::OnInstanceInit", instance.Id);
			}
		}

		/// <summary>
		/// Tears down an instance: removes its cloned maps from the
		/// global map manager, disposes the instance (which disposes
		/// each map), and forgets the id.
		/// </summary>
		public void Destroy(int instanceId)
		{
			Instance inst;
			lock (_syncLock)
			{
				if (!_instances.TryGetValue(instanceId, out inst))
					return;
				_instances.Remove(instanceId);
			}

			NotifyOwner(inst, state: 3); // 3 = subscription ended
			foreach (var map in inst.GetMaps())
			{
				foreach (var npc in map.GetAllNpcs())
					_ = ScriptEvents.RaiseAsync($"{npc.Name}::OnInstanceDestroy", inst.Id);
			}

			var maps = ZoneServer.Instance.World.Maps;
			foreach (var map in inst.GetMaps())
				maps.Remove(map);

			inst.Dispose();
		}

		/// <summary>
		/// Resolves the instance's char-id owner to an online player and
		/// emits the memorial-dungeon subscription update. No-op when the
		/// instance is party/guild-scoped without a single resolvable owner,
		/// when the owner is offline, or when the running protocol version
		/// doesn't carry the packet (pre-S2500).
		/// </summary>
		private static void NotifyOwner(Instance instance, short state)
		{
			if (Game.Version < Versions.S2500)
				return;
			if (instance.OwnerCharId == 0)
				return;

			var pc = FindOwner(instance.OwnerCharId);
			if (pc == null)
				return;

			Send.ZC_MEMORIALDUNGEON_SUBSCRIPTION_NOTIFY(pc, state);
		}

		/// <summary>
		/// Sends the per-instance info packets after a clone map is attached.
		/// Mirrors rAthena's instance_reqinfo flow that fires once the
		/// dungeon map exists and the owner can be told where to enter.
		/// </summary>
		private static void NotifyOwnerMapAttached(Instance instance, string baseMapName)
		{
			if (Game.Version < Versions.S2500)
				return;
			if (instance.OwnerCharId == 0)
				return;

			var pc = FindOwner(instance.OwnerCharId);
			if (pc == null)
				return;

			var remaining = Math.Max(0, (int)(instance.ExpiresAt - DateTime.UtcNow).TotalSeconds);
			Send.ZC_MEMORIALDUNGEON_SUBSCRIPTION_INFO(pc, $"instance#{instance.Id}", baseMapName, remaining);
			Send.ZC_MEMORIALDUNGEON_INFO(pc, $"instance#{instance.Id}", remaining, 0);
		}

		private static PlayerCharacter FindOwner(int charId)
		{
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				if (map.TryGetPlayerById(charId, out var pc))
					return pc;
			}
			return null;
		}

		/// <summary>
		/// Returns a snapshot of every active (non-disposed) instance.
		/// Mostly useful for debugging / GM commands.
		/// </summary>
		public IReadOnlyList<Instance> GetAll()
		{
			lock (_syncLock)
				return _instances.Values.Where(i => !i.IsDisposed).ToArray();
		}

		/// <summary>
		/// Heartbeat tick: destroys any instance whose
		/// <see cref="Instance.ExpiresAt"/> is in the past. Runs on
		/// the world's update loop after the manager is registered
		/// via <c>WorldManager.Heartbeat.Add(this)</c>.
		/// </summary>
		public void Update(TimeSpan elapsed)
		{
			List<int> expired = null;
			var now = DateTime.UtcNow;
			lock (_syncLock)
			{
				foreach (var inst in _instances.Values)
				{
					if (inst.IsDisposed)
						continue;
					if (inst.ExpiresAt <= now)
					{
						(expired ??= new List<int>()).Add(inst.Id);
						continue;
					}

					if (inst.GetMaps().Any(map => map.PlayerCount > 0))
					{
						inst.MarkOccupied();
						continue;
					}

					if (inst.IdleTimeout > TimeSpan.Zero
						&& inst.LastOccupiedAt + inst.IdleTimeout <= now)
					{
						Log.Info("InstanceManager: instance {0} expired while idle.", inst.Id);
						(expired ??= new List<int>()).Add(inst.Id);
					}
				}
			}

			if (expired == null)
				return;

			foreach (var id in expired)
			{
				Log.Info("InstanceManager: instance {0} expired (ttl reached), destroying.", id);
				this.Destroy(id);
			}
		}
	}
}
