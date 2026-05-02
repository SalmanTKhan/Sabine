using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Shops
{
	/// <summary>
	/// Represents an open player-merchant (vending) store.
	/// </summary>
	public class VendingShop
	{
		private readonly List<VendingItem> _items = new();

		/// <summary>Synchronization root for shop state.</summary>
		public object SyncLock { get; } = new();

		/// <summary>The merchant character who owns the store.</summary>
		public PlayerCharacter Owner { get; }

		/// <summary>The name displayed on the shop sign.</summary>
		public string Title { get; }

		/// <summary>Whether the shop is currently open and visible.</summary>
		public bool IsOpen { get; private set; }

		/// <summary>
		/// Creates a new vending shop owned by the given player, with the
		/// given title and initial item list. The shop starts open.
		/// </summary>
		public VendingShop(PlayerCharacter owner, string title, IEnumerable<VendingItem> items)
		{
			this.Owner = owner;
			this.Title = title ?? string.Empty;
			_items.AddRange(items);
			this.IsOpen = true;
		}

		/// <summary>Returns the current item list as a snapshot.</summary>
		public List<VendingItem> GetItemsSnapshot()
		{
			lock (this.SyncLock)
				return new List<VendingItem>(_items);
		}

		/// <summary>Returns the number of remaining vending entries.</summary>
		public int ItemCount
		{
			get { lock (this.SyncLock) return _items.Count; }
		}

		/// <summary>
		/// Tries to find a vending entry by its visible index, which is the
		/// index assigned when the shop was opened (1-based on Beta1).
		/// </summary>
		public bool TryFindByIndex(int index, out VendingItem item)
		{
			lock (this.SyncLock)
			{
				item = _items.FirstOrDefault(a => a.Index == index);
				return item != null;
			}
		}

		/// <summary>
		/// Decrements a vending entry by the given amount, removing it
		/// when its remaining amount reaches zero. Caller is expected to
		/// already hold <see cref="SyncLock"/> through validation.
		/// </summary>
		public void RemoveSold(VendingItem item, int amount)
		{
			lock (this.SyncLock)
			{
				item.Amount -= amount;
				if (item.Amount <= 0)
					_items.Remove(item);
			}
		}

		/// <summary>Marks the shop closed and clears its item list.</summary>
		public void Close()
		{
			lock (this.SyncLock)
			{
				this.IsOpen = false;
				_items.Clear();
			}
		}
	}
}
