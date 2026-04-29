using System;
using System.Collections.Generic;
using System.Linq;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Zone.Battle.Cards;
using Sabine.Zone.Network;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// Represents a player's inventory.
	/// </summary>
	public class Inventory
	{
		private readonly object _syncLock = new();

		private readonly List<Item> _items = new();
		private readonly List<Item> _cartItems = new();
		private EquipSlots _occupiedSlots;

		/// <summary>Maximum total weight of the cart contents.</summary>
		public const int CartMaxWeight = 8000;

		/// <summary>Maximum number of distinct item slots in the cart.</summary>
		public const int CartMaxSlots = 100;

		/// <summary>
		/// Returns the character this inventory belongs to.
		/// </summary>
		public PlayerCharacter Character { get; }

		/// <summary>
		/// Returns a reference to the item that is currently equipped
		/// in the right hand, if any.
		/// </summary>
		public Item RightHand { get; private set; }

		/// <summary>
		/// Returns a reference to the item that is currently equipped
		/// in the right hand, if any.
		/// </summary>
		public Item LeftHand { get; private set; }

		/// <summary>
		/// Returns a reference to the item that is currently designated
		/// as ammo, if any.
		/// </summary>
		public Item Ammo { get; private set; }

		/// <summary>
		/// Creates new inventory for character.
		/// </summary>
		/// <param name="character"></param>
		public Inventory(PlayerCharacter character)
		{
			this.Character = character;
		}

		/// <summary>
		/// Returns true if an item is equipped on the given slot.
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		private bool IsSlotOccupied(EquipSlots slot)
		{
			return (_occupiedSlots & slot) != 0;
		}

		/// <summary>
		/// Returns the total weight of all items in the inventory.
		/// </summary>
		/// <returns></returns>
		public int GetWeight()
		{
			lock (_syncLock)
				return _items.Sum(static a => a.Data.Weight * a.Amount);
		}

		/// <summary>
		/// Returns the total weight of all items currently inside the
		/// player's cart. Used by Cart Revolution for damage scaling.
		/// </summary>
		public int CartWeight
		{
			get
			{
				lock (_syncLock)
					return _cartItems.Sum(static a => a.Data.Weight * a.Amount);
			}
		}

		/// <summary>Number of distinct item stacks in the cart.</summary>
		public int CartItemCount
		{
			get
			{
				lock (_syncLock)
					return _cartItems.Count;
			}
		}

		/// <summary>Snapshot of all cart contents.</summary>
		public Item[] GetCartItems()
		{
			lock (_syncLock)
				return _cartItems.ToArray();
		}

		/// <summary>
		/// Moves up to <paramref name="amount"/> of <paramref name="item"/>
		/// from the body inventory into the cart. Returns the amount that
		/// was actually moved (may be 0 if the cart is full or the
		/// weight cap is exceeded). Stacks merge with an existing stack
		/// of the same class id.
		/// </summary>
		public int MoveToCart(Item item, int amount)
		{
			if (item == null || amount <= 0) return 0;

			lock (_syncLock)
			{
				if (!_items.Contains(item)) return 0;

				var moveAmount = Math.Min(amount, item.Amount);
				var addedWeight = item.Data.Weight * moveAmount;
				if (this.CartWeight + addedWeight > CartMaxWeight) return 0;

				var existing = _cartItems.FirstOrDefault(a => a.ClassId == item.ClassId && item.IsStackable);
				if (existing != null)
				{
					existing.Amount += moveAmount;
				}
				else
				{
					if (_cartItems.Count >= CartMaxSlots) return 0;

					if (moveAmount == item.Amount)
					{
						_items.Remove(item);
						_cartItems.Add(item);
						return moveAmount;
					}

					var split = new Item(item.ClassId, moveAmount);
					split.InventoryId = item.InventoryId;
					_cartItems.Add(split);
				}

				item.Amount -= moveAmount;
				if (item.Amount == 0)
					_items.Remove(item);

				return moveAmount;
			}
		}

		/// <summary>
		/// Reverse of <see cref="MoveToCart"/>: moves items from the cart
		/// back into the body inventory. Returns the amount moved.
		/// </summary>
		public int MoveToBody(Item item, int amount)
		{
			if (item == null || amount <= 0) return 0;

			lock (_syncLock)
			{
				if (!_cartItems.Contains(item)) return 0;

				var moveAmount = Math.Min(amount, item.Amount);

				var existing = _items.FirstOrDefault(a => a.ClassId == item.ClassId && item.IsStackable);
				if (existing != null)
				{
					existing.Amount += moveAmount;
				}
				else
				{
					if (moveAmount == item.Amount)
					{
						_cartItems.Remove(item);
						item.InventoryId = this.GetNewInventoryId();
						_items.Add(item);
						return moveAmount;
					}

					var split = new Item(item.ClassId, moveAmount);
					split.InventoryId = this.GetNewInventoryId();
					_items.Add(split);
				}

				item.Amount -= moveAmount;
				if (item.Amount == 0)
					_cartItems.Remove(item);

				return moveAmount;
			}
		}

		/// <summary>Returns the cart item with the given inventory id, or null.</summary>
		public Item GetCartItem(int invId)
		{
			lock (_syncLock)
				return _cartItems.FirstOrDefault(a => a.InventoryId == invId);
		}

		/// <summary>
		/// Adds item to inventory during loading of a character.
		/// </summary>
		/// <param name="item"></param>
		internal void AddItemInit(Item item)
		{
			lock (_syncLock)
			{
				item.InventoryId = this.GetNewInventoryId();

				_items.Add(item);
				_occupiedSlots |= item.EquippedOn;
			}

			if ((item.EquippedOn & EquipSlots.RightHand) != 0)
				this.Character.WeaponLook = item.Data.LookId;

			if (item.EquippedOn != EquipSlots.None)
				this.UpdateEquipReferences();

			if (this.Character.AmmoClassId == item.ClassId && this.Ammo == null)
				this.Ammo = item;
		}

		/// <summary>
		/// Adds item to inventory and updates client.
		/// </summary>
		/// <param name="item"></param>
		public void AddItem(Item item)
		{
			lock (_syncLock)
			{
				if (_items.Contains(item))
					throw new ArgumentException("The item is already part of this inventory.");

				if (item.IsStackable)
				{
					var existingStack = _items.FirstOrDefault(a => a.ClassId == item.ClassId);
					if (existingStack != null)
					{
						var addAmount = item.Amount;
						existingStack.Amount += addAmount;

						// Sending a pick up packet for the existing stack
						// adds the amount to it.
						Send.ZC_ITEM_PICKUP_ACK(this.Character, existingStack, addAmount, PickUpResult.Okay);
						return;
					}
				}

				item.InventoryId = this.GetNewInventoryId();
				_items.Add(item);
			}

			Send.ZC_ITEM_PICKUP_ACK(this.Character, item, PickUpResult.Okay);
		}

		/// <summary>
		/// Returns an unused inventory id.
		/// </summary>
		private int GetNewInventoryId()
		{
			// TODO: Optimize. Maybe just create a pool for freed ids
			//   that we can take new ones from?

			lock (_syncLock)
			{
				for (var i = 1; i < short.MaxValue; ++i)
				{
					if (!_items.Any(a => a.InventoryId == i))
						return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Removes item from inventory and updates client.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool RemoveItem(Item item)
		{
			var amount = item.Amount;
			return this.DecrementItem(item, item.Amount) == amount;
		}

		/// <summary>
		/// Decrements the item by the given amount. The item is removed
		/// if the amount was greater or equal to the item's amount.
		/// Returns the amount that was actually removed.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public int DecrementItem(Item item, int amount)
		{
			if (!this.ContainsItem(item))
				throw new ArgumentException("Item not found in inventory.");

			var amountBefore = item.Amount;
			item.Amount -= amount;

			var removedAmount = amountBefore - item.Amount;

			if (item.Amount == 0)
			{
				lock (_syncLock)
					_items.Remove(item);
			}

			if (item.Amount == 0 && item == this.Ammo)
				this.UnequipAmmo();

			Send.ZC_ITEM_THROW_ACK(this.Character, item.InventoryId, amount);
			return removedAmount;
		}

		/// <summary>
		/// Returns the item with the given inventory id, or null if it
		/// wasn't found.
		/// </summary>
		/// <param name="invId"></param>
		/// <returns></returns>
		public Item GetItem(int invId)
		{
			lock (_syncLock)
				return _items.FirstOrDefault(a => a.InventoryId == invId);
		}

		/// <summary>
		/// Returns the item with the given inventory id via out. Returns
		/// false if the item wasn't found.
		/// </summary>
		/// <param name="invId"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool TryGetItem(int invId, out Item item)
		{
			item = this.GetItem(invId);
			return item != null;
		}

		/// <summary>
		/// Returns the item with the given handle, or null if it wasn't
		/// found.
		/// </summary>
		/// <param name="handle"></param>
		public Item GetItemByHandle(int handle)
		{
			lock (_syncLock)
				return _items.FirstOrDefault(a => a.Handle == handle);
		}

		/// <summary>
		/// Returns the item that is equipped in the given slot, or null
		/// if no item is equipped in it.
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		private Item GetItemByEquipSlot(EquipSlots slot)
		{
			lock (_syncLock)
				return _items.FirstOrDefault(a => (a.EquippedOn & slot) != 0);
		}

		/// <summary>
		/// Returns a list with all items in the inventory.
		/// </summary>
		/// <returns></returns>
		public Item[] GetItems()
		{
			lock (_syncLock)
				return _items.ToArray();
		}

		/// <summary>
		/// Returns a list of all items in the inventory that match
		/// the given predicate.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public Item[] GetItems(Func<Item, bool> predicate)
		{
			lock (_syncLock)
				return _items.Where(predicate).ToArray();
		}

		/// <summary>
		/// Returns true if the given item is part of this inventory.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool ContainsItem(Item item)
		{
			lock (_syncLock)
				return _items.Contains(item);
		}

		/// <summary>
		/// Equips the item on the given slot.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="slots"></param>
		public void EquipItem(Item item, EquipSlots slots)
		{
			if (!this.ContainsItem(item))
				throw new ArgumentException("The item must be added to the inventory before it can be equipped.");

			lock (_syncLock)
			{
				// If the client sent both accessory slots as the target,
				// as at least some of them do, use Acc2 if it's free or
				// Acc1 if it's not, rotating newly equipped accessories
				// in that slot.
				// If you're testing a new client and accessories only
				// ever seem to go into Acc2, then this check, and the
				// one in the item data, need to be adjusted, because
				// the client sent an unequip packet before the equip
				// packed, removing any accessories.
				if (Game.Version >= Versions.Beta2)
				{
					if (slots == EquipSlots.Accessories)
						slots = this.IsSlotOccupied(EquipSlots.Accessory2) ? EquipSlots.Accessory1 : EquipSlots.Accessory2;

					// The alpha clients always sends an unequip packet
					// before the equip packet, but in later versions the
					// server got more control, so we have to do it here.
					// This was presumably also the point at which acces-
					// sories could be equipped in either slot.
					if (this.IsSlotOccupied(slots))
					{
						var unequippedItem = this.GetItemByEquipSlot(slots);
						if (unequippedItem != null)
							this.UnequipItem(unequippedItem);
					}
				}

				if (this.IsSlotOccupied(slots))
					throw new ArgumentException($"Other items are already occupying the given slots (Slots: {slots}, Occupied: {_occupiedSlots}).");

				_occupiedSlots |= slots;
			}

			item.EquippedOn = slots;

			Send.ZC_REQ_WEAR_EQUIP_ACK.Success(this.Character, item.InventoryId, slots);

			this.UpdateEquipReferences();
			this.OnEquippedItem(item, slots);
		}

		/// <summary>
		/// Unequips the item from its current slot.
		/// </summary>
		/// <param name="item"></param>
		public void UnequipItem(Item item)
		{
			if (!this.ContainsItem(item))
				throw new ArgumentException("The item can't be unequipped if it's not part of the inventory.");

			if (item.EquippedOn == EquipSlots.None)
				throw new ArgumentException("The item is not equipped.");

			var slots = item.EquippedOn;
			item.EquippedOn = EquipSlots.None;

			lock (_syncLock)
				_occupiedSlots &= ~slots;

			Send.ZC_REQ_TAKEOFF_EQUIP_ACK(this.Character, item.InventoryId, slots);

			this.UpdateEquipReferences();
			this.OnUnequippedItem(item, slots);
		}

		/// <summary>
		/// Equips the given item as ammo.
		/// </summary>
		/// <param name="item"></param>
		public void EquipAmmo(Item item)
		{
			this.Character.AmmoClassId = item.ClassId;
			this.Ammo = item;

			Send.ZC_EQUIP_ARROW(this.Character, item);
		}

		/// <summary>
		/// Unequips the current ammo item.
		/// </summary>
		public void UnequipAmmo()
		{
			this.Character.AmmoClassId = 0;
			this.Ammo = null;

			Send.ZC_EQUIP_ARROW(this.Character, null);
		}

		/// <summary>
		/// Sends a list of all items to the client again to refresh it,
		/// particularly the equip items.
		/// </summary>
		/// <remarks>
		/// Since we can't send negative responses to item equip requests
		/// (see CZ_USE_ITEM handler), we need to be able to tell the
		/// client to not try to equip items they shouldn't be able to
		/// equip in the first place. For this purpose we're setting the
		/// wear slots to 0 for the character when sending the items.
		/// After a job or level change, however, we need to update that
		/// information, so the player is able to now equip those items.
		/// This method simply removes all items from the inventory and
		/// adds them again. The player doesn't notice and we get what
		/// we want.
		/// </remarks>
		public void RefreshClient()
		{
			var character = this.Character;
			var items = character.Inventory.GetItems();

			foreach (var item in items)
				Send.ZC_ITEM_THROW_ACK(character, item.InventoryId, item.Amount);

			Send.ZC_EQUIPMENT_ITEMLIST(character, items);
			Send.ZC_NORMAL_ITEMLIST(character, items);
		}

		/// <summary>
		/// Called when the player equipped an item.
		/// </summary>
		/// <param name="item">The item that was equipped.</param>
		/// <param name="slots">The slot(s) the item was equipped on.</param>
		private void OnEquippedItem(Item item, EquipSlots slots)
		{
			CardHandlerManager.ApplyEquipped(this.Character, item);

			if ((slots & EquipSlots.RightHand) != 0)
				this.Character.ChangeLook(SpriteType.Weapon, item.Data.LookId);

			if (Game.Version <= Versions.Beta1)
			{
				if ((slots & EquipSlots.HeadTop) != 0 || (slots & EquipSlots.HeadMiddle) != 0 || (slots & EquipSlots.HeadBottom) != 0)
				{
					this.Character.ChangeLook(SpriteType.Head, item.Data.LookId);
				}
			}
			else
			{
				// Get the look ids for the equipped item and set one of
				// them.

				var top = 0;
				var middle = 0;
				var bottom = 0;

				if ((slots & EquipSlots.HeadTop) != 0)
					top = item.Data.LookId;

				if ((slots & EquipSlots.HeadMiddle) != 0)
					middle = item.Data.LookId;

				if ((slots & EquipSlots.HeadBottom) != 0)
					bottom = item.Data.LookId;

				if (top == middle) middle = 0;
				if (middle == bottom) bottom = 0;
				if (top == bottom) bottom = 0;

				if (top != 0) this.Character.ChangeLook(SpriteType.HeadTop, top);
				if (middle != 0) this.Character.ChangeLook(SpriteType.HeadMiddle, middle);
				if (bottom != 0) this.Character.ChangeLook(SpriteType.HeadBottom, bottom);
			}

			this.Character.Parameters.RecalculateAll();
		}

		/// <summary>
		/// Called when the player unequipped an item.
		/// </summary>
		/// <param name="item">The item that was unequipped.</param>
		/// <param name="slots">The slot(s) the item was unequipped from.</param>
		private void OnUnequippedItem(Item item, EquipSlots slots)
		{
			CardHandlerManager.ApplyUnequipped(this.Character, item);

			if ((slots & EquipSlots.RightHand) != 0)
				this.Character.ChangeLook(SpriteType.Weapon, 0);

			if ((slots & EquipSlots.HeadTop) != 0)
				this.Character.ChangeLook(SpriteType.HeadTop, 0);

			if ((slots & EquipSlots.HeadMiddle) != 0)
				this.Character.ChangeLook(SpriteType.HeadMiddle, 0);

			if ((slots & EquipSlots.HeadBottom) != 0)
				this.Character.ChangeLook(SpriteType.HeadBottom, 0);

			this.Character.Parameters.RecalculateAll();
		}

		/// <summary>
		/// Updates all of the inventory's equip reference properties.
		/// </summary>
		public void UpdateEquipReferences()
		{
			// TODO: Maybe optimize this a little, though it won't really
			//   matter.

			this.RightHand = this.GetItemByEquipSlot(EquipSlots.RightHand);
			this.LeftHand = this.GetItemByEquipSlot(EquipSlots.LeftHand);
		}

		/// <summary>
		/// Unequips items the character should currently not be able to
		/// equip, like after changing its level or job.
		/// </summary>
		public void CheckEquipRequirements()
		{
			lock (_syncLock)
			{
				foreach (var item in _items)
				{
					if (!item.IsEquipped)
						continue;

					if (this.Character.CanEquip(item))
						continue;

					this.UnequipItem(item);
				}
			}
		}

		/// <summary>
		/// Returns the total amount of defense granted to the player
		/// from the currently equipped items.
		/// </summary>
		/// <returns></returns>
		public int GetEquipDefense()
		{
			var result = 0;

			lock (_syncLock)
			{
				foreach (var item in _items)
				{
					if (item.IsEquipped)
						result += item.Data.Defense;
				}
			}

			return result;
		}

		/// <summary>
		/// Returns the total amount of defense granted to the player
		/// from the currently equipped items.
		/// </summary>
		/// <returns></returns>
		public int GetEquipMagicDefense()
		{
			var result = 0;

			lock (_syncLock)
			{
				foreach (var item in _items)
				{
					if (item.IsEquipped)
						result += item.Data.MagicDefense;
				}
			}

			return result;
		}

		/// <summary>
		/// Returns the total amount of an item across all stacks in the
		/// inventory. Equivalent to rAthena's <c>countitem</c>.
		/// </summary>
		/// <param name="classId"></param>
		public int CountItem(int classId)
		{
			var count = 0;

			lock (_syncLock)
			{
				foreach (var item in _items)
				{
					if (item.ClassId == classId)
						count += item.Amount;
				}
			}

			return count;
		}

		/// <summary>
		/// Returns true if any item is equipped on the given slot.
		/// Mirrors rAthena's <c>getequipisequiped</c>.
		/// </summary>
		/// <param name="slot"></param>
		public bool IsEquipped(EquipSlots slot)
		{
			lock (_syncLock)
				return (_occupiedSlots & slot) != 0;
		}

		/// <summary>
		/// Returns true if the inventory has weight headroom for `amount`
		/// more units of `classId`. Mirrors rAthena's <c>checkweight</c>;
		/// returns false (no headroom) if the item id isn't in the DB so
		/// scripts fail safely instead of silently letting unknown items
		/// through.
		/// </summary>
		/// <param name="classId"></param>
		/// <param name="amount"></param>
		public bool CanCarry(int classId, int amount)
		{
			if (amount <= 0)
				return true;

			if (!ZoneServer.Instance.Data.Items.TryFind(classId, out var data))
				return false;

			var addedWeight = data.Weight * amount;
			var newWeight = this.GetWeight() + addedWeight;
			var max = this.Character.Parameters.WeightMax;

			return newWeight <= max;
		}

		/// <summary>
		/// Returns true if the inventory contains at least the given
		/// amount of the item.
		/// </summary>
		/// <param name="classId"></param>
		/// <returns></returns>
		public bool Contains(int classId, int amount = 1)
		{
			var count = 0;

			lock (_syncLock)
			{
				foreach (var item in _items)
				{
					if (item.ClassId == classId)
					{
						count += item.Amount;

						if (count >= amount)
							return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Adds the given amount of items to the inventory.
		/// </summary>
		/// <param name="classId"></param>
		/// <param name="amount"></param>
		public void Add(int classId, int amount)
		{
			var item = new Item(classId, amount);
			this.AddItem(item);
		}

		/// <summary>
		/// Removes the given amount of the item from the inventory.
		/// Returns the actual amount that was removed.
		/// </summary>
		/// <param name="classId"></param>
		/// <param name="amount"></param>
		public int Remove(int classId, int amount)
		{
			var totalRemovedAmount = 0;

			lock (_syncLock)
			{
				foreach (var item in _items)
				{
					if (item.ClassId != classId)
						continue;

					var removeAmount = Math.Min(amount, item.Amount);
					this.DecrementItem(item, removeAmount);

					amount -= removeAmount;
					totalRemovedAmount += removeAmount;

					if (amount == 0)
						break;
				}
			}

			return totalRemovedAmount;
		}

		internal Item? GetEquip(EquipSlots equipSlot)
		{
			return _items.Find(a => (a.EquippedOn & equipSlot) != 0);
		}

		internal Item[] GetEquippedItems()
			=> this.GetItems(i => i.IsEquipped);

		/// <summary>
		/// Returns true if any of the given item class ids are currently
		/// equipped. Mirrors rAthena's <c>isequipped</c> (one-of
		/// semantics).
		/// </summary>
		/// <param name="classIds">Item class ids to check.</param>
		public bool IsAnyEquipped(params int[] classIds)
		{
			if (classIds == null || classIds.Length == 0)
				return false;

			lock (_syncLock)
			{
				foreach (var item in _items)
				{
					if (!item.IsEquipped)
						continue;

					for (var i = 0; i < classIds.Length; ++i)
					{
						if (item.ClassId == classIds[i])
							return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns the class id of the item currently equipped in the
		/// given slot, or 0 if nothing is equipped there. Mirrors
		/// rAthena's <c>getequipid</c>.
		/// </summary>
		/// <param name="slot">Slot to query.</param>
		public int GetEquippedId(EquipSlots slot)
		{
			var item = this.GetEquip(slot);
			return item?.ClassId ?? 0;
		}
	}
}
