using System;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Refinement ATK bonus per weapon level. Mirrors eAthena's
	/// <c>db/refine_db.txt</c> weapon rows.
	/// </summary>
	internal static class RefinementTable
	{
		// Per weapon level (1..4): safeBonus, riskyBonus, safeLimit.
		// safeBonus = ATK gained per refine within the safe range.
		// riskyBonus = ATK gained per refine *above* the safe limit.
		// safeLimit = the highest refine level that still uses safeBonus.
		// Source: eAthena db/refine_db.txt.
		private static readonly (int safe, int risky, int safeLimit)[] _weaponRefine = new[]
		{
			(0, 0, 0),  // index 0 unused (no weapon)
			(2, 3, 7),  // Lv 1 weapons
			(3, 5, 6),  // Lv 2 weapons
			(5, 8, 5),  // Lv 3 weapons
			(7, 13, 4), // Lv 4 weapons
		};

		private const int MaxRefine = 10;

		/// <summary>
		/// Returns the additional ATK granted by refining a weapon to the
		/// given refine level. Levels above the table's safe limit grant
		/// the larger risky bonus per step (representing the over-upgrade
		/// damage scaling, ignoring random crit chance).
		/// </summary>
		public static int GetWeaponAtkBonus(int weaponLevel, int refineLevel)
		{
			if (weaponLevel < 1 || weaponLevel > 4 || refineLevel <= 0)
				return 0;

			var (safe, risky, safeLimit) = _weaponRefine[weaponLevel];
			var capped = Math.Min(refineLevel, MaxRefine);

			if (capped <= safeLimit)
				return capped * safe;

			return safeLimit * safe + (capped - safeLimit) * risky;
		}
	}
}
