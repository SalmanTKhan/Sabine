using Sabine.Shared.Const;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Weapon-type vs target-size damage multiplier. Mirrors eAthena's
	/// <c>db/size_fix.txt</c>.
	/// </summary>
	internal static class SizeTable
	{
		// Rows: SizeType (Small=0, Medium=1, Large=2)
		// Columns: WeaponType — see _weaponTypeToColumn for the mapping.
		// Source: eAthena db/size_fix.txt.
		private static readonly int[,] _modifier = new int[3, 24]
		{
			// Unarm  Knife  1HSwd  2HSwd  1HSpr  2HSpr  1HAxe  2HAxe   Mac    2HMac  Stf    Bow    Knu    Inst   Whip   Book   Katar  Rev    Rifle  Shotgun GatGun GrenL  Shuri  2HStaff
			/* S */ { 100,    100,   75,    75,    75,    75,    50,    50,    75,    100,   100,   100,   100,   75,    75,    100,   75,    100,   100,   100,    100,   100,   100,   100 },
			/* M */ { 100,    75,    100,   75,    75,    75,    75,    75,    100,   100,   100,   100,   75,    100,   100,   100,   100,   100,   100,   100,    100,   100,   100,   100 },
			/* L */ { 100,    50,    75,    100,   100,   100,   100,   100,   100,   100,   100,   75,    50,    75,    50,    50,    75,    100,   100,   100,    100,   100,   100,   100 },
		};

		/// <summary>
		/// Maps a Sabine WeaponType to the eAthena size_fix column index.
		/// Sabine swaps GatlingGun(19)/Shotgun(20) compared to eAthena's
		/// 19=GatlingGun, 20=Shotgun column order, so they need a manual
		/// remap.
		/// </summary>
		private static int WeaponTypeToColumn(WeaponType type)
		{
			return type switch
			{
				WeaponType.Unknown => 0,
				WeaponType.Dagger => 1,
				WeaponType.OneHandedSword => 2,
				WeaponType.TwoHandedSword => 3,
				WeaponType.OneHandedSpear => 4,
				WeaponType.TwoHandedSpear => 5,
				WeaponType.OneHandedAxe => 6,
				WeaponType.TwoHandedAxe => 7,
				WeaponType.Mace => 8,
				WeaponType.Staff => 10,
				WeaponType.Bow => 11,
				WeaponType.Knuckle => 12,
				WeaponType.Instrument => 13,
				WeaponType.Whip => 14,
				WeaponType.Book => 15,
				WeaponType.Katar => 16,
				WeaponType.Revolver => 17,
				WeaponType.Rifle => 18,
				WeaponType.GatlingGun => 19,
				WeaponType.Shotgun => 20,
				WeaponType.GrenadeLauncher => 21,
				WeaponType.Shuriken => 22,
				_ => 0,
			};
		}

		/// <summary>
		/// Returns the percent multiplier (100 = 1.0x) for the given
		/// weapon type striking a target of the given size.
		/// </summary>
		public static int GetModifier(WeaponType weapon, SizeType size)
		{
			var col = WeaponTypeToColumn(weapon);
			var row = (int)size;

			if (row < 0 || row > 2 || col < 0 || col >= _modifier.GetLength(1))
				return 100;

			return _modifier[row, col];
		}
	}
}
