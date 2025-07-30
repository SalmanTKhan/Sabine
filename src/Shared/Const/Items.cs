using System;

namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines the result of a pickup request.
	/// </summary>
	public enum PickUpResult : byte
	{
		/// <summary>
		/// Item was picked up and will be added to the inventory.
		/// </summary>
		Okay = 0,

		/// <summary>
		/// Shows an error saying that the item is not obtainable.
		/// </summary>
		CantGet = 1,

		/// <summary>
		/// Shows an error that the item can't be picked up due to
		/// the character being above their weight limit.
		/// </summary>
		Overweight = 2,
	}

	/// <summary>
	/// Specifies an item's type, which affects under which tab it's
	/// displayed by the client.
	/// </summary>
	public enum ItemType : byte
	{
		/// <summary>
		/// Item tab.
		/// </summary>
		Healing = 0,

		/// <summary>
		/// Item tab.
		/// </summary>
		Item2 = 1,

		/// <summary>
		/// Item tab.
		/// </summary>
		Usable = 2,

		/// <summary>
		/// Etc tab.
		/// </summary>
		Etc = 3,

		/// <summary>
		/// Equip tab.
		/// </summary>
		Weapon = 4,

		/// <summary>
		/// Equip tab.
		/// </summary>
		Armor = 5,

		/// <summary>
		/// Etc tab.
		/// </summary>
		Etc2 = 6,

		/// <summary>
		/// Etc tab.
		/// </summary>
		Etc3 = 7,

		/// <summary>
		/// Equip tab.
		/// </summary>
		/// <remarks>
		/// The alpha client uses this type to modify the attack range.
		/// By default, it's "17" (not in tiles), and it's increased
		/// to "80" if a bow-type weapon is equipped. (Maybe it's in "sub-
		/// tiles"?)
		/// </remarks>
		RangedWeapon = 8,

		/// <summary>
		/// Equip tab.
		/// </summary>
		Weapon3 = 9,

		// Items assigned types greater than 9 don't appear in the
		// inventory on the alpha client.

		Card = 6,
		PetEgg = 7,
		PetArmor = 8,
		Ammo = 10,
		DelayConsume = 11,
		ShadowGear = 12,
		Cash = 18,
	}

	/// <summary>
	/// Specifies the equip slot(s) an item can be equipped on.
	/// </summary>
	[Flags]
	public enum EquipSlots : uint
	{
		None = 0x00,
		HeadLow = 0x01,
		RightHand = 0x02,
		Garment = 0x04,
		AccessoryLeft = 0x08,
		Body = 0x10,
		LeftHand = 0x20,
		Shoes = 0x40,
		AccessoryRight = 0x80,
		HeadTop = 0x100,
		HeadMid = 0x200,
		CostumeHeadTop = 0x400,
		CostumeHeadMid = 0x800,
		CostumeHeadLow = 0x1000,
		CostumeGarment = 0x2000,
		Ammo = 0x8000,
		ShadowArmor = 0x10000,
		ShadowWeapon = 0x20000,
		ShadowShield = 0x40000,
		ShadowShoes = 0x80000,
		ShadowAccRight = 0x100000,
		ShadowAccLeft = 0x200000,

		// Old client compatibility aliases
		Head = HeadLow | HeadMid | HeadTop,
		Accessory1 = AccessoryLeft,
		Accessory2 = AccessoryRight,
		Robe = Garment,
		Accessories = Accessory1 | Accessory2,
	}

	/// <summary>
	/// Extension for item enums.
	/// </summary>
	public static class ItemConstExtensions
	{
		/// <summary>
		/// Returns true if the type is an equip type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsEquip(this ItemType type)
		{
			switch (type)
			{
				case ItemType.Weapon:
				case ItemType.Armor:
				case ItemType.PetArmor:
				case ItemType.ShadowGear:
				// Old client types
				//case ItemType.RangedWeapon:
				case ItemType.Weapon3:
					return true;
				default:
					return false;
			}
		}
	}
}
