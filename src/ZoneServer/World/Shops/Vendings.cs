using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps;

namespace Sabine.Zone.World.Shops
{
	/// <summary>
	/// World-level registry of open vending shops. Mirrors the pattern
	/// used by the trade manager.
	/// </summary>
	public class Vendings
	{
		private readonly Dictionary<int, VendingShop> _byOwnerHandle = new();
		private readonly object _syncLock = new();

		/// <summary>Registers a newly opened shop by its owner's handle.</summary>
		public void Register(VendingShop shop)
		{
			lock (_syncLock)
				_byOwnerHandle[shop.Owner.Handle] = shop;
		}

		/// <summary>Removes a shop owned by the given player, if any.</summary>
		public void Unregister(PlayerCharacter owner)
		{
			lock (_syncLock)
				_byOwnerHandle.Remove(owner.Handle);
		}

		/// <summary>
		/// Returns the open shop owned by the player with the given handle,
		/// or false if no such shop exists.
		/// </summary>
		public bool TryGet(int ownerHandle, out VendingShop shop)
		{
			lock (_syncLock)
				return _byOwnerHandle.TryGetValue(ownerHandle, out shop);
		}

		/// <summary>
		/// Returns all currently open shops on the given map.
		/// </summary>
		public List<VendingShop> GetOpenOnMap(Map map)
		{
			lock (_syncLock)
				return _byOwnerHandle.Values.Where(a => a.IsOpen && a.Owner.Map == map).ToList();
		}
	}
}
