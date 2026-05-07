using System;
using System.Linq;
using Sabine.Shared.Util;
using Sabine.Zone.Network;
using Sabine.Zone.Scripting;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Chats;
using Sabine.Zone.World.Instances;
using Sabine.Zone.World.Maps;
using Sabine.Zone.World.Shops;
using Sabine.Zone.World.Spawning;
using Sabine.Zone.World.Trading;
using Yggdrasil.Collections;
using Yggdrasil.Logging;
using Yggdrasil.Scheduling;

namespace Sabine.Zone.World
{
	/// <summary>
	/// Represents the world and everything in it.
	/// </summary>
	public class WorldManager
	{
		/// <summary>
		/// Returns a reference to a collection of maps in the world.
		/// </summary>
		public MapManager Maps { get; } = new();

		/// <summary>
		/// The world's heartbeast, which controls timed events.
		/// </summary>
		public Heartbeat Heartbeat { get; } = new();

		/// <summary>
		/// The world's scheduler. Use only if a high resolution timer is
		/// absolutely necessary.
		/// </summary>
		public Scheduler Scheduler { get; } = new();

		/// <summary>
		/// Returns a reference to a collection of NPC shops in the world.
		/// </summary>
		public Collection<string, NpcShop> NpcShops { get; } = new();

		/// <summary>
		/// Returns the trade manager, handling trades between players in
		/// the world.
		/// </summary>
		public Trades Trades { get; } = new();

		/// <summary>
		/// Returns the vending registry, tracking open player shops.
		/// </summary>
		public Vendings Vendings { get; } = new();

		/// <summary>
		/// Returns the chat room manager, handling chat rooms created by
		/// players.
		/// </summary>
		public ChatRooms ChatRooms { get; } = new();

		/// <summary>
		/// Returns the instance registry, tracking active dungeon /
		/// scoped-map instances. See <see cref="InstanceManager"/>.
		/// </summary>
		public InstanceManager Instances { get; } = new();

		/// <summary>
		/// Active guild registry. State is DB-backed (see
		/// <c>sql/updates/2026-05-03_002.sql</c>); call <c>LoadAll</c>
		/// once at startup to populate from the database. Mutations
		/// (create, add/remove member, disband, scalar save) push
		/// through to disk transparently.
		/// </summary>
		public Groups.GuildManager Guilds => Groups.GuildManager.Instance;

		/// <summary>
		/// WoE castle ownership registry. Static templates live in
		/// <c>system/data/castles.txt</c>; runtime state (owner,
		/// economy/defense, triggers) is DB-backed via
		/// <c>sql/updates/2026-05-03_003.sql</c>.
		/// </summary>
		public Woe.CastleManager Castles { get; } = new();

		/// <summary>
		/// Global WoE on/off lifecycle (Phase 9). Drives the
		/// `Agit::OnAgitStart` / `OnAgitEnd` event fanout that
		/// agit-related scripts subscribe to.
		/// </summary>
		public Woe.WoeService Woe { get; } = new();

		/// <summary>
		/// Active Battleground (Flavius / KVM / Tierra) team registry.
		/// In-memory only; matches reset on restart. See
		/// <see cref="Battlegrounds.BattlegroundManager"/>.
		/// </summary>
		public Battlegrounds.BattlegroundManager Battlegrounds { get; } = new();

		/// <summary>
		/// Returns the world's monster spawner collection.
		/// </summary>
		public Spawners Spawners { get; } = new Spawners();

		/// <summary>
		/// Server-wide variables. Mirrors rAthena's <c>$x</c> (perm) and
		/// <c>$@x</c> (temp) sigils. <c>Vars.Perm</c> is loaded from
		/// <c>vars_world</c> at startup and saved on shutdown.
		/// <c>Vars.Temp</c> is in-memory only and resets on restart.
		/// </summary>
		public VariableContainer Vars { get; } = new VariableContainer();

		/// <summary>
		/// Fixed owner id used by <see cref="LoadVars"/> / <see cref="SaveVars"/>
		/// when reading/writing <c>vars_world</c>. World-scope is a singleton,
		/// so a constant 0 keeps it inside the existing <c>(ownerId, name)</c>
		/// schema without needing a separate code path.
		/// </summary>
		private const long WorldVarsOwnerId = 0;

		/// <summary>
		/// Loads world and its maps.
		/// </summary>
		public void Load()
		{
			this.LoadMaps();
			this.LoadVars();
			this.LoadGuilds();
			this.LoadCastles();
			this.SetupUpdates();
		}

		/// <summary>
		/// Hydrates <see cref="Castles"/> from the `castle_state`
		/// table.
		/// </summary>
		private void LoadCastles()
		{
			try
			{
				this.Castles.LoadAll();
			}
			catch (Exception ex)
			{
				Log.Error("WorldManager.LoadCastles: failed to load castles: {0}", ex);
			}
		}

		/// <summary>
		/// Hydrates <see cref="Guilds"/> from the `guilds` /
		/// `guild_members` tables. Player-side guild back-pointers
		/// are wired up later, on each character's first login,
		/// via <see cref="Groups.GuildManager.OnPlayerLogin"/>.
		/// </summary>
		private void LoadGuilds()
		{
			try
			{
				this.Guilds.LoadAll();
			}
			catch (Exception ex)
			{
				Log.Error("WorldManager.LoadGuilds: failed to load guilds: {0}", ex);
			}
		}

		/// <summary>
		/// Hydrates <see cref="Vars"/>.<c>Perm</c> from the
		/// <c>vars_world</c> table.
		/// </summary>
		private void LoadVars()
		{
			try
			{
				var stored = ZoneServer.Instance.Database.GetVars("vars_world", WorldVarsOwnerId);
				this.Vars.Perm.Load(stored);
			}
			catch (Exception ex)
			{
				Log.Error("WorldManager.LoadVars: failed to load world variables: {0}", ex);
			}
		}

		/// <summary>
		/// Persists <see cref="Vars"/>.<c>Perm</c> to the
		/// <c>vars_world</c> table. Called automatically on process exit;
		/// can also be invoked manually (e.g. from a chat command) to
		/// flush mid-session.
		/// </summary>
		public void SaveVars()
		{
			try
			{
				ZoneServer.Instance.Database.SaveVars("vars_world", WorldVarsOwnerId, this.Vars.Perm.GetList());
			}
			catch (Exception ex)
			{
				Log.Error("WorldManager.SaveVars: failed to save world variables: {0}", ex);
			}
		}

		/// <summary>
		/// Subscribes world to its heartbeat, to update its maps.
		/// </summary>
		private void SetupUpdates()
		{
			this.Heartbeat.HeartbeatTick += this.Maps.Update;
			this.Heartbeat.HeartbeatTick += this.Instances.Update;
		}

		/// <summary>
		/// Generates maps based on all map data.
		/// </summary>
		private void LoadMaps()
		{
			foreach (var data in ZoneServer.Instance.Data.Maps.Entries.Values)
			{
				var map = new Map(data);
				this.Maps.Add(map);
			}
		}

		/// <summary>
		/// Starts the world's regular events through its hearbeats and
		/// schedulers.
		/// </summary>
		public void Start()
		{
			this.Scheduler.CallbackException += this.OnSchedulerException;

			this.Heartbeat.Start();
			this.Scheduler.Start();
		}

		/// <summary>
		/// Called when an exception occurred while executing a scheduled
		/// callback.
		/// </summary>
		/// <param name="ex"></param>
		private void OnSchedulerException(CallbackException ex)
		{
			Log.Error("An exception occurred while executing a scheduled callback. Exception: {0}", ex);
		}

		/// <summary>
		/// Looks up the first <see cref="Npc"/> with the given visible
		/// <paramref name="name"/> across every map. Used by converted
		/// rAthena scripts that target a specific NPC by name (e.g.
		/// <c>disablenpc "OtherName"</c>); returns <c>null</c> if no
		/// match is found.
		/// </summary>
		/// <remarks>
		/// Linear scan across every map's NPC list. Acceptable while
		/// scripted-NPC counts stay in the low thousands and the calls
		/// are infrequent (script load and one-off dialog actions); if
		/// a hot path needs this, build a name index instead.
		/// </remarks>
		public Npc GetNpc(string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			foreach (var map in this.Maps.GetAll())
			{
				foreach (var npc in map.GetAllNpcs())
				{
					if (string.Equals(npc.Name, name, StringComparison.Ordinal))
						return npc;
				}
			}
			return null;
		}

		/// <summary>
		/// Removes the NPC with the given visible <paramref name="name"/>
		/// from its map. Mirrors rAthena's <c>unloadnpc "Name"</c>.
		/// Returns <c>true</c> if a matching NPC was found and removed.
		/// </summary>
		public bool RemoveNpc(string name)
		{
			var npc = this.GetNpc(name);
			if (npc?.Map == null)
				return false;

			npc.Map.RemoveNpc(npc);
			return true;
		}

		/// <summary>
		/// Broadcasts an announcement to every player on every map.
		/// Mirrors rAthena's global <c>announce</c>; for a map-scoped
		/// announcement use <see cref="Map.Announce"/>.
		/// </summary>
		/// <param name="message">The message to display.</param>
		public void Announce(string message)
			=> Send.ZC_BROADCAST(message);

		/// <summary>
		/// Returns the number of players across all maps.
		/// </summary>
		/// <returns></returns>
		public int GetPlayerCount()
		{
			var maps = this.Maps.GetAll();
			return maps.Sum(a => a.PlayerCount);
		}

		/// <summary>
		/// Removes all entities that were added dynamically from the
		/// world, such as NPCs.
		/// </summary>
		public void RemoveScriptedEntities()
		{
			this.NpcShops.Clear();
			this.Spawners.Clear();
			SpawnService.ClearAll();
			ScriptEvents.ClearAll();

			// Yes, this is inefficient, but do we really care, with how
			// rare script reloads are?
			foreach (var map in this.Maps.GetAll())
			{
				var npcs = map.GetAllNpcs();
				foreach (var npc in npcs)
					map.RemoveNpc(npc);
			}
		}
	}
}
