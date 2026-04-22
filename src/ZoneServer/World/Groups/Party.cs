using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sabine.Shared;
using Sabine.Shared.Network;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Groups
{
	public enum PartyExpShareOption
	{
		EvenShare,
		// Other options can be added here
	}

	public class Party
	{
		private static int _nextId = 1;

		public int Id { get; }
		public string Name { get; set; }

		private readonly List<PartyMember> _members = new();
		public IReadOnlyList<PartyMember> Members => _members.AsReadOnly();

		public PartyExpShareOption ExpShareOption { get; set; } = PartyExpShareOption.EvenShare;

		public Party(PlayerCharacter leader, string name)
		{
			this.Id = _nextId++;
			this.Name = name;

			var leaderMember = new PartyMember(leader, true);
			_members.Add(leaderMember);
			leader.Party = this;
		}

		public PartyMember GetLeader() => _members.FirstOrDefault(m => m.IsLeader);

		public PartyMember GetMember(int characterId) => _members.FirstOrDefault(m => m.CharacterId == characterId);

		public bool AddMember(PlayerCharacter character)
		{
			if (_members.Count >= ZoneServer.Instance.Conf.World.MaxPartySize || GetMember(character.Id) != null)
			{
				return false;
			}

			var newMember = new PartyMember(character);
			_members.Add(newMember);
			character.Party = this;

			UpdateAllClients();
			return true;
		}

		public void RemoveMember(int characterId)
		{
			var member = this.GetMember(characterId);
			if (member == null) return;

			foreach (var otherMember in _members)
			{
				if (otherMember.IsOnline)
					Send.ZC_DELETE_MEMBER_FROM_GROUP(otherMember.Player, member);
			}

			_members.Remove(member);
			member.Player.Party = null;

			// If the leader left, assign a new leader
			if (member.IsLeader && _members.Count > 0)
			{
				_members.First().IsLeader = true;
			}

			// If party is too small, disband
			if (_members.Count < 2)
			{
				this.Disband();
			}
			else
			{
				this.UpdateAllClients();
			}
		}

		public void Disband()
		{
			foreach (var member in _members.ToList())
			{
				if (member.IsOnline)
				{
					// Using a generic "party disbanded" message packet
					Send.ZC_GROUPINFO_CHANGE(member.Player, 4); // 4 = Party has been disbanded
					member.Player.Party = null;
				}
			}
			_members.Clear();
			PartyManager.Instance.RemoveParty(this.Id);
		}

		public void MemberOnline(PlayerCharacter character)
		{
			var member = GetMember(character.Id);
			if (member == null) return;

			member.SetOnline(character);
			character.Party = this;
			UpdateAllClients();
		}

		public void MemberOffline(PlayerCharacter character)
		{
			var member = GetMember(character.Id);
			member?.SetOffline();
			this.UpdateAllClients();
		}

		public void UpdateAllClients()
		{
			foreach (var member in _members)
			{
				if (member.IsOnline)
				{
					Send.ZC_GROUP_LIST(member.Player, this);
				}
			}
		}

		public void Broadcast(Packet packet)
		{
			foreach (var member in _members)
			{
				member.Player?.Connection.Send(packet);
			}
		}
	}
}
