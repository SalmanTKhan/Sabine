using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sabine.Shared;
using Sabine.Shared.Network;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Groups
{
	/// <summary>
	/// EXP distribution mode for a party. Mirrors eAthena's
	/// <c>party.exp</c> bit.
	/// </summary>
	public enum PartyExpShareOption
	{
		/// <summary>Killer keeps all exp. Default.</summary>
		Individual,

		/// <summary>Even split among same-map party members.</summary>
		EvenShare,
	}

	/// <summary>
	/// Item distribution mode for a party. Mirrors eAthena's
	/// <c>party.item</c> 2-bit field, but exposed as a single enum for
	/// clarity. Round-robin and random pick-up will be implemented in
	/// the loot-share slice.
	/// </summary>
	public enum PartyItemShareOption
	{
		Individual,
		EvenShare,
	}

	/// <summary>
	/// A formed group of player characters that shares chat, position
	/// and HP updates, and (when configured) experience and loot.
	/// </summary>
	public class Party
	{
		private static int _nextId;

		private readonly object _syncRoot = new();
		private readonly List<PartyMember> _members = new();
		private int _lootCursor;

		public int Id { get; internal set; }
		public string Name { get; private set; }

		public IReadOnlyList<PartyMember> Members
		{
			get { lock (_syncRoot) return _members.ToList(); }
		}

		public PartyExpShareOption ExpShareOption { get; set; } = PartyExpShareOption.Individual;
		public PartyItemShareOption ItemShareOption { get; set; } = PartyItemShareOption.Individual;

		public Party(PlayerCharacter leader, string name)
		{
			// Id is left at 0 here; PartyManager.CreateParty assigns
			// the auto-generated DB id immediately after construction.
			this.Name = name;

			var leaderMember = new PartyMember(leader, true);
			_members.Add(leaderMember);
			leader.Party = this;
		}

		/// <summary>
		/// Reconstructs a party from a persisted row. Members are
		/// added as offline placeholders (no <c>Player</c>); the live
		/// roster is reconciled lazily as members log in via
		/// <see cref="MemberOnline"/>.
		/// </summary>
		internal Party(PartyDbRow row)
		{
			this.Id = row.PartyId;
			this.Name = row.Name;
			this.ExpShareOption = row.ExpShare
				? PartyExpShareOption.EvenShare
				: PartyExpShareOption.Individual;
			this.ItemShareOption = row.ItemShare
				? PartyItemShareOption.EvenShare
				: PartyItemShareOption.Individual;

			foreach (var m in row.Members)
				_members.Add(new PartyMember(m.CharacterId, m.Name, m.IsLeader));
		}

		public PartyMember GetLeader()
		{
			lock (_syncRoot)
				return _members.FirstOrDefault(m => m.IsLeader);
		}

		public PartyMember GetMember(int characterId)
		{
			lock (_syncRoot)
				return _members.FirstOrDefault(m => m.CharacterId == characterId);
		}

		public PartyMember GetMemberByAccountId(int accountId)
		{
			lock (_syncRoot)
				return _members.FirstOrDefault(m => m.AccountId == accountId);
		}

		public PartyMember GetMemberByName(string name)
		{
			lock (_syncRoot)
				return _members.FirstOrDefault(m => m.Name == name);
		}

		/// <summary>
		/// Returns true if <paramref name="player"/> is this party's
		/// leader. Mirrors rAthena's <c>is_party_leader()</c>.
		/// </summary>
		public bool IsLeader(PlayerCharacter player)
			=> player != null && this.GetLeader()?.CharacterId == player.Id;

		public bool IsFull()
			=> this.Members.Count >= ZoneServer.Instance.Conf.World.MaxPartySize;

		/// <summary>
		/// Returns the next round-robin index for loot distribution
		/// and advances the cursor. Mirrors eAthena's
		/// <c>party_data.itemc</c>.
		/// </summary>
		public int AdvanceLootCursor()
			=> Interlocked.Increment(ref _lootCursor);

		/// <summary>
		/// Adds <paramref name="character"/> to the party. Returns
		/// false when the party is full or the character is already a
		/// member. Broadcasts the new roster to all online members.
		/// </summary>
		public bool AddMember(PlayerCharacter character)
		{
			lock (_syncRoot)
			{
				if (_members.Count >= ZoneServer.Instance.Conf.World.MaxPartySize)
					return false;
				if (_members.Any(m => m.CharacterId == character.Id))
					return false;

				var newMember = new PartyMember(character);
				_members.Add(newMember);
				character.Party = this;
			}

			PartyManager.Instance.RegisterMember(character.Id, this);
			ZoneServer.Instance.Database.InsertPartyMember(this.Id, character.Id, false);

			this.UpdateAllClients();
			return true;
		}

		/// <summary>
		/// Removes a member by character id. Drops the party if it
		/// would shrink below two members. Promotes a new leader if the
		/// leader left.
		/// </summary>
		/// <param name="characterId">Character id of the leaver.</param>
		/// <param name="expelled">True if kicked, false if voluntary.</param>
		public void RemoveMember(int characterId, bool expelled = false)
		{
			PartyMember member;
			bool wasLeader;
			PartyMember newLeader = null;
			List<PartyMember> snapshot;

			lock (_syncRoot)
			{
				member = _members.FirstOrDefault(m => m.CharacterId == characterId);
				if (member == null)
					return;

				wasLeader = member.IsLeader;
				_members.Remove(member);
				if (member.Player != null)
					member.Player.Party = null;

				if (wasLeader && _members.Count > 0)
				{
					_members[0].IsLeader = true;
					newLeader = _members[0];
				}

				snapshot = _members.ToList();
			}

			PartyManager.Instance.UnregisterMember(characterId);
			ZoneServer.Instance.Database.DeletePartyMember(characterId);
			if (newLeader != null)
				ZoneServer.Instance.Database.UpdatePartyLeader(this.Id, newLeader.CharacterId);

			// Notify everyone (including the leaver, if still online).
			var flag = (byte)(expelled ? 1 : 0);
			foreach (var other in snapshot)
			{
				if (other.IsOnline)
					Send.ZC_DELETE_MEMBER_FROM_GROUP(other.Player, member, flag);
			}
			if (member.Player != null)
				Send.ZC_DELETE_MEMBER_FROM_GROUP(member.Player, member, flag);

			if (snapshot.Count < 2)
				this.Disband();
			else
				this.UpdateAllClients();
		}

		/// <summary>
		/// Disbands the party. eAthena sends a "party broken"
		/// notification (0x009D ZC_GROUPINFO_CHANGE) on Beta1+; the
		/// alpha client has no equivalent op, so we fall back to
		/// removing every member individually.
		/// </summary>
		public void Disband()
		{
			List<PartyMember> snapshot;
			lock (_syncRoot)
			{
				snapshot = _members.ToList();
				_members.Clear();
			}

			foreach (var m in snapshot)
			{
				if (m.Player != null)
					m.Player.Party = null;
				PartyManager.Instance.UnregisterMember(m.CharacterId);
			}

			foreach (var m in snapshot)
			{
				if (!m.IsOnline)
					continue;

				if (Game.Version >= Versions.Beta1)
					Send.ZC_GROUPINFO_CHANGE(m.Player, 4); // 4 = disbanded
				else
					Send.ZC_DELETE_MEMBER_FROM_GROUP(m.Player, m, 0);
			}

			PartyManager.Instance.RemoveParty(this.Id);
			ZoneServer.Instance.Database.DeleteParty(this.Id);
		}

		/// <summary>
		/// Transfers leadership to another member. Returns false if
		/// the target is not in the party.
		/// </summary>
		public bool ChangeLeader(int characterId)
		{
			lock (_syncRoot)
			{
				var target = _members.FirstOrDefault(m => m.CharacterId == characterId);
				if (target == null)
					return false;

				foreach (var m in _members)
					m.IsLeader = (m == target);
			}

			ZoneServer.Instance.Database.UpdatePartyLeader(this.Id, characterId);
			this.UpdateAllClients();
			return true;
		}

		public void MemberOnline(PlayerCharacter character)
		{
			var member = this.GetMember(character.Id);
			if (member == null)
				return;

			member.SetOnline(character);
			character.Party = this;
			this.UpdateAllClients();
		}

		public void MemberOffline(PlayerCharacter character)
		{
			var member = this.GetMember(character.Id);
			member?.SetOffline();
			this.UpdateAllClients();
		}

		/// <summary>
		/// Pushes the full roster to every online member. Used after
		/// any state change that could be visible in the party list.
		/// </summary>
		public void UpdateAllClients()
		{
			foreach (var member in this.Members)
			{
				if (member.IsOnline)
					Send.ZC_GROUP_LIST(member.Player, this);
			}
		}

		/// <summary>
		/// Sends a packet to every online member. Used by party chat.
		/// </summary>
		public void Broadcast(Packet packet)
		{
			foreach (var member in this.Members)
				member.Player?.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts the source's grid position to every other member
		/// on the same map. Mirrors eAthena's <c>clif_party_xy</c>.
		/// </summary>
		public void BroadcastPosition(PlayerCharacter source)
		{
			if (source?.Map == null)
				return;

			var srcMapId = source.Map.Id;
			var x = source.Position.X;
			var y = source.Position.Y;
			var srcAccountId = source.Connection.Account.Id;

			foreach (var member in this.Members)
			{
				if (!member.IsOnline || member.CharacterId == source.Id)
					continue;
				if (member.Player.Map?.Id != srcMapId)
					continue;

				Send.ZC_NOTIFY_POSITION_TO_GROUPM(member.Player, srcAccountId, x, y);
			}
		}

		/// <summary>
		/// Broadcasts the source's HP to every other member on the
		/// same map. Mirrors eAthena's <c>clif_party_hp</c>.
		/// </summary>
		public void BroadcastHp(PlayerCharacter source)
		{
			if (source?.Map == null)
				return;

			var srcMapId = source.Map.Id;
			var hp = source.Parameters.Hp;
			var maxHp = source.Parameters.HpMax;
			var srcAccountId = source.Connection.Account.Id;

			foreach (var member in this.Members)
			{
				if (!member.IsOnline || member.CharacterId == source.Id)
					continue;
				if (member.Player.Map?.Id != srcMapId)
					continue;

				Send.ZC_NOTIFY_HP_TO_GROUPM(member.Player, srcAccountId, hp, maxHp);
			}
		}
	}
}
