using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.LordKnight
{
	/// <summary>
	/// LK_AURABLADE — adds level*10 flat ATK to physical attacks while
	/// active. eAthena status.c SC_AURABLADE; the +ATK lives in the
	/// damage pipeline (battle.c) so the status itself is a marker
	/// today. Duration: 20s + 20s/level.
	/// </summary>
	[SkillHandler(SkillId.LK_AURABLADE)]
	public class AuraBladeHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.AuraBlade;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(20 + 20 * level);
	}

	/// <summary>
	/// LK_CONCENTRATION — +5%/level ATK and +10%/level Hit, at the
	/// cost of +5%/level damage taken. eAthena status.c
	/// SC_CONCENTRATION (LK variant). The ATK% and Hit deltas land via
	/// the status handler's OnStart; the self-DEF reduction is marker-
	/// only until the damage pipeline grows a hook for it. Duration:
	/// 20s + 5s/level.
	/// </summary>
	[SkillHandler(SkillId.LK_CONCENTRATION)]
	public class ConcentrationHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.LkConcentration;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(20 + 5 * level);
	}
}
