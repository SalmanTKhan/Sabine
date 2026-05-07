using Sabine.Shared.Data.Databases;
using Xunit;

namespace Tests.Sabine.Shared.Refine
{
	public class RefineBonusTests
	{
		// Lv1 weapon (compact-form expansion): safe 2/refine through
		// +7, then risky 3/refine through +10. Bonus is whole ATK.
		[Theory]
		[InlineData(0, 0)]    // unrefined
		[InlineData(1, 2)]    // 1 safe step
		[InlineData(7, 14)]   // all 7 safe steps
		[InlineData(8, 17)]   // 7*2 + 1*3
		[InlineData(10, 23)]  // 7*2 + 3*3
		[InlineData(15, 23)]  // capped at max refine
		[InlineData(-1, 0)]
		public void Lv1_weapon_bonus(int refineLevel, int expected)
		{
			Assert.Equal(expected, MakeWeaponLv1().GetRawBonus(refineLevel));
		}

		// Lv4 weapon: safe 7/risky 13/safeLimit 4.
		[Theory]
		[InlineData(4, 28)]   // 4*7
		[InlineData(5, 41)]   // 4*7 + 1*13
		[InlineData(10, 106)] // 4*7 + 6*13
		public void Lv4_weapon_bonus(int refineLevel, int expected)
		{
			Assert.Equal(expected, MakeWeaponLv4().GetRawBonus(refineLevel));
		}

		// Armor raw: hundredths of DEF (service divides by 100).
		[Theory]
		[InlineData(1, 70)]
		[InlineData(4, 280)]
		[InlineData(10, 304)]
		public void Armor_raw_bonus(int refineLevel, int expected)
		{
			Assert.Equal(expected, MakeArmor().GetRawBonus(refineLevel));
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(1, 0)]   // 70 / 100
		[InlineData(2, 1)]   // 140 / 100
		[InlineData(4, 2)]   // 280 / 100
		[InlineData(10, 3)]  // 304 / 100
		public void Armor_def_after_scaling(int refineLevel, int expected)
		{
			Assert.Equal(expected, MakeArmor().GetRawBonus(refineLevel) / 100);
		}

		[Fact]
		public void Max_refine_level_matches_data_length()
		{
			Assert.Equal(10, MakeWeaponLv1().MaxRefineLevel);
		}

		// Helpers build fixtures via the compact-loader expansion
		// path so we exercise the same transform real data goes
		// through.

		private static RefineEntry MakeWeaponLv1()
			=> LoadCompactWeapon(1, 2, 3, 7, new[] { 100, 100, 100, 100, 100, 100, 100, 60, 40, 20 });

		private static RefineEntry MakeWeaponLv4()
			=> LoadCompactWeapon(4, 7, 13, 4, new[] { 100, 100, 100, 100, 60, 40, 40, 20, 20, 10 });

		private static RefineEntry MakeArmor()
			=> LoadCompactArmor(70, 4, 4, new[] { 100, 100, 100, 100, 60, 40, 40, 20, 20, 10 });

		private static RefineEntry LoadCompactWeapon(int weaponLevel, int safe, int risky, int safeLimit, int[] rates)
		{
			var json = "[{ group: \"Weapon\", weaponLevel: " + weaponLevel
				+ ", safeBonus: " + safe + ", riskyBonus: " + risky
				+ ", safeLimit: " + safeLimit + ", successRates: ["
				+ string.Join(",", rates) + "] }]";
			return TestHelpers.LoadRefineDb(json).GetWeapon(weaponLevel);
		}

		private static RefineEntry LoadCompactArmor(int safe, int risky, int safeLimit, int[] rates)
		{
			var json = "[{ group: \"Armor\", weaponLevel: 0"
				+ ", safeBonus: " + safe + ", riskyBonus: " + risky
				+ ", safeLimit: " + safeLimit + ", successRates: ["
				+ string.Join(",", rates) + "] }]";
			return TestHelpers.LoadRefineDb(json).GetArmor();
		}
	}
}
