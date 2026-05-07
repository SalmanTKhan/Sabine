using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

		// Counter for synthesizing instance map ids. We pick a high
		// base so the synthesized values never collide with the
		// statically-loaded `MapsDb` ids (those are typically below
		// 10000); using `int.MaxValue / 2` gives a generous runway.
		private int _nextMapId = int.MaxValue / 2;
		private int _nextInstanceId;

		/// <summary>
		/// Allocates a new instance, scoped to the given owner. Either
		/// or both of <paramref name="ownerCharId"/> and
		/// <paramref name="ownerPartyId"/> may be set; the lookup
		/// helpers in <see cref="TryFindByOwner"/> consult them in
		/// guild → party → char order, matching the rAthena precedence.
		/// </summary>
		public Instance Create(int ownerCharId = 0, int ownerPartyId = 0, int ownerGuildId = 0)
		{
			var id = Interlocked.Increment(ref _nextInstanceId);
			var instance = new Instance(id, ownerCharId, ownerPartyId, ownerGuildId);

			lock (_syncLock)
				_instances[id] = instance;

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
		public bool TryFindByOwner(PlayerCharacter player, out Instance instance)
		{
			instance = null;
			if (player == null)
				return false;

			var guildId = player.Guild?.Id ?? 0;
			var partyId = player.Party?.Id ?? 0;
			var charId = player.Id;

			lock (_syncLock)
			{
				foreach (var inst in _instances.Values)
				{
					if (inst.IsDisposed)
						continue;
					if (guildId != 0 && inst.OwnerGuildId == guildId)
					{
						instance = inst;
						return true;
					}
					if (partyId != 0 && inst.OwnerPartyId == partyId)
					{
						instance = inst;
						return true;
					}
					if (charId != 0 && inst.OwnerCharId == charId)
					{
						instance = inst;
						return true;
					}
				}
			}

			return false;
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
			var clone = Map.CreateInstanceClone(source, newId, instanceStringId);

			maps.Add(clone);
			instance.AttachMap(sourceMapStringId, clone);

			return instanceStringId;
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

			var maps = ZoneServer.Instance.World.Maps;
			foreach (var map in inst.GetMaps())
				maps.Remove(map);

			inst.Dispose();
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
					if (inst.IsDisposed || inst.ExpiresAt > now)
						continue;
					(expired ??= new List<int>()).Add(inst.Id);
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
