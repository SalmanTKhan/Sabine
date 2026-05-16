using System.Collections.Generic;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Shared.Network.Helpers;
using Sabine.Zone.Network.Helpers;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Groups;
using Sabine.Zone.World.Maps;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Additional packet senders for missing packets.
	/// </summary>
	public static partial class Send
	{
		/// <summary>
		/// Sends the full party roster to a member. Mirrors eAthena's
		/// <c>clif_party_info</c> (op 0x00FB / 0x0096 alpha).
		/// </summary>
		public static void ZC_GROUP_LIST(PlayerCharacter character, Party party)
		{
			using var packet = Packet.Rent(Op.ZC_GROUP_LIST);

			packet.PutString(party.Name, 24);

			foreach (var member in party.Members)
			{
				packet.PutInt(member.AccountId);
				packet.PutString(member.Name, 24);
				packet.PutString(member.MapName ?? "", 16);
				// 0 = leader, 1 = member; matches eAthena's flipped sense.
				packet.PutByte((byte)(member.IsLeader ? 0 : 1));
				packet.PutByte((byte)(member.IsOnline ? 0 : 1));
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies a member that another member left or was kicked.
		/// </summary>
		/// <param name="flag">0 = withdrew, 1 = expelled.</param>
		public static void ZC_DELETE_MEMBER_FROM_GROUP(PlayerCharacter character, PartyMember member, byte flag)
		{
			using var packet = Packet.Rent(Op.ZC_DELETE_MEMBER_FROM_GROUP);

			packet.PutInt(member.AccountId);
			packet.PutString(member.Name, 24);
			packet.PutByte(flag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Pops the invite dialog on the invitee's screen, telling
		/// them which party invited them. At S2500+ also fires the
		/// EP12 variant so the chat-command path lights up the
		/// modern party UI.
		/// </summary>
		public static void ZC_REQ_JOIN_GROUP(PlayerCharacter character, int inviterAccountId, string partyName)
		{
			using var packet = Packet.Rent(Op.ZC_REQ_JOIN_GROUP);

			packet.PutInt(inviterAccountId);
			packet.PutString(partyName, 24);

			character.Connection.Send(packet);

			// EP12 clients render the popup via 0x02C6 instead of
			// 0x00FE. The pending-invite registry has the inviter's
			// party id (RegisterInvite ran before this call); use
			// that so /invite and the EP12 client UI behave alike.
			if (Game.Version >= Versions.S2500
				&& PartyManager.Instance.TryGetPendingInvite(character.Id, out var invite))
			{
				ZC_PARTY_JOIN_REQ(character, invite.PartyId, partyName);
			}
		}

		/// <summary>
		/// Reports the result of an invite back to the inviter. At
		/// S2500+ also fires the EP12 ack variant.
		/// </summary>
		/// <param name="result">
		/// 0 = already in a party, 1 = rejected, 2 = accepted,
		/// 3 = party full, 4 = same name (leader-only / not allowed).
		/// </param>
		public static void ZC_ACK_REQ_JOIN_GROUP(PlayerCharacter character, string targetName, byte result)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REQ_JOIN_GROUP);

			packet.PutString(targetName, 24);
			packet.PutByte(result);

			character.Connection.Send(packet);

			if (Game.Version >= Versions.S2500)
				ZC_PARTY_JOIN_REQ_ACK(character, targetName, result);
		}

		/// <summary>
		/// sakEP12+ invite popup. Carries the party id (used by the
		/// matching <c>CZ_PARTY_JOIN_REQ_ACK</c>) and the party name.
		/// </summary>
		public static void ZC_PARTY_JOIN_REQ(PlayerCharacter character, int partyId, string partyName)
		{
			using var packet = Packet.Rent(Op.ZC_PARTY_JOIN_REQ);

			packet.PutInt(partyId);
			packet.PutString(partyName, 24);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// sakEP12+ invite reply back to the inviter. The result
		/// codes mirror the older <c>ZC_ACK_REQ_JOIN_GROUP</c> but
		/// the field is widened to an int to match the wire size.
		/// </summary>
		public static void ZC_PARTY_JOIN_REQ_ACK(PlayerCharacter character, string targetName, int result)
		{
			using var packet = Packet.Rent(Op.ZC_PARTY_JOIN_REQ_ACK);

			packet.PutString(targetName, 24);
			packet.PutInt(result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// sakEP12+ "party config" toggle ack. Single-byte payload
		/// echoes the flag the client sent.
		/// </summary>
		public static void ZC_PARTY_CONFIG(PlayerCharacter character, byte expShareFlag)
		{
			using var packet = Packet.Rent(Op.ZC_PARTY_CONFIG);

			packet.PutByte(expShareFlag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies clients about a change in party settings or status.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="reason">e.g., 4 for disbanded</param>
		public static void ZC_GROUPINFO_CHANGE(PlayerCharacter character, byte reason)
		{
			using var packet = Packet.Rent(Op.ZC_GROUPINFO_CHANGE);

			packet.PutByte(reason);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends party chat message to character.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="senderId"></param>
		/// <param name="message"></param>
		public static void ZC_NOTIFY_CHAT_PARTY(PlayerCharacter character, int senderId, string message)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_CHAT_PARTY);

			packet.PutInt(senderId);
			packet.PutString(message);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the result of a warp point save request.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="result">0 = success</param>
		public static void ZC_ACK_REMEMBER_WARPPOINT(PlayerCharacter character, byte result)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REMEMBER_WARPPOINT);
			packet.PutByte(result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends list of available warp points.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skillId"></param>
		/// <param name="mapNames"></param>
		public static void ZC_WARPLIST(PlayerCharacter character, SkillId skillId, List<string> mapNames)
		{
			using var packet = Packet.Rent(Op.ZC_WARPLIST);

			if (Game.Version >= Versions.Sabine)
			{
				// rA PACKET_ZC_WARPLIST at PACKETVER_MAIN_NUM >= 20170502
				// uses dynamic length: packetLength + skillId +
				// PACKET_ZC_WARPLIST_sub maps[]. Per-entry = MAP_NAME_LENGTH_EXT
				// (16). Wire = 2 type + 2 length + 2 skillId + N×16.
				// Ref: rathena packets_struct.hpp:3613.
				packet.PutShort((short)(2 + 2 + 2 + mapNames.Count * 16)); // packetLength
				packet.PutShort((short)skillId);
				foreach (var mapName in mapNames)
					packet.PutString(mapName, 16);
			}
			else if (Game.Version >= Versions.Beta2)
			{
				// Newer versions use dynamic packet
				packet.PutShort((short)skillId);

				foreach (var mapName in mapNames)
				{
					packet.PutString(mapName, 16);
				}
			}
			else
			{
				// Alpha version has fixed 4 slots
				packet.PutShort((short)skillId);

				for (var i = 0; i < 4; i++)
				{
					if (i < mapNames.Count)
						packet.PutString(mapNames[i], 16);
					else
						packet.PutString("", 16);
				}
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the result of an item identification request.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="index"></param>
		/// <param name="result">0 = success</param>
		public static void ZC_ACK_ITEMIDENTIFY(PlayerCharacter character, int index, byte result)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_ITEMIDENTIFY);

			packet.PutShort((short)index);
			packet.PutByte(result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends target character's equipment information.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="target"></param>
		public static void ZC_ACK_OPEN_MEMBER_INFO(PlayerCharacter character, PlayerCharacter target)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_OPEN_MEMBER_INFO);

			// Basic character info
			packet.PutInt(target.Id);

			// Send equipped items
			var equippedItems = target.Inventory.GetEquippedItems();
			foreach (var item in equippedItems)
			{
				packet.PutShort((short)item.InventoryId);
				packet.PutShort((short)item.ClassId);
				// Add more item details as needed
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies character of skill usage.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="sourceId"></param>
		/// <param name="targetId"></param>
		/// <param name="skillId"></param>
		/// <param name="level"></param>
		/// <param name="delay"></param>
		public static void ZC_USESKILL_ACK(Character character, int sourceId, int targetId, SkillId skillId, int level, int delay)
		{
			using var packet = Packet.Rent(Op.ZC_USESKILL_ACK);

			packet.PutInt(sourceId);
			packet.PutInt(targetId);
			packet.PutShort(0); // x position (for ground skills)
			packet.PutShort(0); // y position
			packet.PutShort((short)skillId);

			if (Game.Version >= Versions.Beta1)
			{
				packet.PutInt(0); // element
			}

			packet.PutInt(delay);

			if (Game.Version >= Versions.Beta1)
			{
				packet.PutByte(1); // disposable
			}

			if (Game.Version >= Versions.Sabine)
			{
				// rA PACKET_ZC_USESKILL_ACK 0x0b1a at PACKETVER_MAIN_NUM
				// >= 20181212 appends uint32 attackMT (attack motion
				// timer) — used by the modern client to keep cast bar
				// scaling in sync with the actor animation.
				// Body = 4 srcId + 4 dstId + 2 x + 2 y + 2 skillId + 4
				// element + 4 delayTime + 1 disposable + 4 attackMT = 27,
				// wire = 29.
				// Ref: rathena packets_struct.hpp:3952.
				packet.PutInt(delay);
			}

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Notifies about a ground-targeted skill.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skillId"></param>
		/// <param name="sourceId"></param>
		/// <param name="level"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="startTime"></param>
		public static void ZC_NOTIFY_GROUNDSKILL(Character character, SkillId skillId, int sourceId, int level, int x, int y, int startTime)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_GROUNDSKILL);

			packet.PutShort((short)skillId);
			packet.PutInt(sourceId);
			packet.PutShort((short)level);
			packet.PutShort((short)x);
			packet.PutShort((short)y);
			packet.PutInt(startTime);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Renders the cooldown sweep on a hotbar slot (S2500+).
		/// </summary>
		/// <remarks>
		/// Reference: eA <c>src/map/clif.c</c> + <c>db/packet_db.txt:1283</c>.
		/// Fixed 8 bytes: W skill_id + L delay_ms.
		/// </remarks>
		public static void ZC_SKILL_POSTDELAY(PlayerCharacter character, SkillId skillId, int delayMs)
		{
			using var packet = Packet.Rent(Op.ZC_SKILL_POSTDELAY);
			packet.PutShort((short)skillId);
			packet.PutInt(delayMs);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends storage item count information.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="currentAmount"></param>
		/// <param name="maxAmount"></param>
		public static void ZC_NOTIFY_STOREITEM_COUNTINFO(PlayerCharacter character, int currentAmount, int maxAmount)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_STOREITEM_COUNTINFO);

			packet.PutShort((short)currentAmount);
			packet.PutShort((short)maxAmount);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Adds an item to storage display.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="item"></param>
		public static void ZC_ADD_ITEM_TO_STORE(PlayerCharacter character, Item item)
		{
			using var packet = Packet.Rent(Op.ZC_ADD_ITEM_TO_STORE);

			if (Game.Version >= Versions.Sabine)
			{
				// rA PACKET_ZC_ADD_ITEM_TO_STORE 0x0a0a at PACKETVER_MAIN_NUM
				// >= 20181121. Wire = 49.
				// Ref: rathena packets_struct.hpp:2386.
				packet.AddCartStoreItemV6(item);
				character.Connection.Send(packet);
				return;
			}

			packet.PutShort((short)item.InventoryId);
			packet.PutInt(item.Amount);
			packet.PutShort((short)item.ClassId);
			packet.PutByte((byte)item.Type);
			packet.PutByte(item.IsIdentified);
			packet.PutByte(0); // damaged

			if (Game.Version >= Versions.Beta1)
			{
				packet.PutByte(0); // refine
								   // Add card slots
				for (var i = 0; i < 4; i++)
					packet.PutShort(0);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Removes an item from storage display.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="index"></param>
		/// <param name="amount"></param>
		public static void ZC_DELETE_ITEM_FROM_STORE(PlayerCharacter character, int index, int amount)
		{
			using var packet = Packet.Rent(Op.ZC_DELETE_ITEM_FROM_STORE);

			packet.PutShort((short)index);
			packet.PutInt(amount);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends party member position update.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="memberId"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public static void ZC_NOTIFY_POSITION_TO_GROUPM(PlayerCharacter character, int memberId, int x, int y)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_POSITION_TO_GROUPM);

			packet.PutInt(memberId);
			packet.PutShort((short)x);
			packet.PutShort((short)y);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends party member HP update.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="memberId"></param>
		/// <param name="hp"></param>
		/// <param name="maxHp"></param>
		public static void ZC_NOTIFY_HP_TO_GROUPM(PlayerCharacter character, int memberId, int hp, int maxHp)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_HP_TO_GROUPM);

			packet.PutInt(memberId);

			if (Game.Version >= Versions.Beta1)
			{
				packet.PutInt(hp);
				packet.PutInt(maxHp);
			}
			else
			{
				packet.PutShort((short)hp);
				packet.PutShort((short)maxHp);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies about skill entry (ground skill effect).
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skillId"></param>
		/// <param name="sourceId"></param>
		/// <param name="level"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public static void ZC_SKILL_ENTRY(Character caster, Character target, SkillId skillId, int level, int x, int y)
		{
			using var packet = Packet.Rent(Op.ZC_SKILL_ENTRY);

			if (Game.Version >= Versions.Beta2)
			{
				packet.PutShort(0); // packet length (will be set automatically)
			}

			packet.PutInt(target.Handle);
			packet.PutInt(caster.Handle);
			packet.PutShort((short)x);
			packet.PutShort((short)y);

			if (Game.Version >= Versions.Beta2)
			{
				packet.PutInt((int)skillId);
			}
			else
			{
				packet.PutByte((byte)skillId);
			}

			if (Game.Version >= Versions.Beta2)
			{
				packet.PutByte(0); // radius range
			}

			packet.PutByte(1); // visible

			if (Game.Version >= Versions.S350)
			{
				packet.PutByte((byte)level);
			}

			caster.Map.Broadcast(packet, caster, BroadcastTargets.All);
		}

		/// <summary>
		/// Notifies about skill disappearing.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skillObjectId"></param>
		public static void ZC_SKILL_DISAPPEAR(Character character, int skillObjectId)
		{
			using var packet = Packet.Rent(Op.ZC_SKILL_DISAPPEAR);

			packet.PutInt(skillObjectId);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}
	}
}
