using Sabine.Shared.Data.Databases;
using Xunit;

namespace Tests.Sabine.Shared.Refine
{
	public class RefineDbTests
	{
		[Fact]
		public void Compact_form_loads_armor_and_weapon()
		{
			var db = TestHelpers.LoadRefineDb(ClassicJson);

			var armor = db.GetArmor();
			Assert.NotNull(armor);
			Assert.Equal(10, armor.MaxRefineLevel);
			Assert.Equal(70, armor.GetRawBonus(1));

			var weapon3 = db.GetWeapon(3);
			Assert.NotNull(weapon3);
			Assert.Equal(5, weapon3.GetRawBonus(1));
			// Lv3 has safeLimit 5, so +6 = 5*5 + 1*8 = 33.
			Assert.Equal(33, weapon3.GetRawBonus(6));
		}

		[Fact]
		public void Missing_weapon_level_returns_null()
		{
			var db = TestHelpers.LoadRefineDb(ClassicJson);
			Assert.Null(db.GetWeapon(7));
		}

		[Fact]
		public void Compact_classic_break_above_safe_limit()
		{
			var db = TestHelpers.LoadRefineDb(ClassicJson);
			var weapon1 = db.GetWeapon(1);

			// Lv1 weapon safeLimit = 7. Below the limit, the synthesized
			// Normal chance has BreakingRate 0; above it, 100.
			Assert.Equal(0, weapon1.GetChance(0, RefineCostType.Normal).BreakingRate);
			Assert.Equal(100, weapon1.GetChance(7, RefineCostType.Normal).BreakingRate);
			Assert.Equal(0, weapon1.GetChance(0, RefineCostType.Normal).DowngradeAmount);
		}

		[Fact]
		public void Expanded_form_loads_per_level_chances()
		{
			var db = TestHelpers.LoadRefineDb(@"[{
				group: ""Armor"", weaponLevel: 0,
				levels: [
					{ level: 1, bonus: 100, chances: [
						{ type: ""Normal"",   rate: 100, material: 985, price: 2000 },
						{ type: ""HD"",       rate: 100, material: 6226, price: 20000 }
					]},
					{ level: 5, bonus: 600, chances: [
						{ type: ""Normal"", rate: 60, material: 985, price: 2000, breakingRate: 100 },
						{ type: ""HD"",     rate: 60, material: 6226, price: 20000, downgradeAmount: 1 }
					]}
				]
			}]");

			var armor = db.GetArmor();
			Assert.Equal(5, armor.MaxRefineLevel);
			Assert.Equal(100, armor.GetRawBonus(1));
			Assert.Equal(600, armor.GetRawBonus(5));

			// HD ore at +5 attempt: downgrades, doesn't break.
			var hd = armor.GetChance(4, RefineCostType.HD);
			Assert.NotNull(hd);
			Assert.Equal(60, hd.Rate);
			Assert.Equal(0, hd.BreakingRate);
			Assert.Equal(1, hd.DowngradeAmount);

			// Normal ore at +5 attempt: breaks on failure.
			var normal = armor.GetChance(4, RefineCostType.Normal);
			Assert.Equal(100, normal.BreakingRate);
			Assert.Equal(0, normal.DowngradeAmount);
		}

		[Fact]
		public void Available_cost_types_reports_data_order()
		{
			var db = TestHelpers.LoadRefineDb(@"[{
				group: ""Armor"", weaponLevel: 0,
				levels: [{ level: 1, bonus: 100, chances: [
					{ type: ""Normal"",   rate: 100, material: 985, price: 2000 },
					{ type: ""HD"",       rate: 100, material: 6226, price: 20000 },
					{ type: ""Enriched"", rate: 100, material: 7619, price: 2000 }
				]}]
			}]");

			var types = db.GetArmor().GetAvailableCostTypes(0);
			Assert.Equal(new[] { RefineCostType.Normal, RefineCostType.HD, RefineCostType.Enriched }, types);

			// No data for +2 -> returns empty.
			Assert.Empty(db.GetArmor().GetAvailableCostTypes(1));
		}

		[Fact]
		public void Missing_cost_type_returns_zero_rate()
		{
			var db = TestHelpers.LoadRefineDb(ClassicJson);
			// Classic data has no HD chance, even though the level exists.
			Assert.Equal(0, db.GetArmor().GetSuccessRate(0, RefineCostType.HD));
		}

		// Compact eAthena-shaped fixture, kept terse on purpose.
		private const string ClassicJson = @"[
{ group: ""Armor"",  weaponLevel: 0, safeBonus: 70, riskyBonus:  4, safeLimit: 4, successRates: [100,100,100,100,60,40,40,20,20,10] },
{ group: ""Weapon"", weaponLevel: 1, safeBonus:  2, riskyBonus:  3, safeLimit: 7, successRates: [100,100,100,100,100,100,100,60,40,20] },
{ group: ""Weapon"", weaponLevel: 2, safeBonus:  3, riskyBonus:  5, safeLimit: 6, successRates: [100,100,100,100,100,100,60,40,20,20] },
{ group: ""Weapon"", weaponLevel: 3, safeBonus:  5, riskyBonus:  8, safeLimit: 5, successRates: [100,100,100,100,100,60,50,20,20,20] },
{ group: ""Weapon"", weaponLevel: 4, safeBonus:  7, riskyBonus: 13, safeLimit: 4, successRates: [100,100,100,100,60,40,40,20,20,10] }
]";
	}
}
