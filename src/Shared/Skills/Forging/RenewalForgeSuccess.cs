namespace Sabine.Shared.Skills.Forging
{
	/// <summary>
	/// rAthena renewal-branch forge success formula
	/// (skill.cpp::skill_produce_mix, weapon-forging branch).
	/// </summary>
	/// <remarks>
	/// Diverges from <see cref="ClassicForgeSuccess"/> in four places:
	/// no flat 5000 base, weapon level contributes (4/wlv)*1000 as a
	/// *bonus* (+4000/+2000/+1333) instead of a penalty, element-stone
	/// penalty is -2500 (vs -2000), and the Oridecon anvil bonus is
	/// 250 (vs 300). A per-cast `rnd_value(1,100)*10` term is also
	/// added, which the caller rolls and passes in via
	/// <see cref="ForgeContext.RandomBoost"/>.
	/// </remarks>
	public class RenewalForgeSuccess : IForgeSuccessStrategy
	{
		public int CalcSuccessPer10000(ForgeContext ctx)
		{
			var wlv = ctx.WeaponLevel;

			var per = ctx.JobLevel * 20 + ctx.Dex * 10 + ctx.Luk * 10;
			per += ctx.RandomBoost;

			if (wlv > 0 && wlv < 4)
				per += (4 / wlv) * 1000;

			per += ctx.ForgeSkillLevel * 500;
			per += ctx.WeaponResearchLevel * 100;
			if (wlv >= 3)
				per += ctx.OridecoResearchLevel * 100;

			if (ctx.ElementStone)
				per -= 2500;

			per -= ctx.StarCrumbs * 1500;

			per += ctx.AnvilBonus;

			if (per < 1)
				per = 1;

			return per;
		}
	}
}
