using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Groups
{
	public class PartyMember
	{
		public int AccountId { get; }
		public int CharacterId { get; }
		public string Name { get; }
		public bool IsLeader { get; set; }

		/// <summary>
		/// A direct reference to the PlayerCharacter object. Null if offline.
		/// </summary>
		public PlayerCharacter Player { get; private set; }

		public bool IsOnline => this.Player != null;

		public JobId JobId { get; private set; }
		public int BaseLevel { get; private set; }
		public int MapId { get; private set; }

		public PartyMember(PlayerCharacter character, bool isLeader = false)
		{
			AccountId = character.Connection.Account.Id;
			CharacterId = character.Id;
			Name = character.Name;
			IsLeader = isLeader;

			SetOnline(character);
		}

		public void SetOnline(PlayerCharacter character)
		{
			Player = character;
			UpdateStatus();
		}

		public void SetOffline()
		{
			// Keep last known status but remove the direct reference
			UpdateStatus();
			Player = null;
		}

		private void UpdateStatus()
		{
			if (Player == null) return;

			JobId = Player.JobId;
			BaseLevel = Player.Parameters.BaseLevel;
			MapId = Player.Map.Id;
		}
	}
}
