using System;
using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Groups
{
	/// <summary>
	/// Guild record. Ownership of state mutations runs through
	/// <see cref="GuildManager"/> so the DB stays in sync. Allies /
	/// antagonists / custom rank labels are still in-memory only —
	/// the schema (`sql/updates/2026-05-03_002.sql`) covers the
	/// load-bearing fields and the membership roster.
	/// </summary>
	public class Guild
	{
		public int Id { get; internal set; }
		public string Name { get; set; }
		public int MasterCharId { get; set; }
		public int Level { get; set; } = 1;
		public int ExpCurrent { get; set; }
		public int EmblemId { get; set; }
		public string Notice { get; set; } = string.Empty;
		public string MasterName { get; set; }

		/// <summary>
		/// Shared guild storage. Lifetime matches the guild record;
		/// hydrated by <see cref="GuildManager.LoadAll"/> at startup
		/// and persisted via <see cref="Database.ZoneDb.SaveGuildStorage"/>
		/// on roster change / shutdown.
		/// </summary>
		public GuildStorage Storage { get; private set; }

		private readonly List<GuildMember> _members = new();
		public IReadOnlyList<GuildMember> Members => _members.AsReadOnly();

		private readonly HashSet<int> _allies = new();
		public IReadOnlyCollection<int> Allies => _allies;

		private readonly HashSet<int> _antagonists = new();
		public IReadOnlyCollection<int> Antagonists => _antagonists;

		/// <summary>
		/// Constructs a fresh guild for a newly-created run. The id
		/// stays at 0 until <see cref="GuildManager.Create"/>
		/// inserts the row and assigns the auto-generated DB id.
		/// </summary>
		public Guild(PlayerCharacter master, string name)
		{
			this.Name = name;
			this.MasterCharId = master.Id;
			this.MasterName = master.Name;
			_members.Add(new GuildMember(master, isMaster: true));
			master.Guild = this;
		}

		/// <summary>
		/// Reconstructs a guild from a persisted row. Members are
		/// added as offline placeholders (no `Player` reference); the
		/// online roster is reconciled lazily as players log in via
		/// <see cref="AttachOnlinePlayer"/>.
		/// </summary>
		internal Guild(GuildDbRow row)
		{
			this.Id = row.GuildId;
			this.Name = row.Name;
			this.MasterCharId = row.MasterCharId;
			this.Level = row.Level;
			this.ExpCurrent = row.ExpCurrent;
			this.EmblemId = row.EmblemId;
			this.Notice = row.Notice;

			foreach (var m in row.Members)
			{
				_members.Add(new GuildMember(m.CharacterId, m.IsMaster, m.Position));
				if (m.IsMaster && this.MasterName == null)
					this.MasterName = string.Empty;
			}
		}

		/// <summary>
		/// Ensures <see cref="Storage"/> is allocated and bound to the
		/// current <see cref="Id"/>. Idempotent: re-running with the
		/// same id is a no-op. Called by <see cref="GuildManager"/>
		/// after the DB id is known.
		/// </summary>
		internal void EnsureStorage()
		{
			if (this.Storage != null && this.Storage.GuildId == this.Id)
				return;
			this.Storage = new GuildStorage(this.Id);
		}

		/// <summary>
		/// Wires an online <see cref="PlayerCharacter"/> to the
		/// matching offline-placeholder member entry, lazily filling
		/// the display name and back-pointer. Called by
		/// <see cref="GuildManager.OnPlayerLogin"/> when a guild
		/// member's character logs in.
		/// </summary>
		internal void AttachOnlinePlayer(PlayerCharacter player)
		{
			var member = this.GetMember(player.Id);
			if (member == null)
				return;
			member.Player = player;
			member.Name = player.Name;
			player.Guild = this;
			if (member.IsMaster)
				this.MasterName = player.Name;
		}

		/// <summary>
		/// Detaches the online <see cref="PlayerCharacter"/>
		/// reference (called on logout). The member row stays in
		/// place; only the live back-pointer is cleared so the
		/// player object can GC.
		/// </summary>
		internal void DetachOnlinePlayer(int characterId)
		{
			var member = this.GetMember(characterId);
			if (member == null)
				return;
			member.Player = null;
		}

		public GuildMember GetMember(int characterId) => _members.FirstOrDefault(m => m.CharacterId == characterId);

		/// <summary>
		/// Returns true if <paramref name="player"/> is this guild's
		/// master (leader). Mirrors rAthena's <c>is_guild_leader()</c>.
		/// </summary>
		public bool IsLeader(PlayerCharacter player)
			=> player != null && player.Id == this.MasterCharId;

		public bool AddMember(PlayerCharacter character)
		{
			if (this.GetMember(character.Id) != null)
				return false;

			_members.Add(new GuildMember(character));
			character.Guild = this;
			return true;
		}

		public bool RemoveMember(int characterId)
		{
			var member = this.GetMember(characterId);
			if (member == null) return false;

			_members.Remove(member);
			if (member.Player != null) member.Player.Guild = null;
			return true;
		}

		public void AddAlly(int otherGuildId) { _allies.Add(otherGuildId); _antagonists.Remove(otherGuildId); }
		public void AddAntagonist(int otherGuildId) { _antagonists.Add(otherGuildId); _allies.Remove(otherGuildId); }
		public void RemoveAlly(int otherGuildId) => _allies.Remove(otherGuildId);
		public void RemoveAntagonist(int otherGuildId) => _antagonists.Remove(otherGuildId);

		public IEnumerable<PlayerCharacter> OnlineMembers => _members.Where(m => m.Player != null).Select(m => m.Player);
	}

	public class GuildMember
	{
		public int CharacterId { get; }
		public string Name { get; set; }
		public bool IsMaster { get; set; }
		public string Position { get; set; } = string.Empty;
		public PlayerCharacter Player { get; set; }

		/// <summary>
		/// Constructor for new members joining an active session —
		/// the live <see cref="PlayerCharacter"/> back-pointer is
		/// set immediately.
		/// </summary>
		public GuildMember(PlayerCharacter character, bool isMaster = false)
		{
			this.CharacterId = character.Id;
			this.Name = character.Name;
			this.IsMaster = isMaster;
			this.Player = character;
		}

		/// <summary>
		/// Constructor for members reconstructed from a DB row at
		/// startup. <see cref="Name"/> is filled in lazily when the
		/// player logs in (or via <c>ZoneDb.GetCharacterName</c> on
		/// demand). <see cref="Player"/> stays null while offline.
		/// </summary>
		public GuildMember(int characterId, bool isMaster, string position)
		{
			this.CharacterId = characterId;
			this.Name = string.Empty;
			this.IsMaster = isMaster;
			this.Position = position ?? string.Empty;
		}
	}
}
