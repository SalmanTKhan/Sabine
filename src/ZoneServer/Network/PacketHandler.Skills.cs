using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Zone.Skills;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Groups;
using Yggdrasil.Logging;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Additional packet handler methods for missing packets.
	/// </summary>
	public partial class PacketHandler : PacketHandler<ZoneConnection>
	{
		/// <summary>
		/// Request to warp to a saved warp point.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_SELECT_WARPPOINT)]
		public void CZ_SELECT_WARPPOINT(ZoneConnection conn, Packet packet)
		{
			var skillId = (SkillId)packet.GetShort();
			var mapName = packet.GetString(16);

			var character = conn.GetCurrentCharacter();

			// Validate the warp request
			if (!character.Skills.Has(skillId))
			{
				Log.Debug("CZ_SELECT_WARPPOINT: Character '{0}' doesn't have warp skill.", character.Name);
				return;
			}

			// Check if the map name is valid and character has saved this location
			// (Full implementation would check saved warp points)

			Log.Debug("CZ_SELECT_WARPPOINT: Character '{0}' requesting warp to '{1}'", character.Name, mapName);
		}

		/// <summary>
		/// Request to remember current position as warp point.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_REMEMBER_WARPPOINT)]
		public void CZ_REMEMBER_WARPPOINT(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();

			// Save the current location as a warp point
			// (Full implementation would save to character's saved locations)

			var success = true; // Placeholder
			Send.ZC_ACK_REMEMBER_WARPPOINT(character, success ? (byte)0 : (byte)1);

			Log.Debug("CZ_REMEMBER_WARPPOINT: Character '{0}' saved warp point at ({1}, {2})",
				character.Name, character.Position.X, character.Position.Y);
		}

		/// <summary>
		/// Request to move an item from inventory into account storage.
		/// </summary>
		[PacketHandler(Op.CZ_MOVE_ITEM_FROM_BODY_TO_STORE)]
		public void CZ_MOVE_ITEM_FROM_BODY_TO_STORE(ZoneConnection conn, Packet packet)
		{
			var itemIndex = packet.GetShort();
			var amount = packet.GetInt();

			var character = conn.GetCurrentCharacter();
			if (!character.Storage.IsOpen)
				return;

			var item = character.Inventory.GetItem(itemIndex);
			if (item == null)
				return;

			if (amount <= 0 || amount > item.Amount)
				return;

			if (item.IsEquipped)
				return;

			var added = character.Storage.AddFrom(item, amount);
			if (added == null)
				return;

			Send.ZC_ADD_ITEM_TO_STORE(character, added);
			Send.ZC_NOTIFY_STOREITEM_COUNTINFO(character, character.Storage.ItemCount, Sabine.Zone.World.Actors.Components.Characters.Storage.MaxSlots);

			character.Inventory.DecrementItem(item, amount);
		}

		/// <summary>
		/// Request to move an item from account storage back to inventory.
		/// </summary>
		[PacketHandler(Op.CZ_MOVE_ITEM_FROM_STORE_TO_BODY)]
		public void CZ_MOVE_ITEM_FROM_STORE_TO_BODY(ZoneConnection conn, Packet packet)
		{
			var itemIndex = packet.GetShort();
			var amount = packet.GetInt();

			var character = conn.GetCurrentCharacter();
			if (!character.Storage.IsOpen)
				return;

			if (amount <= 0)
				return;

			var entry = character.Storage.GetItem(itemIndex);
			if (entry == null)
				return;

			var weightAdd = entry.Data.Weight * amount;
			if (character.Parameters.Weight + weightAdd > character.Parameters.WeightMax)
				return;

			// Slot guard for novel stackables / each non-stackable unit;
			// without this a withdraw could push body over MAX_INVENTORY.
			var needsSlot = entry.Data.Type.IsEquip()
				? amount
				: (character.Inventory.Contains(entry.Data.ClassId) ? 0 : 1);

			if (character.Inventory.ItemCount + needsSlot > Sabine.Zone.World.Actors.Components.Characters.Inventory.MaxSlots)
				return;

			var detached = character.Storage.RemoveByIndex(itemIndex, amount);
			if (detached == null)
				return;

			Send.ZC_DELETE_ITEM_FROM_STORE(character, itemIndex, amount);
			character.Inventory.AddItem(detached);
		}

		/// <summary>
		/// Request party chat. Broadcasts the message to every online
		/// member, including the sender (so the client sees the
		/// echo).
		/// </summary>
		[PacketHandler(Op.CZ_REQUEST_CHAT_PARTY)]
		public void CZ_REQUEST_CHAT_PARTY(ZoneConnection conn, Packet packet)
		{
			var len = packet.GetShort();
			var message = packet.GetString(len - 4);

			var character = conn.GetCurrentCharacter();
			if (character.Party == null)
				return;

			var senderId = conn.Account.Id;
			foreach (var member in character.Party.Members)
			{
				if (member.IsOnline)
					Send.ZC_NOTIFY_CHAT_PARTY(member.Player, senderId, message);
			}
		}

		/// <summary>
		/// Request to identify an item.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_REQ_ITEMIDENTIFY)]
		public void CZ_REQ_ITEMIDENTIFY(ZoneConnection conn, Packet packet)
		{
			var itemIndex = packet.GetShort();

			var character = conn.GetCurrentCharacter();
			var item = character.Inventory.GetItem(itemIndex);

			if (item == null)
			{
				Log.Debug("CZ_REQ_ITEMIDENTIFY: Character '{0}' tried to identify non-existent item.", character.Name);
				return;
			}

			// Identify the item
			item.IsIdentified = true;

			Send.ZC_ACK_ITEMIDENTIFY(character, itemIndex, 0); // Success

			Log.Debug("CZ_REQ_ITEMIDENTIFY: Character '{0}' identified item {1}", character.Name, item.ClassId);
		}

		/// <summary>
		/// Request to view another character's equipment.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_REQ_OPEN_MEMBER_INFO)]
		public void CZ_REQ_OPEN_MEMBER_INFO(ZoneConnection conn, Packet packet)
		{
			var targetId = packet.GetInt();

			var character = conn.GetCurrentCharacter();
			var target = character.Map.GetCharacter(targetId);

			if (target is not PlayerCharacter targetPlayer)
			{
				Log.Debug("CZ_REQ_OPEN_MEMBER_INFO: Character '{0}' tried to view invalid target.", character.Name);
				return;
			}

			// Send target's equipment info
			Send.ZC_ACK_OPEN_MEMBER_INFO(character, targetPlayer);
		}

		/// <summary>
		/// Request to change party EXP / item sharing options. Sent
		/// by Beta1+ clients only; the alpha client has no UI for
		/// this.
		/// </summary>
		[PacketHandler(Op.CZ_CHANGE_GROUPEXPOPTION)]
		public void CZ_CHANGE_GROUPEXPOPTION(ZoneConnection conn, Packet packet)
		{
			var expOption = packet.GetInt();

			var character = conn.GetCurrentCharacter();
			var party = character.Party;
			if (party == null)
				return;

			// Only the leader may change party options. Silently drop
			// otherwise; eAthena does the same.
			if (!party.IsLeader(character))
				return;

			party.ExpShareOption = expOption == 0
				? PartyExpShareOption.Individual
				: PartyExpShareOption.EvenShare;

			ZoneServer.Instance.Database.UpdatePartyOptions(party.Id,
				party.ExpShareOption == PartyExpShareOption.EvenShare,
				party.ItemShareOption == PartyItemShareOption.EvenShare);

			party.UpdateAllClients();
		}

		/// <summary>
		/// Request to reset stat points.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_RESET)]
		public void CZ_RESET(ZoneConnection conn, Packet packet)
		{
			var resetType = packet.GetShort();

			var character = conn.GetCurrentCharacter();

			// Perform reset based on type
			// 0 = stat reset, 1 = skill reset
			switch (resetType)
			{
				case 0:
					// Reset stats (requires item or NPC support)
					Log.Debug("CZ_RESET: Character '{0}' requesting stat reset", character.Name);
					break;
				case 1:
					// Reset skills (requires item or NPC support)
					Log.Debug("CZ_RESET: Character '{0}' requesting skill reset", character.Name);
					break;
			}
		}

		/// <summary>
		/// Request to change map type (for GM commands).
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_CHANGE_MAPTYPE)]
		public void CZ_CHANGE_MAPTYPE(ZoneConnection conn, Packet packet)
		{
			var x = packet.GetShort();
			var y = packet.GetShort();
			var mapType = packet.GetShort();

			var character = conn.GetCurrentCharacter();

			// Check GM permissions
			// TODO: Add GM level check
			// if (character.Account.GMLevel < 1) return;

			Log.Debug("CZ_CHANGE_MAPTYPE: GM '{0}' changing map type at ({1},{2}) to {3}",
				character.Name, x, y, mapType);
		}

		/// <summary>
		/// Request to expel a member from the party. Leader-only.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_EXPEL_GROUP_MEMBER)]
		public void CZ_REQ_EXPEL_GROUP_MEMBER(ZoneConnection conn, Packet packet)
		{
			var accountId = packet.GetInt();
			var characterName = packet.GetString(24).TrimEnd('\0');

			var character = conn.GetCurrentCharacter();
			var party = character.Party;
			if (party == null || !party.IsLeader(character))
				return;

			var target = party.GetMemberByAccountId(accountId)
				?? party.GetMemberByName(characterName);
			if (target == null || target.CharacterId == character.Id)
				return;

			party.RemoveMember(target.CharacterId, expelled: true);
		}

		/// <summary>
		/// Invite request from a party leader. Carries the invitee's
		/// account id; the leader's client picked it from the local
		/// actor list. Validates leader-only / not-full / target-not
		/// -in-party rules, then pops the invite dialog on the
		/// invitee's screen.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_JOIN_GROUP)]
		public void CZ_REQ_JOIN_GROUP(ZoneConnection conn, Packet packet)
		{
			var targetAccountId = packet.GetInt();

			var character = conn.GetCurrentCharacter();
			var party = character.Party;

			// Not in a party — eAthena ignores; we do too. Players
			// can't invite without a party first.
			if (party == null || !party.IsLeader(character))
				return;

			if (!ZoneServer.Instance.World.Maps.TryGetPlayerByAccountId(targetAccountId, out var target))
				return;

			if (target.Party != null)
			{
				Send.ZC_ACK_REQ_JOIN_GROUP(character, target.Name, 0); // already in party
				return;
			}

			if (party.IsFull())
			{
				Send.ZC_ACK_REQ_JOIN_GROUP(character, target.Name, 3); // full
				return;
			}

			PartyManager.Instance.RegisterInvite(character, target);
			Send.ZC_REQ_JOIN_GROUP(target, conn.Account.Id, party.Name);
		}

		/// <summary>
		/// Reply to a pending invite. Flag is 1 = accept, 0 = reject.
		/// </summary>
		[PacketHandler(Op.CZ_JOIN_GROUP)]
		public void CZ_JOIN_GROUP(ZoneConnection conn, Packet packet)
		{
			var inviterAccountId = packet.GetInt();
			var flag = packet.GetInt();

			var character = conn.GetCurrentCharacter();

			if (!PartyManager.Instance.ConsumePendingInvite(character.Id, out var invite))
				return;

			var party = PartyManager.Instance.GetParty(invite.PartyId);
			if (party == null)
				return;

			ZoneServer.Instance.World.Maps.TryGetPlayerByAccountId(inviterAccountId, out var inviter);

			if (flag != 1)
			{
				if (inviter != null)
					Send.ZC_ACK_REQ_JOIN_GROUP(inviter, character.Name, 1); // rejected
				return;
			}

			// Re-validate before binding: the invitee may have joined
			// another party while the popup was open.
			if (character.Party != null || party.IsFull())
			{
				if (inviter != null)
					Send.ZC_ACK_REQ_JOIN_GROUP(inviter, character.Name, 0);
				return;
			}

			if (!party.AddMember(character))
				return;

			if (inviter != null)
				Send.ZC_ACK_REQ_JOIN_GROUP(inviter, character.Name, 2); // accepted
		}

		/// <summary>
		/// sakEP12+ invite request. Carries the invitee's character
		/// name (newer clients dropped the account-id flow). Same
		/// validation rules as the legacy <see cref="CZ_REQ_JOIN_GROUP"/>.
		/// </summary>
		[PacketHandler(Op.CZ_PARTY_JOIN_REQ)]
		public void CZ_PARTY_JOIN_REQ(ZoneConnection conn, Packet packet)
		{
			var targetName = packet.GetString(24).TrimEnd('\0');

			var character = conn.GetCurrentCharacter();
			var party = character.Party;
			if (party == null || !party.IsLeader(character))
				return;

			if (!ZoneServer.Instance.World.Maps.TryGetPlayerByName(targetName, out var target))
				return;

			if (target.Party != null)
			{
				Send.ZC_PARTY_JOIN_REQ_ACK(character, targetName, 0);
				return;
			}

			if (party.IsFull())
			{
				Send.ZC_PARTY_JOIN_REQ_ACK(character, targetName, 3);
				return;
			}

			PartyManager.Instance.RegisterInvite(character, target);
			Send.ZC_PARTY_JOIN_REQ(target, party.Id, party.Name);
		}

		/// <summary>
		/// sakEP12+ reply to a pending invite. Carries the party id
		/// the popup was offering and a single-byte accept/reject
		/// flag (1 = accept, 0 = reject).
		/// </summary>
		[PacketHandler(Op.CZ_PARTY_JOIN_REQ_ACK)]
		public void CZ_PARTY_JOIN_REQ_ACK(ZoneConnection conn, Packet packet)
		{
			var partyId = packet.GetInt();
			var flag = packet.GetByte();

			var character = conn.GetCurrentCharacter();

			if (!PartyManager.Instance.ConsumePendingInvite(character.Id, out var invite))
				return;

			// The popup carries a party id, but a stale popup might
			// reference a long-gone party. Fall back to the invite's
			// recorded party id and validate the popup matches.
			if (invite.PartyId != partyId)
				return;

			var party = PartyManager.Instance.GetParty(invite.PartyId);
			if (party == null)
				return;

			ZoneServer.Instance.World.Maps.TryGetPlayerById(invite.InviterCharacterId, out var inviter);

			if (flag != 1)
			{
				if (inviter != null)
					Send.ZC_PARTY_JOIN_REQ_ACK(inviter, character.Name, 1);
				return;
			}

			if (character.Party != null || party.IsFull())
			{
				if (inviter != null)
					Send.ZC_PARTY_JOIN_REQ_ACK(inviter, character.Name, 0);
				return;
			}

			if (!party.AddMember(character))
				return;

			if (inviter != null)
				Send.ZC_PARTY_JOIN_REQ_ACK(inviter, character.Name, 2);
		}

		/// <summary>
		/// sakEP12+ party-config toggle. Replaces the older
		/// <see cref="CZ_CHANGE_GROUPEXPOPTION"/> with a single
		/// byte payload (0 = individual, 1 = even share).
		/// </summary>
		[PacketHandler(Op.CZ_PARTY_CONFIG)]
		public void CZ_PARTY_CONFIG(ZoneConnection conn, Packet packet)
		{
			var expFlag = packet.GetByte();

			var character = conn.GetCurrentCharacter();
			var party = character.Party;
			if (party == null || !party.IsLeader(character))
				return;

			party.ExpShareOption = expFlag == 0
				? PartyExpShareOption.Individual
				: PartyExpShareOption.EvenShare;

			ZoneServer.Instance.Database.UpdatePartyOptions(party.Id,
				party.ExpShareOption == PartyExpShareOption.EvenShare,
				party.ItemShareOption == PartyItemShareOption.EvenShare);

			Send.ZC_PARTY_CONFIG(character, expFlag);
			party.UpdateAllClients();
		}
	}
}
