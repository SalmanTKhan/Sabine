using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Zone.Skills;
using Sabine.Zone.World.Entities;
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
		/// Request to use a skill on a target.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_USE_SKILL)]
		public void CZ_USE_SKILL(ZoneConnection conn, Packet packet)
		{
			var skillLevel = packet.GetShort();
			var skillId = (SkillId)packet.GetShort();
			var targetId = packet.GetInt();

			var character = conn.GetCurrentCharacter();

			// Basic validation
			if (character.IsDead)
			{
				Log.Debug("CZ_USE_SKILL: Character '{0}' tried to use skill while dead.", character.Name);
				return;
			}

			var target = character.Map.GetCharacter(targetId);
			if (target == null)
			{
				Log.Debug("CZ_USE_SKILL: Character '{0}' tried to use skill on non-existent target.", character.Name);
				return;
			}

			_ = character.Skills.Use(skillId, skillLevel, target);
		}

		/// <summary>
		/// Request to use a skill on ground.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_USE_SKILL_TOGROUND)]
		public void CZ_USE_SKILL_TOGROUND(ZoneConnection conn, Packet packet)
		{
			var skillLevel = packet.GetShort();
			var skillId = (SkillId)packet.GetShort();
			var x = packet.GetShort();
			var y = packet.GetShort();

			var character = conn.GetCurrentCharacter();

			if (character.IsDead)
			{
				Log.Debug("CZ_USE_SKILL_TOGROUND: Character '{0}' tried to use skill while dead.", character.Name);
				return;
			}

			if (!character.Skills.Has(skillId))
			{
				Log.Debug("CZ_USE_SKILL_TOGROUND: Character '{0}' tried to use skill they don't have: {1}", character.Name, skillId);
				return;
			}

			// TODO: Use the skill on ground position
			//character.Skills.UseOnGround(skillId, skillLevel, new Position(x, y));
		}

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
		/// Request to open storage.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_MOVE_ITEM_FROM_BODY_TO_STORE)]
		public void CZ_MOVE_ITEM_FROM_BODY_TO_STORE(ZoneConnection conn, Packet packet)
		{
			var itemIndex = packet.GetShort();
			var amount = packet.GetInt();

			var character = conn.GetCurrentCharacter();
			var item = character.Inventory.GetItem(itemIndex);

			if (item == null)
			{
				Log.Debug("CZ_MOVE_ITEM_FROM_BODY_TO_STORE: Character '{0}' tried to move non-existent item.", character.Name);
				return;
			}

			if (amount <= 0 || amount > item.Amount)
			{
				Log.Debug("CZ_MOVE_ITEM_FROM_BODY_TO_STORE: Character '{0}' tried to move invalid amount.", character.Name);
				return;
			}

			// Move item to storage (placeholder - needs storage system implementation)
			Log.Debug("CZ_MOVE_ITEM_FROM_BODY_TO_STORE: Character '{0}' moving {1}x {2} to storage",
				character.Name, amount, item.ClassId);
		}

		/// <summary>
		/// Request to move item from storage to inventory.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_MOVE_ITEM_FROM_STORE_TO_BODY)]
		public void CZ_MOVE_ITEM_FROM_STORE_TO_BODY(ZoneConnection conn, Packet packet)
		{
			var itemIndex = packet.GetShort();
			var amount = packet.GetInt();

			var character = conn.GetCurrentCharacter();

			// Move item from storage (placeholder - needs storage system implementation)
			Log.Debug("CZ_MOVE_ITEM_FROM_STORE_TO_BODY: Character '{0}' moving item from storage", character.Name);
		}

		/// <summary>
		/// Request to create a party/group.
		/// </summary>
		[PacketHandler(Op.CZ_MAKE_GROUP)]
		public void CZ_MAKE_GROUP(ZoneConnection conn, Packet packet)
		{
			var partyName = packet.GetString(24).TrimEnd('\0');
			var character = conn.GetCurrentCharacter();

			if (character.Party != null)
			{
				Send.ZC_ACK_MAKE_GROUP(character, 2); // You are already in a party
				return;
			}

			if (string.IsNullOrWhiteSpace(partyName))
			{
				Send.ZC_ACK_MAKE_GROUP(character, 1); // Invalid party name
				return;
			}

			var party = PartyManager.Instance.CreateParty(character, partyName);
			if (party == null)
			{
				Send.ZC_ACK_MAKE_GROUP(character, 1); // Party name already exists
				return;
			}

			Log.Debug("CZ_MAKE_GROUP: Character '{0}' creating party '{1}'", character.Name, partyName);
			Send.ZC_ACK_MAKE_GROUP(character, 0); // Success
		}

		/// <summary>
		/// Request party chat.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_REQUEST_CHAT_PARTY)]
		public void CZ_REQUEST_CHAT_PARTY(ZoneConnection conn, Packet packet)
		{
			var len = packet.GetShort();
			var message = packet.GetString(len - 4);

			var character = conn.GetCurrentCharacter();

			// Check if character is in a party
			// (Full implementation would check party membership and broadcast)

			Log.Debug("CZ_REQUEST_CHAT_PARTY: Character '{0}' party message: {1}", character.Name, message);
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
		/// Request to change group EXP sharing option.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_CHANGE_GROUPEXPOPTION)]
		public void CZ_CHANGE_GROUPEXPOPTION(ZoneConnection conn, Packet packet)
		{
			var expOption = packet.GetInt();

			var character = conn.GetCurrentCharacter();

			// Change party EXP sharing settings
			// (Full implementation needs party system)

			Log.Debug("CZ_CHANGE_GROUPEXPOPTION: Character '{0}' changed exp option to {1}", character.Name, expOption);
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
		/// Request to leave party/group.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_REQ_LEAVE_GROUP)]
		public void CZ_REQ_LEAVE_GROUP(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();

			// Leave party (placeholder - needs party system)
			Log.Debug("CZ_REQ_LEAVE_GROUP: Character '{0}' leaving party", character.Name);
		}

		/// <summary>
		/// Request to expel member from party.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.CZ_REQ_EXPEL_GROUP_MEMBER)]
		public void CZ_REQ_EXPEL_GROUP_MEMBER(ZoneConnection conn, Packet packet)
		{
			var accountId = packet.GetInt();
			var characterName = packet.GetString(24);

			var character = conn.GetCurrentCharacter();

			// Expel member (placeholder - needs party system)
			Log.Debug("CZ_REQ_EXPEL_GROUP_MEMBER: Character '{0}' expelling '{1}' from party",
				character.Name, characterName);
		}
	}
}
