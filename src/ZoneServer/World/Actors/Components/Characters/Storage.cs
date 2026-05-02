using System;
using System.Collections.Generic;
using System.Linq;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// Account-bound item storage (the Kafra "Cody"/"Storage" window).
	/// Items here are shared across characters of the same account; on
	/// Sabine they are loaded onto the active <see cref="PlayerCharacter"/>
	/// at character-load and persisted by account id.
	/// </summary>
	public class Storage
	{
		/// <summary>Maximum number of distinct stacks the storage holds.</summary>
		public const int MaxSlots = 600;

		private readonly object _syncLock = new();
		private readonly List<Item> _items = new();

		/// <summary>The character whose connection currently "owns" this view.</summary>
		public PlayerCharacter Character { get; }

		/// <summary>Whether the storage window is currently open on the client.</summary>
		public bool IsOpen { get; set; }

		/// <summary>Number of distinct item stacks in storage.</summary>
		public int ItemCount
		{
			get { lock (_syncLock) return _items.Count; }
		}

		public Storage(PlayerCharacter character)
		{
			this.Character = character;
		}

		/// <summary>Adds an item without notifying the client. For loading.</summary>
		internal void AddItemInit(Item item)
		{
			lock (_syncLock)
			{
				item.InventoryId = this.GetNewIndexLocked();
				_items.Add(item);
			}
		}

		/// <summary>Snapshot of all items in storage.</summary>
		public Item[] GetItems()
		{
			lock (_syncLock)
				return _items.ToArray();
		}

		/// <summary>Returns the storage entry with the given index, or null.</summary>
		public Item GetItem(int index)
		{
			lock (_syncLock)
				return _items.FirstOrDefault(a => a.InventoryId == index);
		}

		/// <summary>
		/// Adds <paramref name="amount"/> of <paramref name="source"/> into
		/// storage, merging into an existing stack when possible. Returns
		/// the entry that holds the moved quantity (existing or new), or
		/// null if storage is full. The source item's <see cref="Item.Amount"/>
		/// is NOT touched here — callers handle the inventory side.
		/// </summary>
		public Item AddFrom(Item source, int amount)
		{
			if (source == null || amount <= 0)
				return null;

			lock (_syncLock)
			{
				if (source.IsStackable)
				{
					var existing = _items.FirstOrDefault(a => a.ClassId == source.ClassId && a.IsStackable);
					if (existing != null)
					{
						existing.Amount += amount;
						return existing;
					}
				}

				if (_items.Count >= MaxSlots)
					return null;

				var copy = new Item(source.ClassId, amount);
				copy.RefineLevel = source.RefineLevel;
				copy.IsIdentified = source.IsIdentified;
				copy.IsDamaged = source.IsDamaged;
				copy.InventoryId = this.GetNewIndexLocked();
				_items.Add(copy);
				return copy;
			}
		}

		/// <summary>
		/// Removes up to <paramref name="amount"/> of the storage entry with
		/// the given index and returns it as a detached <see cref="Item"/>
		/// suitable for handing to <see cref="Inventory.AddItem"/>.
		/// </summary>
		public Item RemoveByIndex(int index, int amount)
		{
			if (amount <= 0)
				return null;

			lock (_syncLock)
			{
				var item = _items.FirstOrDefault(a => a.InventoryId == index);
				if (item == null)
					return null;

				var moveAmount = Math.Min(amount, item.Amount);
				if (moveAmount <= 0)
					return null;

				if (moveAmount == item.Amount)
				{
					_items.Remove(item);
					item.InventoryId = 0;
					return item;
				}

				item.Amount -= moveAmount;

				var split = new Item(item.ClassId, moveAmount);
				split.RefineLevel = item.RefineLevel;
				split.IsIdentified = item.IsIdentified;
				split.IsDamaged = item.IsDamaged;
				return split;
			}
		}

		/// <summary>
		/// Returns the number of remaining free stack slots in this storage.
		/// </summary>
		public int FreeSlots
		{
			get { lock (_syncLock) return Math.Max(0, MaxSlots - _items.Count); }
		}

		private int GetNewIndexLocked()
		{
			for (var i = 1; i < short.MaxValue; ++i)
			{
				if (!_items.Any(a => a.InventoryId == i))
					return i;
			}
			return -1;
		}
	}
}
