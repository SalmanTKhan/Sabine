using System;
using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Groups
{
	/// <summary>
	/// In-memory guild record. Modeled on <see cref="Party"/>; persistence
	/// hooks are intentionally absent — Sabine's guild system is a stub
	/// for command surface only.
	/// </summary>
	public class Guild
	{
		private static int _nextId = 1;

		public int Id { get; }
		public string Name { get; set; }
		public int MasterCharId { get; set; }
		public int Level { get; set; } = 1;
		public int ExpCurrent { get; set; }
		public int EmblemId { get; set; }
		public string Notice { get; set; } = string.Empty;
		public string MasterName { get; set; }

		private readonly List<GuildMember> _members = new();
		public IReadOnlyList<GuildMember> Members => _members.AsReadOnly();

		private readonly HashSet<int> _allies = new();
		public IReadOnlyCollection<int> Allies => _allies;

		private readonly HashSet<int> _antagonists = new();
		public IReadOnlyCollection<int> Antagonists => _antagonists;

		public Guild(PlayerCharacter master, string name)
		{
			this.Id = _nextId++;
			this.Name = name;
			this.MasterCharId = master.Id;
			this.MasterName = master.Name;
			_members.Add(new GuildMember(master, isMaster: true));
			master.Guild = this;
		}

		public GuildMember GetMember(int characterId) => _members.FirstOrDefault(m => m.CharacterId == characterId);

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

		public GuildMember(PlayerCharacter character, bool isMaster = false)
		{
			this.CharacterId = character.Id;
			this.Name = character.Name;
			this.IsMaster = isMaster;
			this.Player = character;
		}
	}
}
