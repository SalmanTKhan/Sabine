using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Small shared helpers consumed by <see cref="BattleCalculator"/>
	/// and the combat-event listeners. Kept separate from the calculator
	/// so listener code (e.g.,
	/// <see cref="Sabine.Zone.Battle.Listeners.ReflectShieldListener"/>)
	/// doesn't have to reach into the calculator's private surface.
	/// </summary>
	public static class BattleHelpers
	{
		/// <summary>
		/// Returns true if the attacker's right-hand weapon is a ranged
		/// (bow / firearm / shuriken) weapon. Mirrors eAthena's
		/// <c>weapon_type_to_range</c> bow check used in BF_LONG
		/// classification.
		/// </summary>
		public static bool IsRangedWeapon(Character attacker)
		{
			var type = GetWeaponType(attacker);
			return type == WeaponType.Bow
				|| type == WeaponType.Revolver
				|| type == WeaponType.Rifle
				|| type == WeaponType.Shotgun
				|| type == WeaponType.GatlingGun
				|| type == WeaponType.GrenadeLauncher
				|| type == WeaponType.Shuriken;
		}

		/// <summary>
		/// Returns the right-hand weapon type for a player attacker, or
		/// <see cref="WeaponType.Unknown"/> for monsters / unarmed players.
		/// Mirrors eAthena's <c>sstatus->rhw.weapontype</c> read used by
		/// per-weapon ATK rate / size table lookups.
		/// </summary>
		public static WeaponType GetWeaponType(Character attacker)
		{
			if (attacker is not PlayerCharacter pc)
				return WeaponType.Unknown;

			return pc.Inventory.RightHand?.Data.GetWeaponType() ?? WeaponType.Unknown;
		}
	}
}
