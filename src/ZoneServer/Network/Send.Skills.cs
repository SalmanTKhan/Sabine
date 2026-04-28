using System.Collections.Generic;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Shared.Network.Helpers;
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
		/// Sends the full party list to a member.
		/// </summary>
		public static void ZC_GROUP_LIST(PlayerCharacter character, Party party)
		{
			using var packet = Packet.Rent(Op.ZC_GROUP_LIST);

			packet.PutString(party.Name, 24);

			var memberCount = party.Members.Count;
			packet.PutByte((byte)memberCount);

			// This structure is based on older clients. It might need adjustment.
			foreach (var member in party.Members)
			{
				packet.PutInt(member.AccountId);
				packet.PutString(member.Name, 24);
				packet.PutString("mapname", 16); // Placeholder, need to resolve map name
				packet.PutByte((byte)(member.IsLeader ? 1 : 0)); // Role or isLeader
			}

			character.Connection.Send(packet);
		}

		public static void ZC_DELETE_MEMBER_FROM_GROUP(PlayerCharacter character, PartyMember member)
		{
			using var packet = Packet.Rent(Op.ZC_DELETE_MEMBER_FROM_GROUP);

			packet.PutInt(member.AccountId);
			packet.PutString(member.Name, 24);

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

			if (Game.Version >= Versions.Beta2)
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
