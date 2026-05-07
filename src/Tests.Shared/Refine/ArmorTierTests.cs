using Sabine.Shared.Data.Databases;
using Xunit;

namespace Tests.Sabine.Shared.Refine
{
	public class ArmorTierTests
	{
		[Fact]
		public void Compact_armor_normalizes_weapon_level_zero_to_tier_one()
		{
			var db = TestHelpers.LoadRefineDb(@"[
				{ group: ""Armor"", weaponLevel: 0, safeBonus: 70, riskyBonus: 4, safeLimit: 4, successRates: [100,100,100,100,60,40,40,20,20,10] }
			]");

			Assert.NotNull(db.GetArmor());
			Assert.NotNull(db.GetArmor(1));
			Assert.Equal(1, db.GetArmor().Tier);
		}

		[Fact]
		public void Get_armor_falls_back_to_lv1_when_requested_tier_missing()
		{
			var db = TestHelpers.LoadRefineDb(@"[
				{ group: ""Armor"", armorLevel: 1, levels: [{ level: 1, bonus: 100, chances: [{ type: ""Normal"", rate: 100, material: 985, price: 2000 }] }] }
			]");

			// No lv2 row; the lv1 fallback wins.
			var lv2Lookup = db.GetArmor(2);
			Assert.NotNull(lv2Lookup);
			Assert.Equal(1, lv2Lookup.Tier);
		}

		[Fact]
		public void Distinct_armor_tiers_load_independently()
		{
			var db = TestHelpers.LoadRefineDb(@"[
				{ group: ""Armor"", armorLevel: 1, levels: [{ level: 1, bonus: 100, chances: [{ type: ""Normal"", rate: 100, material: 985, price: 2000 }] }] },
				{ group: ""Armor"", armorLevel: 2, levels: [{ level: 1, bonus: 120, chances: [{ type: ""Normal"", rate: 100, material: 1000331, price: 50000 }] }] }
			]");

			var lv1 = db.GetArmor(1);
			var lv2 = db.GetArmor(2);

			Assert.Equal(100, lv1.GetRawBonus(1));
			Assert.Equal(120, lv2.GetRawBonus(1));
			Assert.Equal(985, lv1.GetChance(0, RefineCostType.Normal).MaterialItemId);
			Assert.Equal(1000331, lv2.GetChance(0, RefineCostType.Normal).MaterialItemId);
		}

		[Fact]
		public void Armor_lv2_failure_uses_downgrade_not_break()
		{
			var db = TestHelpers.LoadRefineDb(@"[
				{ group: ""Armor"", armorLevel: 2, levels: [
					{ level: 4, bonus: 480, chances: [
						{ type: ""Normal"", rate: 60, material: 1000331, price: 50000, downgradeAmount: 3 }
					]}
				]}
			]");

			var chance = db.GetArmor(2).GetChance(3, RefineCostType.Normal);
			Assert.Equal(0, chance.BreakingRate);
			Assert.Equal(3, chance.DowngradeAmount);
		}

		[Fact]
		public void Tier_field_overrides_legacy_aliases()
		{
			var db = TestHelpers.LoadRefineDb(@"[
				{ group: ""Weapon"", tier: 2, weaponLevel: 1, levels: [{ level: 1, bonus: 3, chances: [{ type: ""Normal"", rate: 100, material: 1011, price: 200 }] }] }
			]");

			// tier wins; the row should index under weapon level 2.
			Assert.NotNull(db.GetWeapon(2));
			Assert.Null(db.GetWeapon(1));
		}
	}
}
