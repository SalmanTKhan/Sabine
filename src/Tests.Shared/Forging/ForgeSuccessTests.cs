using Sabine.Shared.Skills.Forging;
using Xunit;

namespace Tests.Sabine.Shared.Forging
{
	public class ForgeSuccessTests
	{
		// Reference (eAthena classic, skill.c::skill_produce_mix
		// weapon-forging branch):
		//   per = 5000 + jobLv*20 + dex*10 + luk*10
		//       + forgeSkillLv*500 + weaponResearchLv*100
		//       + (wlv>=3 ? oridecoLv*100 : 0)
		//       - (elementStone ? 2000 : 0)
		//       - starCrumbs*1500
		//       - (wlv>1 ? wlv*1000 : 0)
		//       + anvilBonus

		[Fact]
		public void Baseline_lv1_no_modifiers()
		{
			var ctx = new ForgeContext(dex: 0, luk: 0, jobLevel: 1, weaponLevel: 1,
				forgeSkillLevel: 0, weaponResearchLevel: 0, oridecoResearchLevel: 0,
				starCrumbs: 0, elementStone: false, anvilBonus: 0);

			// 5000 + 20 = 5020
			Assert.Equal(5020, new ClassicForgeSuccess().CalcSuccessPer10000(ctx));
		}

		[Fact]
		public void Statted_blacksmith_lv3_with_research_and_anvil()
		{
			var ctx = new ForgeContext(dex: 90, luk: 50, jobLevel: 50, weaponLevel: 3,
				forgeSkillLevel: 5, weaponResearchLevel: 10, oridecoResearchLevel: 5,
				starCrumbs: 0, elementStone: false, anvilBonus: 1000);

			// 5000 + 1000 (job) + 900 (dex) + 500 (luk) = 7400
			//      + 2500 (forge) + 1000 (research) + 500 (orideco wlv>=3) = 11400
			//      - 0 (no stone) - 0 (no crumbs) - 3000 (wlv penalty) = 8400
			//      + 1000 (emperium anvil) = 9400
			Assert.Equal(9400, new ClassicForgeSuccess().CalcSuccessPer10000(ctx));
		}

		[Fact]
		public void Element_stone_and_star_crumbs_cost_chance()
		{
			var ctx = new ForgeContext(dex: 80, luk: 40, jobLevel: 40, weaponLevel: 2,
				forgeSkillLevel: 5, weaponResearchLevel: 10, oridecoResearchLevel: 0,
				starCrumbs: 3, elementStone: true, anvilBonus: 500);

			// 5000 + 800 + 800 + 400 = 7000
			//      + 2500 + 1000 = 10500
			//      - 2000 (stone) - 4500 (3*1500 crumbs) - 2000 (wlv2 penalty) = 2000
			//      + 500 (golden anvil) = 2500
			Assert.Equal(2500, new ClassicForgeSuccess().CalcSuccessPer10000(ctx));
		}

		[Fact]
		public void Result_clamped_to_at_least_one()
		{
			var ctx = new ForgeContext(dex: 0, luk: 0, jobLevel: 1, weaponLevel: 3,
				forgeSkillLevel: 0, weaponResearchLevel: 0, oridecoResearchLevel: 0,
				starCrumbs: 3, elementStone: true, anvilBonus: 0);

			// 5000 + 20 - 2000 - 4500 - 3000 = -4480 -> clamped to 1
			Assert.Equal(1, new ClassicForgeSuccess().CalcSuccessPer10000(ctx));
		}

		[Fact]
		public void Classic_ignores_random_boost()
		{
			// rAthena's per-cast random term is renewal-only; classic
			// must produce the same number whether or not we hand it a
			// random boost.
			var withoutRoll = new ForgeContext(dex: 50, luk: 30, jobLevel: 40, weaponLevel: 2,
				forgeSkillLevel: 5, weaponResearchLevel: 5, oridecoResearchLevel: 0,
				starCrumbs: 1, elementStone: false, anvilBonus: 300, randomBoost: 0);
			var withRoll = new ForgeContext(dex: 50, luk: 30, jobLevel: 40, weaponLevel: 2,
				forgeSkillLevel: 5, weaponResearchLevel: 5, oridecoResearchLevel: 0,
				starCrumbs: 1, elementStone: false, anvilBonus: 300, randomBoost: 750);

			var classic = new ClassicForgeSuccess();
			Assert.Equal(classic.CalcSuccessPer10000(withoutRoll), classic.CalcSuccessPer10000(withRoll));
		}

		// Reference (rAthena renewal, skill.cpp::skill_produce_mix
		// weapon-forging branch):
		//   per = jobLv*20 + dex*10 + luk*10 + randomBoost
		//       + (wlv in 1..3 ? (4/wlv)*1000 : 0)
		//       + forgeSkillLv*500 + weaponResearchLv*100
		//       + (wlv>=3 ? oridecoLv*100 : 0)
		//       - (elementStone ? 2500 : 0)
		//       - starCrumbs*1500
		//       + anvilBonus

		[Fact]
		public void Renewal_baseline_lv1_no_modifiers_no_random()
		{
			var ctx = new ForgeContext(dex: 0, luk: 0, jobLevel: 1, weaponLevel: 1,
				forgeSkillLevel: 0, weaponResearchLevel: 0, oridecoResearchLevel: 0,
				starCrumbs: 0, elementStone: false, anvilBonus: 0, randomBoost: 0);

			// 20 (job) + 4000 (wlv1 bonus) = 4020
			Assert.Equal(4020, new RenewalForgeSuccess().CalcSuccessPer10000(ctx));
		}

		[Fact]
		public void Renewal_statted_blacksmith_lv3_with_research_and_anvil()
		{
			var ctx = new ForgeContext(dex: 90, luk: 50, jobLevel: 50, weaponLevel: 3,
				forgeSkillLevel: 5, weaponResearchLevel: 10, oridecoResearchLevel: 5,
				starCrumbs: 0, elementStone: false, anvilBonus: 1000, randomBoost: 500);

			// 1000 (job) + 900 (dex) + 500 (luk) + 500 (random) = 2900
			//      + 1000 (wlv3 bonus: (4/3)*1000 == 1000 in integer
			//             arithmetic, matching rAthena's C++ behavior) = 3900
			//      + 2500 (forge) + 1000 (research) + 500 (orideco wlv>=3) = 7900
			//      - 0 (no stone/crumbs) + 1000 (emperium anvil) = 8900
			Assert.Equal(8900, new RenewalForgeSuccess().CalcSuccessPer10000(ctx));
		}

		[Fact]
		public void Renewal_element_stone_penalty_is_2500()
		{
			var withoutStone = new ForgeContext(dex: 50, luk: 30, jobLevel: 40, weaponLevel: 2,
				forgeSkillLevel: 5, weaponResearchLevel: 5, oridecoResearchLevel: 0,
				starCrumbs: 0, elementStone: false, anvilBonus: 0, randomBoost: 0);
			var withStone = new ForgeContext(dex: 50, luk: 30, jobLevel: 40, weaponLevel: 2,
				forgeSkillLevel: 5, weaponResearchLevel: 5, oridecoResearchLevel: 0,
				starCrumbs: 0, elementStone: true, anvilBonus: 0, randomBoost: 0);

			var renewal = new RenewalForgeSuccess();
			Assert.Equal(2500, renewal.CalcSuccessPer10000(withoutStone) - renewal.CalcSuccessPer10000(withStone));
		}

		[Fact]
		public void Renewal_random_boost_is_additive()
		{
			// Caller rolls rnd_value(1,100)*10; with the same other
			// inputs the strategy adds it linearly.
			var noRoll = new ForgeContext(dex: 50, luk: 30, jobLevel: 40, weaponLevel: 2,
				forgeSkillLevel: 5, weaponResearchLevel: 5, oridecoResearchLevel: 0,
				starCrumbs: 1, elementStone: false, anvilBonus: 300, randomBoost: 0);
			var bigRoll = new ForgeContext(dex: 50, luk: 30, jobLevel: 40, weaponLevel: 2,
				forgeSkillLevel: 5, weaponResearchLevel: 5, oridecoResearchLevel: 0,
				starCrumbs: 1, elementStone: false, anvilBonus: 300, randomBoost: 1000);

			var renewal = new RenewalForgeSuccess();
			Assert.Equal(1000, renewal.CalcSuccessPer10000(bigRoll) - renewal.CalcSuccessPer10000(noRoll));
		}
	}
}
