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
		Card = 6,

		/// <summary>
		/// Etc tab.
		/// </summary>
		PetEgg = 7,

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

#pragma warning disable CA1069 // Enums values should not be duplicated

		/// <summary>
		/// Alias of RangedWeapon, used by some newer-client item data.
		/// </summary>
		PetArmor = 8,

		/// <summary>
		/// Equip tab.
		/// </summary>
		Ammo = 10,

		/// <summary>
		/// Newer-client item type.
		/// </summary>
		DelayConsume = 11,

		/// <summary>
		/// Newer-client equip-tab item type.
		/// </summary>
		ShadowGear = 12,

		/// <summary>
		/// Newer-client cash-shop item type.
		/// </summary>
		Cash = 18,

#pragma warning restore CA1069 // Enums values should not be duplicated
	}

	/// <summary>
	/// Specifies the equip slot(s) an item can be equipped on.
	/// </summary>
	[Flags]
	public enum EquipSlots : uint
	{
#pragma warning disable CA1069 // Enums values should not be duplicated

		None = 0x00,

		/// <summary>
		/// Alpha: Head slot.
		/// Beta+: Bottom headgear.
		/// </summary>
		HeadBottom = 0x01,

		/// <summary>
		/// Alpha: Head slot.
		/// Beta+: Bottom headgear.
		/// </summary>
		Head = 0x01,

		/// <summary>
		/// Newer-client alias of HeadBottom.
		/// </summary>
		HeadLow = 0x01,

		/// <summary>
		/// Weapons. Character's actual right hand on the left side in
		/// the UI.
		/// </summary>
		RightHand = 0x02,

		/// <summary>
		/// Robes, Garmants
		/// </summary>
		Robe = 0x04,

		/// <summary>
		/// Newer-client alias of Robe.
		/// </summary>
		Garment = 0x04,

		/// <summary>
		/// Accessory slot on the left side.
		/// </summary>
		Accessory1 = 0x08,

		/// <summary>
		/// Newer-client alias of Accessory1.
		/// </summary>
		AccessoryLeft = 0x08,

		/// <summary>
		/// Armor, Clothes
		/// </summary>
		Body = 0x10,

		/// <summary>
		/// Shields, off-hand weapons. Character's actual left hand, on
		/// the right side of the UI.
		/// </summary>
		LeftHand = 0x20,

		/// <summary>
		/// Shoes, Boots
		/// </summary>
		Shoes = 0x40,

		/// <summary>
		/// Accessory slot on the right side.
		/// </summary>
		Accessory2 = 0x80,

		/// <summary>
		/// Newer-client alias of Accessory2.
		/// </summary>
		AccessoryRight = 0x80,

		/// <summary>
		/// Top headgear. Not available in Alpha and Beta1.
		/// </summary>
		HeadTop = 0x100,

		/// <summary>
		/// Middle headgear. Not available in Alpha and Beta1.
		/// </summary>
		HeadMiddle = 0x200,

		/// <summary>
		/// Newer-client alias of HeadMiddle.
		/// </summary>
		HeadMid = 0x200,

		// Newer-client costume slots.
		CostumeHeadTop = 0x400,
		CostumeHeadMid = 0x800,
		CostumeHeadLow = 0x1000,
		CostumeGarment = 0x2000,

		Ammo = 0x8000,

		// Newer-client shadow-gear slots.
		ShadowArmor = 0x10000,
		ShadowWeapon = 0x20000,
		ShadowShield = 0x40000,
		ShadowShoes = 0x80000,
		ShadowAccRight = 0x100000,
		ShadowAccLeft = 0x200000,

		ShadowAccessories = ShadowAccLeft | ShadowAccRight,

		/// <summary>
		/// Both/Either accessory slot.
		/// </summary>
		Accessories = Accessory1 | Accessory2,

#pragma warning restore CA1069 // Enums values should not be duplicated
	}

	public enum WeaponType
	{
		// A good practice is to have a default/unknown value at 0.
		Unknown = 0,

		Dagger = 1,
		OneHandedSword = 2,
		TwoHandedSword = 3,
		OneHandedSpear = 4,
		TwoHandedSpear = 5,
		OneHandedAxe = 6,
		TwoHandedAxe = 7,
		Mace = 8,
		// LookId 9 is intentionally skipped as in the original code
		Staff = 10,
		Bow = 11,
		Knuckle = 12,
		Instrument = 13,
		Whip = 14,
		Book = 15,
		Katar = 16,
		Revolver = 17,
		Rifle = 18,
		GatlingGun = 19,
		Shotgun = 20,
		GrenadeLauncher = 21,
		Shuriken = 22
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
				case ItemType.RangedWeapon: // also PetArmor (alias)
				case ItemType.Weapon3:
				case ItemType.ShadowGear:
					return true;
				default:
					return false;
			}
		}
	}
}
