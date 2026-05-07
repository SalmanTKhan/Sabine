using Sabine.Shared.Data.Databases;
using Sabine.Shared.Skills.Refining;
using Xunit;

namespace Tests.Sabine.Shared.Refine
{
	public class RefineCostTableTests
	{
		[Theory]
		[InlineData(1, RefineCostTable.Phracon, 50)]
		[InlineData(2, RefineCostTable.Emveretarcon, 200)]
		[InlineData(3, RefineCostTable.Oridecon, 5000)]
		[InlineData(4, RefineCostTable.Oridecon, 20000)]
		public void Weapon_costs_match_classic_pricing(int weaponLevel, int expectedItemId, int expectedZeny)
		{
			var cost = RefineCostTable.Get(RefineGroup.Weapon, weaponLevel);
			Assert.Equal(expectedItemId, cost.MaterialItemId);
			Assert.Equal(expectedZeny, cost.Zeny);
		}

		[Fact]
		public void Armor_uses_elunium_at_two_thousand_zeny()
		{
			var cost = RefineCostTable.Get(RefineGroup.Armor, 0);
			Assert.Equal(RefineCostTable.Elunium, cost.MaterialItemId);
			Assert.Equal(2000, cost.Zeny);
		}

		[Fact]
		public void Unknown_weapon_level_returns_no_cost()
		{
			var cost = RefineCostTable.Get(RefineGroup.Weapon, 0);
			Assert.Equal(0, cost.MaterialItemId);
			Assert.Equal(0, cost.Zeny);
		}
	}
}
