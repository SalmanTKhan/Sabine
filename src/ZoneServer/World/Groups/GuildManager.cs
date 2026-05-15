using System;
using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.World.Actors;
using Yggdrasil.Logging;

namespace Sabine.Zone.World.Groups
{
	/// <summary>
	/// Active guild registry. State lives in-memory; mutations are
	/// pushed through to the database (see
	/// <c>sql/updates/2026-05-03_002.sql</c>) so guilds and rosters
	/// survive restarts.
	/// </summary>
	/// <remarks>
	/// The legacy <see cref="Instance"/> static is preserved for
	/// existing call sites; new code should prefer
	/// <c>ZoneServer.Instance.World.Guilds</c> for symmetry with the
	/// rest of the world subsystems.
	/// </remarks>
	public class GuildManager
	{
		public static GuildManager Instance { get; } = new GuildManager();

		private readonly Dictionary<int, Guild> _guilds = new();
		private readonly Dictionary<string, Guild> _guildsByName = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<int, Guild> _guildsByMember = new();

		/// <summary>
		/// Loads every persisted guild + member row at startup.
		/// Called once from <see cref="WorldManager.LoadGuilds"/>.
		/// Idempotent — re-running clears the in-memory state and
		/// reloads from disk (useful for hot-reload tooling).
		/// </summary>
		public void LoadAll()
		{
			lock (_guilds)
			{
				_guilds.Clear();
				_guildsByName.Clear();
				_guildsByMember.Clear();

				var rows = ZoneServer.Instance.Database.LoadAllGuilds();
				foreach (var row in rows)
				{
					var guild = new Guild(row);
					guild.EnsureStorage();

					try
					{
						var stored = ZoneServer.Instance.Database.LoadGuildStorage(guild.Id);
						foreach (var item in stored)
							guild.Storage.AddItemInit(item);
					}
					catch (Exception ex)
					{
						Log.Error("GuildManager.LoadAll: failed to load storage for guild {0}: {1}", guild.Id, ex);
					}

					_guilds[guild.Id] = guild;
					_guildsByName[guild.Name] = guild;
					foreach (var m in guild.Members)
						_guildsByMember[m.CharacterId] = guild;
				}
				Log.Info("GuildManager: loaded {0} guild(s).", _guilds.Count);
			}
		}

		/// <summary>
		/// Creates a new guild led by <paramref name="master"/>,
		/// inserts it in the database, and registers it in memory.
		/// Returns null when the leader already has a guild or the
		/// name is taken.
		/// </summary>
		public Guild Create(PlayerCharacter master, string name)
		{
			lock (_guilds)
			{
				if (master.Guild != null) return null;
				if (_guildsByName.ContainsKey(name)) return null;

				var guild = new Guild(master, name);
				guild.Id = ZoneServer.Instance.Database.InsertGuild(
					name, master.Id, guild.Level, guild.ExpCurrent,
					guild.EmblemId, guild.Notice);
				ZoneServer.Instance.Database.InsertGuildMember(
					guild.Id, master.Id, isMaster: true, position: string.Empty);

				guild.EnsureStorage();

				_guilds[guild.Id] = guild;
				_guildsByName[name] = guild;
				_guildsByMember[master.Id] = guild;
				return guild;
			}
		}

		public Guild Get(int guildId)
		{
			lock (_guilds)
			{
				_guilds.TryGetValue(guildId, out var g);
				return g;
			}
		}

		public Guild GetByName(string name)
		{
			lock (_guilds)
			{
				_guildsByName.TryGetValue(name, out var g);
				return g;
			}
		}

		/// <summary>
		/// Resolves the guild for a given character id (online or
		/// offline). Used by the player-login reconcile path so
		/// `player.Guild` is populated for returning members.
		/// </summary>
		public Guild GetByMember(int characterId)
		{
			lock (_guilds)
			{
				_guildsByMember.TryGetValue(characterId, out var g);
				return g;
			}
		}

		public IReadOnlyList<Guild> All
		{
			get { lock (_guilds) return _guilds.Values.ToList(); }
		}

		/// <summary>
		/// Adds a member to a guild and persists the row. Returns
		/// false when the player is already in a guild or the
		/// guild's id is unknown.
		/// </summary>
		public bool AddMember(int guildId, PlayerCharacter player)
		{
			lock (_guilds)
			{
				if (player.Guild != null) return false;
				if (!_guilds.TryGetValue(guildId, out var guild)) return false;
				if (!guild.AddMember(player)) return false;

				ZoneServer.Instance.Database.InsertGuildMember(
					guildId, player.Id, isMaster: false, position: string.Empty);
				_guildsByMember[player.Id] = guild;

				this.SaveStorage(guild);
				return true;
			}
		}

		/// <summary>
		/// Removes a member from a guild and persists the deletion.
		/// </summary>
		public bool RemoveMember(int guildId, int characterId)
		{
			lock (_guilds)
			{
				if (!_guilds.TryGetValue(guildId, out var guild)) return false;
				if (!guild.RemoveMember(characterId)) return false;

				ZoneServer.Instance.Database.DeleteGuildMember(characterId);
				_guildsByMember.Remove(characterId);

				this.SaveStorage(guild);
				return true;
			}
		}

		/// <summary>
		/// Disbands a guild: drops every member's back-pointer,
		/// removes the in-memory record, and lets the FK cascade
		/// delete the member rows in the database.
		/// </summary>
		public bool Disband(int guildId)
		{
			lock (_guilds)
			{
				if (!_guilds.TryGetValue(guildId, out var guild)) return false;
				_guilds.Remove(guildId);
				_guildsByName.Remove(guild.Name);

				foreach (var m in guild.Members)
				{
					_guildsByMember.Remove(m.CharacterId);
					if (m.Player != null) m.Player.Guild = null;
				}

				ZoneServer.Instance.Database.DeleteGuild(guildId);
				return true;
			}
		}

		/// <summary>
		/// Hook invoked when a player finishes logging in. If the
		/// player is registered as a member of an existing guild,
		/// the live back-pointer is wired up here.
		/// </summary>
		public void OnPlayerLogin(PlayerCharacter player)
		{
			Guild guild;
			lock (_guilds)
			{
				if (!_guildsByMember.TryGetValue(player.Id, out guild))
					return;
			}
			guild.AttachOnlinePlayer(player);
		}

		/// <summary>
		/// Hook invoked when a player disconnects. Clears the live
		/// back-pointer on the matching member entry; the row
		/// stays in place.
		/// </summary>
		public void OnPlayerLogout(PlayerCharacter player)
		{
			Guild guild;
			lock (_guilds)
			{
				if (!_guildsByMember.TryGetValue(player.Id, out guild))
					return;
			}
			guild.DetachOnlinePlayer(player.Id);
			player.Guild = null;
		}

		/// <summary>
		/// Persists the scalar fields of an existing guild (level,
		/// exp, emblem, notice, name, master). Member roster mutations
		/// go through <see cref="AddMember"/> / <see cref="RemoveMember"/>.
		/// </summary>
		public void Save(Guild guild)
		{
			if (guild == null || guild.Id == 0)
				return;
			ZoneServer.Instance.Database.UpdateGuild(
				guild.Id, guild.Name, guild.MasterCharId,
				guild.Level, guild.ExpCurrent, guild.EmblemId,
				guild.Notice ?? string.Empty, guild.Zeny, guild.EmblemData);
		}

		/// <summary>
		/// Flushes a single guild's shared storage to the DB. Called
		/// on roster changes (so a leaving member can't leave the
		/// guild stash in an unflushed state) and from
		/// <see cref="SaveAllStorages"/> on map-server stop. Failures
		/// are logged but never thrown -- a transient DB error must
		/// not abort the calling membership mutation.
		/// </summary>
		public void SaveStorage(Guild guild)
		{
			if (guild == null || guild.Id == 0 || guild.Storage == null)
				return;

			try
			{
				ZoneServer.Instance.Database.SaveGuildStorage(
					guild.Id, guild.Storage.GetItems());
			}
			catch (Exception ex)
			{
				Log.Error("GuildManager.SaveStorage: failed to save storage for guild {0}: {1}", guild.Id, ex);
			}
		}

		/// <summary>
		/// Flushes every loaded guild's shared storage to the DB.
		/// Wired into the map-server shutdown hook so guild storage
		/// always survives a clean stop -- personal storage waits
		/// for logout, but guild storage outlives any single member.
		/// </summary>
		public void SaveAllStorages()
		{
			List<Guild> snapshot;
			lock (_guilds)
				snapshot = _guilds.Values.ToList();

			foreach (var guild in snapshot)
				this.SaveStorage(guild);
		}
	}
}
