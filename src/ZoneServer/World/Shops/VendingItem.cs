using Sabine.Zone.World.Entities;

namespace Sabine.Zone.World.Shops
{
	/// <summary>
	/// Represents an item being sold in a player's vending store.
	/// </summary>
	public class VendingItem
	{
		/// <summary>
		/// The actual item instance being sold. This contains all details
		/// like ClassId, refinement, cards, etc.
		/// </summary>
		public Item Item { get; }

		/// <summary>
		/// The price in Zeny set by the player for this item.
		/// </summary>
		public int Price { get; }

		/// <summary>
		/// The quantity of the item being sold in this vending slot.
		/// </summary>
		public int Amount { get; set; }

		/// <summary>
		/// The index of this item in the vending store's list.
		/// </summary>
		public int Index { get; set; }

		public VendingItem(Item item, int price, int amount)
		{
			Item = item;
			Price = price;
			Amount = amount;
		}
	}
}
