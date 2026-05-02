using System.Collections.Generic;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Shared.Network;
using Sabine.Shared.Network.Helpers;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps;
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
			if (character.Parameters.Cart == 0)
			{
				Send.ZC_ACK_ADDITEM_TO_CART(character, false);
				return;
			}

			var item = character.Inventory.GetItem(index);
			if (item == null)
			{
				Send.ZC_ACK_ADDITEM_TO_CART(character, false);
				return;
			}

			var moved = character.Inventory.MoveToCart(item, count);
			Send.ZC_ACK_ADDITEM_TO_CART(character, moved > 0);

			if (moved > 0)
				SendCartCount(character);
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
			if (character.Parameters.Cart == 0)
				return;

			var item = character.Inventory.GetCartItem(index);
			if (item == null)
				return;

			var moved = character.Inventory.MoveToBody(item, count);
			if (moved > 0)
				SendCartCount(character);
		}

		private static void SendCartCount(PlayerCharacter character)
		{
			Send.ZC_NOTIFY_CARTITEM_COUNTINFO(character,
				character.Inventory.CartItemCount,
				Sabine.Zone.World.Actors.Components.Characters.Inventory.CartMaxSlots,
				character.Inventory.CartWeight,
				Sabine.Zone.World.Actors.Components.Characters.Inventory.CartMaxWeight);
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
			if (!character.Storage.IsOpen || character.Parameters.Cart == 0 || count <= 0)
				return;

			var entry = character.Storage.GetItem(index);
			if (entry == null)
				return;

			var addedWeight = entry.Data.Weight * count;
			if (character.Inventory.CartWeight + addedWeight > Sabine.Zone.World.Actors.Components.Characters.Inventory.CartMaxWeight)
				return;

			var detached = character.Storage.RemoveByIndex(index, count);
			if (detached == null)
				return;

			// MoveToCart consumes a body-side item; route through inventory
			// to take advantage of stacking + cart weight bookkeeping.
			character.Inventory.AddItem(detached);
			var moved = character.Inventory.MoveToCart(detached, count);
			if (moved <= 0)
				return;

			Send.ZC_DELETE_ITEM_FROM_STORE(character, index, count);
			Send.ZC_NOTIFY_STOREITEM_COUNTINFO(character, character.Storage.ItemCount, Sabine.Zone.World.Actors.Components.Characters.Storage.MaxSlots);
			SendCartCount(character);
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
			if (!character.Storage.IsOpen || character.Parameters.Cart == 0 || count <= 0)
				return;

			var cartItem = character.Inventory.GetCartItem(index);
			if (cartItem == null || count > cartItem.Amount)
				return;

			var added = character.Storage.AddFrom(cartItem, count);
			if (added == null)
				return;

			var removed = character.Inventory.RemoveFromCartByInventoryId(index, count);
			if (removed == null)
				return;

			Send.ZC_ADD_ITEM_TO_STORE(character, added);
			Send.ZC_NOTIFY_STOREITEM_COUNTINFO(character, character.Storage.ItemCount, Sabine.Zone.World.Actors.Components.Characters.Storage.MaxSlots);
			SendCartCount(character);
		}

		/// <summary>
		/// Request to remove the cart. Honored only when the player is not
		/// currently vending; cart contents are preserved across toggles.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_CARTOFF)]
		public void CZ_REQ_CARTOFF(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();

			if (character.VendingShop?.IsOpen == true)
				return;

			character.Parameters.Set(ParameterType.Cart, 0);
			Send.ZC_CARTOFF(character);
		}

		/// <summary>
		/// Request to close player's own vending store.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_CLOSESTORE)]
		public void CZ_REQ_CLOSESTORE(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			CloseVendingShop(character);
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

			// Pre-validate state.
			if (character.VendingShop?.IsOpen == true)
				return;

			if (ZoneServer.Instance.World.Trades.TryGetTrade(character, out _))
				return;

			if (character.Parameters.Cart == 0)
				return;

			if (character.Map?.HasFlag(MapFlags.NoVending) == true)
				return;

			var skillLvl = character.Skills.GetLevel(SkillId.MC_VENDING);
			if (skillLvl < 1)
				return;

			var maxItems = 2 + skillLvl;
			if (count < 1 || count > maxItems)
				return;

			// Parse and filter the requested entries against the cart.
			// Indices in the cart inventory are 1-based for the client; on
			// Sabine the cart's InventoryId is what GetCartItem expects.
			var items = new List<VendingItem>(count);
			for (var i = 0; i < count; i++)
			{
				var index = packet.GetShort();
				var amount = packet.GetShort();
				var price = packet.GetInt();

				if (amount <= 0 || price <= 0)
					continue;

				var cartItem = character.Inventory.GetCartItem(index);
				if (cartItem == null)
					continue;

				if (amount > cartItem.Amount)
					continue;

				if (!cartItem.IsIdentified || cartItem.IsDamaged)
					continue;

				items.Add(new VendingItem(cartItem, price, amount) { Index = index });
			}

			if (items.Count == 0)
				return;

			var shop = new VendingShop(character, storeName, items);
			character.VendingShop = shop;
			ZoneServer.Instance.World.Vendings.Register(shop);

			character.Controller.StopMove();
			character.SetMovementBlock(true);

			Send.ZC_OPENSTORE(character, items.Count);
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

			if (!ZoneServer.Instance.World.Vendings.TryGet(merchantId, out var shop) || !shop.IsOpen)
				return;

			var merchant = shop.Owner;
			if (merchant.Map != character.Map)
				return;

			if (!character.Position.InRange(merchant.Position, character.Map.VisibleRange))
				return;

			Send.ZC_PC_PURCHASE_ITEMLIST_FROMMC(character, merchant, shop.GetItemsSnapshot());
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

			var count = (len - 8) / 4; // 4 bytes per item
			var requested = new List<(int Amount, int Index)>(count);
			for (var i = 0; i < count; i++)
			{
				var amount = packet.GetShort();
				var index = packet.GetShort();
				requested.Add((amount, index));
			}

			if (count < 1)
				return;

			if (!ZoneServer.Instance.World.Vendings.TryGet(merchantId, out var shop) || !shop.IsOpen)
			{
				Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, 0, 0, PurchaseResult.Unknown);
				return;
			}

			var merchant = shop.Owner;
			if (merchant == character || merchant.Map != character.Map ||
				!character.Position.InRange(merchant.Position, character.Map.VisibleRange))
			{
				Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, 0, 0, PurchaseResult.Unknown);
				return;
			}

			lock (shop.SyncLock)
			{
				if (!shop.IsOpen)
				{
					Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, 0, 0, PurchaseResult.Unknown);
					return;
				}

				// Validate every line up front.
				var resolved = new List<(VendingItem Entry, int Amount)>(requested.Count);
				long totalCost = 0;
				int totalWeight = 0;

				foreach (var (reqAmount, reqIndex) in requested)
				{
					if (reqAmount <= 0)
					{
						Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, reqIndex, reqAmount, PurchaseResult.Unknown);
						return;
					}

					if (!shop.TryFindByIndex(reqIndex, out var entry))
					{
						Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, reqIndex, reqAmount, PurchaseResult.Unknown);
						return;
					}

					if (reqAmount > entry.Amount)
					{
						Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, reqIndex, reqAmount, PurchaseResult.ItemCountOver);
						return;
					}

					totalCost += (long)entry.Price * reqAmount;
					totalWeight += entry.Item.Data.Weight * reqAmount;
					resolved.Add((entry, reqAmount));
				}

				if (totalCost > int.MaxValue || character.Parameters.Zeny < totalCost)
				{
					Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, 0, 0, PurchaseResult.NotEnoughZeny);
					return;
				}

				if (character.Parameters.Weight + totalWeight > character.Parameters.WeightMax)
				{
					Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, 0, 0, PurchaseResult.Overweight);
					return;
				}

				if ((long)merchant.Parameters.Zeny + totalCost > int.MaxValue)
				{
					Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, 0, 0, PurchaseResult.Unknown);
					return;
				}

				// Commit transaction.
				character.Parameters.Modify(ParameterType.Zeny, -(int)totalCost);
				merchant.Parameters.Modify(ParameterType.Zeny, (int)totalCost);

				foreach (var (entry, amount) in resolved)
				{
					var detached = merchant.Inventory.RemoveFromCartByInventoryId(entry.Item.InventoryId, amount);
					if (detached == null)
						continue;

					character.Inventory.AddItem(detached);
					Send.ZC_DELETEITEM_FROM_MCSTORE(merchant, entry.Index, amount);
					shop.RemoveSold(entry, amount);
				}

				Send.ZC_PC_PURCHASE_RESULT_FROMMC(character, 0, 0, PurchaseResult.Success);

				if (shop.ItemCount == 0)
					CloseVendingShop(merchant);
			}
		}

		/// <summary>
		/// Closes the player's vending shop, broadcasting the disappearance
		/// to nearby players and clearing related state. Safe to call when
		/// no shop is open.
		/// </summary>
		private static void CloseVendingShop(PlayerCharacter character)
		{
			var shop = character.VendingShop;
			if (shop == null)
				return;

			var wasOpen = shop.IsOpen;
			shop.Close();
			ZoneServer.Instance.World.Vendings.Unregister(character);
			character.VendingShop = null;
			character.SetMovementBlock(false);

			if (wasOpen)
				Send.ZC_DISAPPEAR_ENTRY(character);
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
