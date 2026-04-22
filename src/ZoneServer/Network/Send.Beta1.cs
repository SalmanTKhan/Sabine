using System.Collections.Generic;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Shared.Network.Helpers;
using Sabine.Shared.World;
using Sabine.Zone.Network.Helpers;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Shops;
using Yggdrasil.Util;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Packet senders for the Beta1 version.
	/// </summary>
	public static partial class Send
	{
		/// <summary>
		/// Notifies clients about a skill being used.
		/// </summary>
		public static void ZC_NOTIFY_SKILL(Character character, int targetId, SkillId skillId, int level, int damage, int attackTime, int attackedTime, ActionType action)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_SKILL);

			packet.PutShort((short)skillId);
			packet.PutInt(character.Handle);
			packet.PutInt(targetId);
			packet.PutInt(Game.GetTick());
			packet.PutInt(attackTime);
			packet.PutInt(attackedTime);
			packet.PutShort((short)damage);
			packet.PutShort((short)level);
			packet.PutShort(1); // count
			packet.PutByte((byte)action);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Notifies clients about a positional skill being used.
		/// </summary>
		public static void ZC_NOTIFY_SKILL_POSITION(Character character, int targetId, SkillId skillId, int level, Position pos, int damage, int attackTime, int attackedTime, ActionType action)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_SKILL_POSITION);

			packet.PutShort((short)skillId);
			packet.PutInt(character.Handle);
			packet.PutInt(targetId);
			packet.PutInt(Game.GetTick());
			packet.PutInt(attackTime);
			packet.PutInt(attackedTime);
			packet.PutShort((short)pos.X);
			packet.PutShort((short)pos.Y);
			packet.PutShort((short)damage);
			packet.PutShort((short)level);
			packet.PutShort(1); // count
			packet.PutByte((byte)action);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Notifies clients about a character's state change.
		/// </summary>
		public static void ZC_STATE_CHANGE(Character character)
		{
			using var packet = Packet.Rent(Op.ZC_STATE_CHANGE);

			packet.PutInt(character.Handle);
			packet.PutShort((short)character.BodyState);
			packet.PutShort((short)character.HealthState);
			packet.PutShort((short)character.EffectState);
			packet.PutByte(0); // isPKModeOn

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Acknowledges a skill use request.
		/// </summary>
		public static void ZC_USE_SKILL(PlayerCharacter character, SkillId skillId, int level, int targetId, bool result)
		{
			using var packet = Packet.Rent(Op.ZC_USE_SKILL);

			packet.PutShort((short)skillId);
			packet.PutShort((short)level);
			packet.PutInt(targetId);
			packet.PutInt(character.Handle);
			packet.PutByte(result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Creates a ground-based skill effect.
		/// Based on the Beta1 size, the structure is likely: AID (4), X (2), Y (2), SkillID (1 byte).
		/// </summary>
		public static void ZC_SKILL_ENTRY(Character character, SkillId skillId, Position pos)
		{
			using var packet = Packet.Rent(Op.ZC_SKILL_ENTRY);

			packet.PutInt(character.Handle);
			packet.PutShort((short)pos.X);
			packet.PutShort((short)pos.Y);
			packet.PutByte((byte)skillId);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Sends cart item and weight information.
		/// </summary>
		public static void ZC_NOTIFY_CARTITEM_COUNTINFO(PlayerCharacter character, int currentCount, int maxCount, int currentWeight, int maxWeight)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_CARTITEM_COUNTINFO);

			packet.PutShort((short)currentCount);
			packet.PutShort((short)maxCount);
			packet.PutInt(currentWeight);
			packet.PutInt(maxWeight);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the list of equipment items in the cart.
		/// </summary>
		public static void ZC_CART_EQUIPMENT_ITEMLIST(PlayerCharacter character, IEnumerable<Item> items)
		{
			using var packet = Packet.Rent(Op.ZC_CART_EQUIPMENT_ITEMLIST);
			foreach (var item in items)
			{
				if (!item.Type.IsEquip())
					continue;
				packet.AddEquipItem(item, item.WearSlots);
			}
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the list of normal items in the cart.
		/// </summary>
		public static void ZC_CART_NORMAL_ITEMLIST(PlayerCharacter character, IEnumerable<Item> items)
		{
			using var packet = Packet.Rent(Op.ZC_CART_NORMAL_ITEMLIST);
			foreach (var item in items)
			{
				if (item.Type.IsEquip())
					continue;
				packet.AddNormalItem(item);
			}
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Adds a single item to the cart display.
		/// </summary>
		public static void ZC_ADD_ITEM_TO_CART(PlayerCharacter character, Item item)
		{
			using var packet = Packet.Rent(Op.ZC_ADD_ITEM_TO_CART);

			packet.PutShort((short)item.InventoryId);
			packet.PutInt(item.Amount);
			// This will likely need adjustment based on real Beta1 packet structure.
			// Using the structure of ZC_ADD_ITEM_TO_STORE as a template.
			packet.PutShort((short)item.ClassId);
			packet.PutByte((byte)item.Type);
			packet.PutByte(item.IsIdentified);
			packet.PutByte(item.IsDamaged); // damaged
			packet.PutByte(item.RefineLevel); // refine
			for (var i = 0; i < 4; i++) // 4 card slots
				packet.PutShort(0);
			packet.PutEmpty(11); // Padding to reach size 32

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Deletes an item from the cart display.
		/// </summary>
		public static void ZC_DELETE_ITEM_FROM_CART(PlayerCharacter character, int index, int amount)
		{
			using var packet = Packet.Rent(Op.ZC_DELETE_ITEM_FROM_CART);
			packet.PutShort((short)index);
			packet.PutInt(amount);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the client that the cart has been removed.
		/// </summary>
		public static void ZC_CARTOFF(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_CARTOFF);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Acknowledges adding an item to the cart.
		/// </summary>
		public static void ZC_ACK_ADDITEM_TO_CART(PlayerCharacter character, bool success)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_ADDITEM_TO_CART);
			packet.PutByte(success);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens the vending store item setup window.
		/// </summary>
		public static void ZC_OPENSTORE(PlayerCharacter character, int itemCount)
		{
			using var packet = Packet.Rent(Op.ZC_OPENSTORE);
			packet.PutShort((short)itemCount);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies clients that a player's vending store has appeared.
		/// </summary>
		public static void ZC_STORE_ENTRY(Character character, string storeName)
		{
			using var packet = Packet.Rent(Op.ZC_STORE_ENTRY);
			packet.PutInt(character.Handle);
			packet.PutString(storeName, 80);
			character.Map.Broadcast(packet, character, BroadcastTargets.AllButSource);
		}

		/// <summary>
		/// Notifies clients that a player's store has disappeared.
		/// </summary>
		public static void ZC_DISAPPEAR_ENTRY(Character character)
		{
			using var packet = Packet.Rent(Op.ZC_DISAPPEAR_ENTRY);
			packet.PutInt(character.Handle);
			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Sends a player merchant's item list to a potential buyer.
		/// </summary>
		public static void ZC_PC_PURCHASE_ITEMLIST_FROMMC(PlayerCharacter character, PlayerCharacter merchant, IEnumerable<VendingItem> items)
		{
			using var packet = Packet.Rent(Op.ZC_PC_PURCHASE_ITEMLIST_FROMMC);
			packet.PutInt(merchant.Handle);
			// Assuming VendingItem has Item and Price
			foreach (var vendingItem in items)
			{
				packet.PutInt(vendingItem.Price);
				// Full item data follows
				packet.AddNormalItem(vendingItem.Item);
			}
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the result of a purchase from a player merchant.
		/// </summary>
		public static void ZC_PC_PURCHASE_RESULT_FROMMC(PlayerCharacter character, int index, int amount, PurchaseResult result)
		{
			using var packet = Packet.Rent(Op.ZC_PC_PURCHASE_RESULT_FROMMC);
			packet.PutShort((short)index);
			packet.PutShort((short)amount);
			packet.PutByte((byte)result);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the list of items in the player's own vending store.
		/// </summary>
		public static void ZC_PC_PURCHASE_MYITEMLIST(PlayerCharacter character, IEnumerable<VendingItem> items)
		{
			using var packet = Packet.Rent(Op.ZC_PC_PURCHASE_MYITEMLIST);
			packet.PutInt(character.Handle);
			foreach (var vendingItem in items)
			{
				packet.PutInt(vendingItem.Price);
				packet.AddNormalItem(vendingItem.Item);
			}
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies a merchant that an item was sold from their store.
		/// </summary>
		public static void ZC_DELETEITEM_FROM_MCSTORE(PlayerCharacter merchant, int index, int amount)
		{
			using var packet = Packet.Rent(Op.ZC_DELETEITEM_FROM_MCSTORE);
			packet.PutShort((short)index);
			packet.PutShort((short)amount);
			merchant.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the client that their attack failed due to being out of range.
		/// </summary>
		public static void ZC_ATTACK_FAILURE_FOR_DISTANCE(PlayerCharacter character, Character target)
		{
			using var packet = Packet.Rent(Op.ZC_ATTACK_FAILURE_FOR_DISTANCE);
			packet.PutInt(target.Handle);
			packet.PutShort((short)target.Position.X);
			packet.PutShort((short)target.Position.Y);
			packet.PutShort((short)character.Position.X);
			packet.PutShort((short)character.Position.Y);
			packet.PutShort((short)character.Parameters.Get(ParameterType.AttackRange));
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the character's current attack range.
		/// </summary>
		public static void ZC_ATTACK_RANGE(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_ATTACK_RANGE);
			packet.PutShort((short)character.Parameters.Get(ParameterType.AttackRange));
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the client that an action has failed.
		/// </summary>
		public static void ZC_ACTION_FAILURE(PlayerCharacter character, int reason)
		{
			using var packet = Packet.Rent(Op.ZC_ACTION_FAILURE);
			packet.PutShort((short)reason);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Confirms equipping an arrow.
		/// </summary>
		public static void ZC_EQUIP_ARROW(PlayerCharacter character, int invId)
		{
			using var packet = Packet.Rent(Op.ZC_EQUIP_ARROW);
			packet.PutShort((short)invId);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the client about HP/SP recovery from items or skills.
		/// </summary>
		public static void ZC_RECOVERY(PlayerCharacter character, ParameterType type, int amount)
		{
			using var packet = Packet.Rent(Op.ZC_RECOVERY);
			packet.PutShort((short)type);
			packet.PutShort((short)amount);
			character.Connection.Send(packet);
		}
	}
}
