using System.Collections.Generic;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Shared.Network;
using Sabine.Shared.Network.Helpers;
using Sabine.Zone.World.Entities;
using Sabine.Zone.World.Shops;
using Yggdrasil.Logging;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Packet handler methods for the Beta1 version.
	/// </summary>
	public partial class PacketHandler : PacketHandler<ZoneConnection>
	{
		/// <summary>
		/// Request to move an item from inventory to cart.
		/// </summary>
		[PacketHandler(Op.CZ_MOVE_ITEM_FROM_BODY_TO_CART)]
		public void CZ_MOVE_ITEM_FROM_BODY_TO_CART(ZoneConnection conn, Packet packet)
		{
			var index = packet.GetShort();
			var count = packet.GetInt();

			var character = conn.GetCurrentCharacter();
			// TODO: Add cart and item moving logic
			Log.Debug("CZ_MOVE_ITEM_FROM_BODY_TO_CART: Char '{0}' wants to move item at index {1} ({2} amount) to cart.", character.Name, index, count);

			// Placeholder response
			Send.ZC_ACK_ADDITEM_TO_CART(character, true);
		}

		/// <summary>
		/// Request to move an item from cart to inventory.
		/// </summary>
		[PacketHandler(Op.CZ_MOVE_ITEM_FROM_CART_TO_BODY)]
		public void CZ_MOVE_ITEM_FROM_CART_TO_BODY(ZoneConnection conn, Packet packet)
		{
			var index = packet.GetShort();
			var count = packet.GetInt();

			var character = conn.GetCurrentCharacter();
			// TODO: Add cart and item moving logic
			Log.Debug("CZ_MOVE_ITEM_FROM_CART_TO_BODY: Char '{0}' wants to move item at index {1} ({2} amount) from cart.", character.Name, index, count);
		}

		/// <summary>
		/// Request to move an item from storage to cart.
		/// </summary>
		[PacketHandler(Op.CZ_MOVE_ITEM_FROM_STORE_TO_CART)]
		public void CZ_MOVE_ITEM_FROM_STORE_TO_CART(ZoneConnection conn, Packet packet)
		{
			var index = packet.GetShort();
			var count = packet.GetInt();

			var character = conn.GetCurrentCharacter();
			// TODO: Add storage/cart and item moving logic
			Log.Debug("CZ_MOVE_ITEM_FROM_STORE_TO_CART: Char '{0}' wants to move item at index {1} ({2} amount) from store to cart.", character.Name, index, count);
		}

		/// <summary>
		/// Request to move an item from cart to storage.
		/// </summary>
		[PacketHandler(Op.CZ_MOVE_ITEM_FROM_CART_TO_STORE)]
		public void CZ_MOVE_ITEM_FROM_CART_TO_STORE(ZoneConnection conn, Packet packet)
		{
			var index = packet.GetShort();
			var count = packet.GetInt();

			var character = conn.GetCurrentCharacter();
			// TODO: Add storage/cart and item moving logic
			Log.Debug("CZ_MOVE_ITEM_FROM_CART_TO_STORE: Char '{0}' wants to move item at index {1} ({2} amount) from cart to store.", character.Name, index, count);
		}

		/// <summary>
		/// Request to remove the cart.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_CARTOFF)]
		public void CZ_REQ_CARTOFF(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			// TODO: Add logic to remove cart
			Log.Debug("CZ_REQ_CARTOFF: Char '{0}' requested to remove cart.", character.Name);

			Send.ZC_CARTOFF(character);
		}

		/// <summary>
		/// Request to close player's own vending store.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_CLOSESTORE)]
		public void CZ_REQ_CLOSESTORE(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			// TODO: Add vending logic to close store
			Log.Debug("CZ_REQ_CLOSESTORE: Char '{0}' requested to close their store.", character.Name);
		}

		/// <summary>
		/// Request to open a vending store.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_OPENSTORE)]
		public void CZ_REQ_OPENSTORE(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			var len = packet.GetShort();
			var storeName = packet.GetString(80);
			var count = (len - 84) / 8; // 8 bytes per item

			Log.Debug("CZ_REQ_OPENSTORE: Char '{0}' wants to open store '{1}' with {2} items.", character.Name, storeName, count);

			for (var i = 0; i < count; i++)
			{
				var index = packet.GetShort();
				var amount = packet.GetShort();
				var price = packet.GetInt();
				// TODO: Add vending logic
			}

			// For now, just send a success response and display the store
			Send.ZC_STORE_ENTRY(character, storeName);
		}

		/// <summary>
		/// Request to view items from another player's vending store.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_BUY_FROMMC)]
		public void CZ_REQ_BUY_FROMMC(ZoneConnection conn, Packet packet)
		{
			var merchantId = packet.GetInt();
			var character = conn.GetCurrentCharacter();
			var merchant = character.Map.GetCharacter(merchantId) as PlayerCharacter;

			if (merchant == null)
			{
				Log.Debug("CZ_REQ_BUY_FROMMC: Char '{0}' tried to buy from non-existent merchant {1}.", character.Name, merchantId);
				return;
			}

			// TODO: Get merchant's actual vending items
			var placeholderItems = new List<VendingItem>();
			Send.ZC_PC_PURCHASE_ITEMLIST_FROMMC(character, merchant, placeholderItems);
		}

		/// <summary>
		/// Request to buy items from another player's vending store.
		/// </summary>
		[PacketHandler(Op.CZ_PC_PURCHASE_ITEMLIST_FROMMC)]
		public void CZ_PC_PURCHASE_ITEMLIST_FROMMC(ZoneConnection conn, Packet packet)
		{
			var len = packet.GetShort();
			var merchantId = packet.GetInt();
			var character = conn.GetCurrentCharacter();
			var merchant = character.Map.GetCharacter(merchantId) as PlayerCharacter;

			if (merchant == null)
			{
				Log.Debug("CZ_PC_PURCHASE_ITEMLIST_FROMMC: Char '{0}' tried to buy from non-existent merchant {1}.", character.Name, merchantId);
				return;
			}

			var count = (len - 8) / 4; // 4 bytes per item
			for (var i = 0; i < count; i++)
			{
				var amount = packet.GetShort();
				var index = packet.GetShort();
				// TODO: Vending purchase logic
			}

			// For now, just send a success response
			Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, 0, 0, PurchaseResult.Success);
		}

		/// <summary>
		/// Request to change PK mode.
		/// </summary>
		[PacketHandler(Op.CZ_PKMODE_CHANGE)]
		public void CZ_PKMODE_CHANGE(ZoneConnection conn, Packet packet)
		{
			var mode = packet.GetByte(); // 0 = off, 1 = on
			var character = conn.GetCurrentCharacter();

			Log.Debug("CZ_PKMODE_CHANGE: Char '{0}' changed PK mode to {1}.", character.Name, mode);
			// TODO: PK mode logic
		}

		/// <summary>
		/// Request to create an item (e.g., Arrow Crafting).
		/// </summary>
		[PacketHandler(Op.CZ_ITEM_CREATE)]
		public void CZ_ITEM_CREATE(ZoneConnection conn, Packet packet)
		{
			var itemName = packet.GetString(24);
			var character = conn.GetCurrentCharacter();

			Log.Debug("CZ_ITEM_CREATE: Char '{0}' trying to create item '{1}'.", character.Name, itemName);
			// TODO: Item creation logic
		}

		/// <summary>
		/// GM command to move to a map.
		/// </summary>
		[PacketHandler(Op.CZ_MOVETO_MAP)]
		public void CZ_MOVETO_MAP(ZoneConnection conn, Packet packet)
		{
			var mapName = packet.GetString(16).Replace(".gat", "");
			var x = packet.GetShort();
			var y = packet.GetShort();

			var character = conn.GetCurrentCharacter();
			// TODO: Add GM level check
			// if (character.Account.GMLevel < 1) return;

			Log.Debug("CZ_MOVETO_MAP: GM '{0}' warping to {1} ({2},{3})", character.Name, mapName, x, y);

			if (!ZoneServer.Instance.Data.Maps.TryFind(mapName, out var mapData))
			{
				Log.Warning("CZ_MOVETO_MAP: Map '{0}' not found.", mapName);
				return;
			}

			character.Warp(mapData.Id, new Shared.World.Position(x, y));
		}
	}
}
