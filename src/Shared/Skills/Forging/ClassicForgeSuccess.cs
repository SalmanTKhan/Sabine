namespace Sabine.Shared.Skills.Forging
{
	/// <summary>
	/// eAthena classic forge success formula (pc.c::skill_produce_mix,
	/// weapon-forging branch). Numbers match kRO weapon-forging tables.
	/// </summary>
	public class ClassicForgeSuccess : IForgeSuccessStrategy
	{
		public int CalcSuccessPer10000(ForgeContext ctx)
		{
			var wlv = ctx.WeaponLevel;

			var per = 5000 + ctx.JobLevel * 20 + ctx.Dex * 10 + ctx.Luk * 10;

			per += ctx.ForgeSkillLevel * 500;
			per += ctx.WeaponResearchLevel * 100;
			if (wlv >= 3)
				per += ctx.OridecoResearchLevel * 100;

			if (ctx.ElementStone)
				per -= 2000;

			per -= ctx.StarCrumbs * 1500;

			if (wlv > 1)
				per -= wlv * 1000;

			per += ctx.AnvilBonus;

			if (per < 1)
				per = 1;

			return per;
		}
	}
}
