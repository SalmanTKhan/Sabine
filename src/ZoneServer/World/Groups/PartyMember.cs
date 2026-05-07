using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Groups
{
	/// <summary>
	/// Snapshot of a character's party-relevant state, kept fresh while
	/// the player is online. Mirrors eAthena's <c>party_member</c>.
	/// </summary>
	public class PartyMember
	{
		public int AccountId { get; private set; }
		public int CharacterId { get; }
		public string Name { get; private set; }
		public bool IsLeader { get; set; }

		/// <summary>
		/// Direct reference to the live character. Null while offline.
		/// </summary>
		public PlayerCharacter Player { get; private set; }

		public bool IsOnline => this.Player != null;

		public JobId JobId { get; private set; }
		public int BaseLevel { get; private set; }
		public int MapId { get; private set; }

		/// <summary>
		/// Last known map's string id (e.g. "prontera"). Used by the
		/// party-list packet, which carries the map name on the wire.
		/// </summary>
		public string MapName { get; private set; } = "";

		/// <summary>
		/// Constructs a member from a live character. Used for new
		/// joins.
		/// </summary>
		public PartyMember(PlayerCharacter character, bool isLeader = false)
		{
			this.AccountId = character.Connection.Account.Id;
			this.CharacterId = character.Id;
			this.Name = character.Name;
			this.IsLeader = isLeader;

			this.SetOnline(character);
		}

		/// <summary>
		/// Constructs an offline placeholder for a member loaded from
		/// the database. Live state (account id, level, map) is
		/// populated when the player logs in.
		/// </summary>
		public PartyMember(int characterId, string name, bool isLeader)
		{
			this.CharacterId = characterId;
			this.Name = name ?? string.Empty;
			this.IsLeader = isLeader;
		}

		public void SetOnline(PlayerCharacter character)
		{
			this.Player = character;
			this.AccountId = character.Connection.Account.Id;
			this.Name = character.Name;
			this.UpdateStatus();
		}

		public void SetOffline()
		{
			// Keep last-known stats; just drop the live reference.
			this.UpdateStatus();
			this.Player = null;
		}

		private void UpdateStatus()
		{
			if (this.Player == null)
				return;

			this.JobId = this.Player.JobId;
			this.BaseLevel = this.Player.Parameters.BaseLevel;
			this.MapId = this.Player.Map.Id;
			this.MapName = this.Player.Map.StringId;
		}
	}
}
