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
		/// Guild treasury balance. Mutated via <see cref="DepositZeny"/>
		/// / <see cref="WithdrawZeny"/>; persisted by
		/// <see cref="GuildManager.Save"/>.
		/// </summary>
		public long Zeny { get; set; }

		/// <summary>
		/// Raw emblem-image bytes (BMP) or null when none uploaded. Set
		/// via <c>CZ_REGISTER_GUILD_EMBLEM_IMG</c> and persisted to
		/// <c>guilds.emblemData</c>. Served back to clients via
		/// <c>ZC_GUILD_EMBLEM_IMG</c>.
		/// </summary>
		public byte[] EmblemData { get; set; }

		/// <summary>
		/// Shared guild storage. Lifetime matches the guild record;
		/// hydrated by <see cref="GuildManager.LoadAll"/> at startup
		/// and persisted via <see cref="Database.ZoneDb.SaveGuildStorage"/>
		/// on roster change / shutdown.
		/// </summary>
		public GuildStorage Storage { get; private set; }

		private readonly List<GuildMember> _members = new();
		public IReadOnlyList<GuildMember> Members => _members.AsReadOnly();

		// Guild skills are owned by the guild, not its leader. Keep them in
		// their own keyed collection because rAthena skill ids are sparse.
		private readonly Dictionary<int, int> _skills = new();
		public IReadOnlyDictionary<int, int> Skills => _skills;

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
			this.Zeny = row.Zeny;
			this.EmblemData = row.EmblemData;
			foreach (var skill in row.Skills)
				_skills[skill.SkillId] = skill.Level;

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

		public int GetSkillLevel(int skillId)
			=> _skills.TryGetValue(skillId, out var level) ? level : 0;

		internal void SetSkillLevel(int skillId, int level)
		{
			if (level <= 0)
				_skills.Remove(skillId);
			else
				_skills[skillId] = level;
		}

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

		/// <summary>
		/// rAthena's <c>MAX_GUILDPOSITION</c>. Slot 0 is permanently the
		/// guild master; slot 8 ("the lowest title") is the default for
		/// fresh members. Slots between are editable via
		/// <c>CZ_REG_CHANGE_GUILD_POSITIONINFO</c>.
		/// </summary>
		public const int PositionCount = 9;

		/// <summary>
		/// Editable position roster (in-memory only — same scope note as
		/// allies / antagonists). Indexed 0..<see cref="PositionCount"/>-1.
		/// Master is always position 0; new members default to position 8.
		/// </summary>
		public GuildPosition[] Positions { get; } = CreateDefaultPositions();

		private static GuildPosition[] CreateDefaultPositions()
		{
			var arr = new GuildPosition[PositionCount];
			arr[0] = new GuildPosition { Mode = 0x11, Ranking = 0, PayRate = 0, Name = "GuildMaster" };
			for (var i = 1; i < PositionCount - 1; i++)
				arr[i] = new GuildPosition { Mode = 0, Ranking = i, PayRate = 0, Name = "Position " + i };
			arr[PositionCount - 1] = new GuildPosition { Mode = 0, Ranking = PositionCount - 1, PayRate = 0, Name = "Newbie" };
			return arr;
		}
	}

	/// <summary>
	/// One editable rank/position slot. Mirrors rA's <c>guild_position</c>
	/// struct (mode = invite/expel/punish bitfield, payRate = treasury
	/// payout %, ranking = display order).
	/// </summary>
	public struct GuildPosition
	{
		public int Mode;
		public int Ranking;
		public int PayRate;
		public string Name;
	}

	public class GuildMember
	{
		public int CharacterId { get; }
		public string Name { get; set; }
		public bool IsMaster { get; set; }
		public string Position { get; set; } = string.Empty;
		public PlayerCharacter Player { get; set; }

		/// <summary>
		/// Position slot index (0..<see cref="Guild.PositionCount"/>-1).
		/// Master is 0; new members default to the bottom slot. In-memory
		/// only today — see Guild.Positions.
		/// </summary>
		public int PositionId { get; set; } = Guild.PositionCount - 1;

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
			this.PositionId = isMaster ? 0 : Guild.PositionCount - 1;
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
			this.PositionId = isMaster ? 0 : Guild.PositionCount - 1;
		}
	}
}
