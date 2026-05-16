using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Network.Helpers
{
	/// <summary>
	/// Item-related extension methods for Packet.
	/// </summary>
	public static class PacketItemExtensions
	{
		/// <summary>
		/// Adds item information to packet.
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="item"></param>
		public static void AddNormalItem(this Packet packet, Item item)
		{
			if (Game.Version >= Versions.Sabine)
			{
				AddNormalItemV6(packet, item);
				return;
			}

			if (Game.Version < Versions.Beta2)
			{
				var size = 6 + Sizes.ItemNames;
				if (Game.Version >= Versions.Beta1)
					size += 4;

				// The first byte contains the size of the item
				// struct plus the size byte, which the client
				// memcpys for handling. It's currently unclear
				// why this size byte is necessary, but it's
				// working this way.
				packet.PutByte((byte)size);

				packet.PutByte((byte)item.Type);
				packet.PutShort((short)item.InventoryId);
				packet.PutShort((short)item.Amount);

				if (Game.Version >= Versions.Beta1)
				{
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
				}

				packet.PutString(item.StringId, Sizes.ItemNames);
			}
			else
			{
				packet.PutShort((short)item.InventoryId);
				packet.PutShort((short)item.ClassId);
				packet.PutByte((byte)item.Type);
				packet.PutByte(item.IsIdentified);
				packet.PutShort((short)item.Amount);
				packet.PutShort(0);
			}
		}

		/// <summary>
		/// Adds equip item information to packet.
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="item"></param>
		/// <param name="wearSlots"></param>
		public static void AddEquipItem(this Packet packet, Item item, EquipSlots wearSlots)
		{
			if (Game.Version >= Versions.Sabine)
			{
				AddEquipItemV6(packet, item, wearSlots);
				return;
			}

			if (Game.Version < Versions.Beta2)
			{
				var size = 6 + Sizes.ItemNames;
				if (Game.Version >= Versions.Beta1)
					size += 16;

				// The first byte contains the size of the item
				// struct plus the size byte, which the client
				// memcpys for handling. It's currently unclear
				// why this size byte is necessary, but it's
				// working this way.
				packet.PutByte((byte)size);

				packet.PutByte((byte)item.Type);
				packet.PutByte((byte)wearSlots);
				packet.PutShort((short)item.InventoryId);
				packet.PutByte((byte)item.EquippedOn);

				if (Game.Version >= Versions.Beta1)
				{
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
					packet.PutByte(0);
				}

				packet.PutString(item.StringId, Sizes.ItemNames);
			}
			else
			{
				packet.PutShort((short)item.InventoryId);
				packet.PutShort((short)item.ClassId);
				packet.PutByte((byte)item.Type);
				packet.PutByte(item.IsIdentified);
				packet.PutShort((short)wearSlots);
				packet.PutShort((short)item.EquippedOn);
				packet.PutByte(0);   // Attribute
				packet.PutByte(0);   // Refine
				packet.PutShort(0);  // Card1
				packet.PutShort(0);  // Card2
				packet.PutShort(0);  // Card3
				packet.PutShort(0);  // Card4
			}
		}

		/// <summary>
		/// PV23 / kRO Sak EP12 (20080102 &lt;= PACKETVER &lt; 20120925)
		/// per-entry layout for <c>ZC_NORMAL_ITEMLIST3</c> / store /
		/// cart v3 ops. Mirrors rA's <c>NORMALITEM_INFO</c> struct
		/// (<c>Ref/rathena/src/map/packets_struct.hpp:418</c>): 22
		/// bytes per item — adds the 8-byte <c>EQUIPSLOTINFO</c>
		/// card block and the 4-byte <c>HireExpireDate</c> tail
		/// that the v1 layout omitted.
		/// </summary>
		public static void AddNormalItemV3(this Packet packet, Item item)
		{
			packet.PutShort((short)item.InventoryId);
			packet.PutShort((short)item.ClassId);
			packet.PutByte((byte)item.Type);
			packet.PutByte(item.IsIdentified);
			packet.PutShort((short)item.Amount);
			packet.PutShort(0);              // WearState
			packet.PutShort(0);              // slot.card[0]
			packet.PutShort(0);              // slot.card[1]
			packet.PutShort(0);              // slot.card[2]
			packet.PutShort(0);              // slot.card[3]
			packet.PutInt(0);                // HireExpireDate
		}

		/// <summary>
		/// PV23 per-entry layout for <c>ZC_EQUIPMENT_ITEMLIST3</c> /
		/// store / cart v3 ops. Mirrors rA's <c>EQUIPITEM_INFO</c>
		/// (<c>packets_struct.hpp:457</c>): 26 bytes — same head as
		/// the v1 layout but adds <c>HireExpireDate</c> (4) and
		/// <c>bindOnEquipType</c> (2) tail.
		/// </summary>
		public static void AddEquipItemV3(this Packet packet, Item item, EquipSlots wearSlots)
		{
			packet.PutShort((short)item.InventoryId);
			packet.PutShort((short)item.ClassId);
			packet.PutByte((byte)item.Type);
			packet.PutByte(item.IsIdentified);
			packet.PutShort((short)wearSlots);   // location
			packet.PutShort((short)item.EquippedOn); // WearState
			packet.PutByte(0);                   // IsDamaged
			packet.PutByte(0);                   // RefiningLevel
			packet.PutShort(0);                  // slot.card[0]
			packet.PutShort(0);                  // slot.card[1]
			packet.PutShort(0);                  // slot.card[2]
			packet.PutShort(0);                  // slot.card[3]
			packet.PutInt(0);                    // HireExpireDate
			packet.PutShort(0);                  // bindOnEquipType
		}

		/// <summary>
		/// Adds information for item entrying the scene.
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="item"></param>
		// ---- Sabine PV 50000 (rA V6) inventory helpers -----------------
		//
		// NORMALITEM_INFO and EQUIPITEM_INFO at PACKETVER_MAIN_NUM >=
		// 20181121 (pre-20200916 grade flip). Per-entry wire sizes:
		// normal = 26, equip = 59. See rA packets_struct.hpp:418 and 457.
		// Most fields default to zero on Sabine — the V6 wire shape is
		// reproduced for client compatibility; item-options/grade data
		// can be populated by future item-script work without touching
		// the helpers.

		/// <summary>
		/// Writes a single-item entry for ZC_ADD_ITEM_TO_CART / ZC_ADD_ITEM_TO_STORE
		/// in rA V6 shape at PACKETVER_MAIN_NUM &gt;= 20181121. Body = 2 index +
		/// 4 amount + 4 itemId + 1 itemType + 1 identified + 1 damaged + 1 refine
		/// + 8 slot + 25 option_data = 47, wire = 49. Both ops share the layout.
		/// Refs: rathena packets_struct.hpp:2386 (store 0x0a0a), :2483 (cart 0x0a0b).
		/// </summary>
		public static void AddCartStoreItemV6(this Packet packet, Item item)
		{
			packet.PutShort((short)item.InventoryId);
			packet.PutInt(item.Amount);
			packet.PutInt(item.ClassId);
			packet.PutByte((byte)item.Type);
			packet.PutByte(item.IsIdentified);
			packet.PutByte(item.IsDamaged);
			packet.PutByte(item.RefineLevel);
			AddSlotInfo(packet, item);
			AddItemOptions(packet, item);
		}

		/// <summary>
		/// Writes a single-item entry for ZC_ADD_EXCHANGE_ITEM (trade offer)
		/// in rA V6 shape at PACKETVER_MAIN_NUM &gt;= 20181121 (pre-grade,
		/// header 0x0a96). Body = 4 itemId + 1 itemType + 4 amount + 1
		/// identified + 1 damaged + 1 refine + 8 slot + 25 option_data +
		/// 4 location + 2 look = 51, wire = 53.
		/// Ref: rathena packets_struct.hpp:2608, :2643.
		/// </summary>
		public static void AddExchangeItemV6(this Packet packet, Item item, int amount, EquipSlots wearLocation)
		{
			packet.PutInt(item.ClassId);
			packet.PutByte((byte)item.Type);
			packet.PutInt(amount);
			packet.PutByte(item.IsIdentified);
			packet.PutByte(item.IsDamaged);
			packet.PutByte(item.RefineLevel);
			AddSlotInfo(packet, item);
			AddItemOptions(packet, item);
			packet.PutInt((int)wearLocation);
			packet.PutShort((short)(item.SpriteNumber != 0 ? item.SpriteNumber : item.ClassId));
		}

		/// <summary>
		/// Writes the 8-byte EQUIPSLOTINFO block (4 × uint16 card slots).
		/// </summary>
		public static void AddSlotInfoForPickupAck(this Packet packet, Item item)
			=> AddSlotInfo(packet, item);

		/// <summary>
		/// Writes the 25-byte option_data[5] block.
		/// </summary>
		public static void AddItemOptionsForPickupAck(this Packet packet, Item item)
			=> AddItemOptions(packet, item);

		private static void AddSlotInfo(Packet packet, Item item)
		{
			for (var i = 0; i < 4; i++)
			{
				var card = (item.Cards != null && i < item.Cards.Length) ? item.Cards[i] : 0;
				packet.PutShort((short)card);
			}
		}

		private static void AddItemOptions(Packet packet, Item item)
		{
			for (var i = 0; i < 5; i++)
			{
				var opt = item.Options[i];
				packet.PutShort(opt.Index);
				packet.PutShort(opt.Value);
				packet.PutByte(opt.Param);
			}
		}

		private static byte BuildNormalItemFlag(Item item)
		{
			// Bit 0: IsIdentified. Bit 1: PlaceETCTab (always 0 here).
			byte flag = 0;
			if (item.IsIdentified) flag |= 0x01;
			return flag;
		}

		private static byte BuildEquipItemFlag(Item item)
		{
			// Bit 0: IsIdentified. Bit 1: IsDamaged. Bit 2: PlaceETCTab.
			byte flag = 0;
			if (item.IsIdentified) flag |= 0x01;
			if (item.IsDamaged) flag |= 0x02;
			return flag;
		}

		public static void AddNormalItemV6(this Packet packet, Item item)
		{
			// 26 bytes: index(2) + ITID(4) + type(1) + count(2) +
			// WearState(4) + slot(8) + HireExpireDate(4) + Flag(1).
			packet.PutShort((short)item.InventoryId);
			packet.PutInt(item.ClassId);
			packet.PutByte((byte)item.Type);
			packet.PutShort((short)item.Amount);
			packet.PutInt((int)item.EquippedOn);   // WearState
			AddSlotInfo(packet, item);
			packet.PutInt(item.HireExpireDate);
			packet.PutByte(BuildNormalItemFlag(item));
		}

		public static void AddEquipItemV6(this Packet packet, Item item, EquipSlots wearSlots)
		{
			// 59 bytes: index(2) + ITID(4) + type(1) + location(4) +
			// WearState(4) + RefiningLevel(1) + slot(8) +
			// HireExpireDate(4) + bindOnEquipType(2) + spriteNum(2) +
			// option_count(1) + option_data[5](25) + Flag(1).
			packet.PutShort((short)item.InventoryId);
			packet.PutInt(item.ClassId);
			packet.PutByte((byte)item.Type);
			packet.PutInt((int)wearSlots);          // location
			packet.PutInt((int)item.EquippedOn);    // WearState
			packet.PutByte(item.RefineLevel);
			AddSlotInfo(packet, item);
			packet.PutInt(item.HireExpireDate);
			packet.PutShort((short)item.BindOnEquipType);
			packet.PutShort((short)(item.SpriteNumber != 0 ? item.SpriteNumber : item.ClassId));
			packet.PutByte(5);                      // option_count (always emit 5; zero entries are inert)
			AddItemOptions(packet, item);
			packet.PutByte(BuildEquipItemFlag(item));
		}

		public static void AddEntryItem(this Packet packet, Item item)
		{
			packet.PutInt(item.Handle);

			if (Game.Version >= Versions.Beta2)
			{
				packet.PutShort((short)item.ClassId);
				packet.PutByte(item.IsIdentified);
			}

			packet.PutShort((short)item.Position.X);
			packet.PutShort((short)item.Position.Y);
			packet.PutByte(8);     // Sub-Tile Position
			packet.PutByte(8);     // Sub-Tile Position
			packet.PutShort((short)item.Amount);

			if (Game.Version < Versions.Beta2)
				packet.PutString(item.StringId, Sizes.ItemNames);
		}
	}
}
