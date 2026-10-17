using System;
using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.World.Maps;

namespace Sabine.Zone.World.Instances
{
	/// <summary>rAthena instance ownership modes.</summary>
	public enum InstanceMode
	{
		None,
		Character,
		Party,
		Guild,
		Clan,
	}

	/// <summary>rAthena <c>instance_enter</c> return values.</summary>
	public enum InstanceEnterResult
	{
		Ok,
		NoMember,
		NoInstance,
		Other,
	}

	/// <summary>
	/// A scoped, runtime-only copy of one or more maps shared by a single
	/// owner (a player, party, or guild). Mirrors rAthena's instance
	/// concept: dungeons that need their own isolated map state per
	/// party run (drops, monster kills, NPC progress) instead of bleed-
	/// through from a global map.
	/// </summary>
	/// <remarks>
	/// Instances are not persisted; they live for the duration of the
	/// run and are torn down on <see cref="Dispose"/>. Static tile data
	/// (the underlying <c>MapCacheData</c>) is shared with the source
	/// map; only runtime state (NPC list, monster spawns, cell flag
	/// overlay) is per-instance.
	/// </remarks>
	public class Instance : IDisposable
	{
		private readonly object _mapsLock = new();
		private readonly Dictionary<string, Map> _maps = new();

		/// <summary>
		/// Server-allocated instance id (positive integer). Used by the
		/// `<base>@<id>` suffix scheme in <see cref="GetInstanceMapName"/>
		/// and the `<name>#<id>` suffix in <see cref="GetInstanceNpcName"/>.
		/// </summary>
		public int Id { get; }
		public string TemplateName { get; }
		public InstanceMode Mode { get; }
		public int OwnerId { get; }

		/// <summary>
		/// Char id of the player who created this instance (or 0 if the
		/// instance is party/guild scoped without a single owner). Used
		/// by <see cref="InstanceManager.TryFindByOwner"/> to resolve a
		/// player back to their instance.
		/// </summary>
		public int OwnerCharId { get; }

		/// <summary>
		/// Party id this instance is bound to, or 0 when the instance
		/// is char-scoped. When set, every member of the party shares
		/// the instance (resolution looks at OwnerPartyId first).
		/// </summary>
		public int OwnerPartyId { get; }

		/// <summary>
		/// Guild id this instance is bound to, or 0 when char/party-
		/// scoped.
		/// </summary>
		public int OwnerGuildId { get; }

		public int OwnerClanId { get; }
		public string EntryMap { get; }
		public int EntryX { get; }
		public int EntryY { get; }

		/// <summary>
		/// Wall-clock time the instance was created. Combined with
		/// <see cref="Ttl"/> to compute <see cref="ExpiresAt"/>.
		/// </summary>
		public DateTime CreatedAt { get; } = DateTime.UtcNow;

		/// <summary>
		/// How long the instance lives before <see cref="InstanceManager"/>
		/// auto-destroys it. Default mirrors rAthena's one-hour cap;
		/// templates can override via <c>InstanceData.TtlMinutes</c>.
		/// </summary>
		public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(1);
		public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
		public DateTime LastOccupiedAt { get; private set; } = DateTime.UtcNow;

		/// <summary>
		/// Convenience computed property — wall-clock time at which
		/// the instance manager's tick will tear this down. Updates
		/// when <see cref="Ttl"/> is reassigned.
		/// </summary>
		public DateTime ExpiresAt => this.CreatedAt + this.Ttl;

		/// <summary>
		/// Returns true once <see cref="Dispose"/> has been called.
		/// Disposed instances should not be reused; callers that hold
		/// a stale reference can check this before touching maps.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Creates a new instance descriptor. Maps are attached
		/// separately via <see cref="AttachMap"/> so the manager can
		/// track ownership of each clone.
		/// </summary>
		public Instance(int id, string templateName, InstanceMode mode, int ownerId,
			int ownerCharId = 0, int ownerPartyId = 0, int ownerGuildId = 0,
			int ownerClanId = 0, string entryMap = "", int entryX = 0, int entryY = 0)
		{
			this.Id = id;
			this.TemplateName = templateName;
			this.Mode = mode;
			this.OwnerId = ownerId;
			this.OwnerCharId = ownerCharId;
			this.OwnerPartyId = ownerPartyId;
			this.OwnerGuildId = ownerGuildId;
			this.OwnerClanId = ownerClanId;
			this.EntryMap = entryMap;
			this.EntryX = entryX;
			this.EntryY = entryY;
		}

		public void MarkOccupied() => this.LastOccupiedAt = DateTime.UtcNow;

		/// <summary>
		/// Records an attached map clone keyed by the *base* map name
		/// (the original `prontera` rather than `prontera@5`), so
		/// scripts that ask `instance_mapname("orcs_lab")` can
		/// resolve back to the per-instance copy.
		/// </summary>
		public void AttachMap(string baseStringId, Map clone)
		{
			lock (_mapsLock)
				_maps[baseStringId] = clone;
		}

		/// <summary>
		/// Returns the cloned map for the given base name (e.g.
		/// `"1@orcs"`), or null when no such map is attached.
		/// </summary>
		public Map TryGetMap(string baseStringId)
		{
			lock (_mapsLock)
				return _maps.TryGetValue(baseStringId, out var map) ? map : null;
		}

		/// <summary>
		/// Returns a snapshot of all attached map clones. Used by the
		/// manager during disposal to unregister each from
		/// <see cref="MapManager"/>.
		/// </summary>
		public IReadOnlyList<Map> GetMaps()
		{
			lock (_mapsLock)
				return _maps.Values.ToArray();
		}

		/// <summary>
		/// Resolves the per-instance map *string id* for a base name —
		/// `"orcs_lab" -> "orcs_lab@5"` — without needing to look the
		/// clone up. The format is rAthena's documented convention.
		/// </summary>
		public string GetInstanceMapName(string baseStringId)
			=> $"{baseStringId}@{this.Id}";

		/// <summary>
		/// Resolves the per-instance NPC name for a base name —
		/// `"#Boss" -> "#Boss#5"` — used by `instance_npcname`. NPCs
		/// inside an instance map carry the suffix so script-side
		/// `donpcevent` calls scope to a single run.
		/// </summary>
		public string GetInstanceNpcName(string baseName)
			=> $"{baseName}#{this.Id}";

		/// <summary>
		/// Tears the instance down. Disposes every attached map clone
		/// (releases their lock objects) and clears the map dictionary.
		/// The manager removes the clones from <see cref="MapManager"/>
		/// before calling this.
		/// </summary>
		public void Dispose()
		{
			if (this.IsDisposed)
				return;

			this.IsDisposed = true;

			lock (_mapsLock)
			{
				foreach (var map in _maps.Values)
					map.Dispose();
				_maps.Clear();
			}
		}
	}
}
