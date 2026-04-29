using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Priest
{
	[SkillHandler(SkillId.PR_IMPOSITIO)]
	public class ImpositioManusHandler : BuffSkillHandler
	{
		// eAthena PR_IMPOSITIO: +5 ATK per level for 60s.
		protected override StatusId StatusId => StatusId.ImpositioManus;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60);
	}

	[SkillHandler(SkillId.PR_SUFFRAGIUM)]
	public class SuffragiumHandler : BuffSkillHandler
	{
		// eAthena PR_SUFFRAGIUM: cast time -15%/-30%/-45% for 30s
		// or until next cast. Stored level → reduction in val1.
		protected override StatusId StatusId => StatusId.Suffragium;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30);
	}

	[SkillHandler(SkillId.PR_ASPERSIO)]
	public class AspersioHandler : BuffSkillHandler
	{
		// eAthena PR_ASPERSIO: weapon element → Holy for 60s. Costs
		// one Holy Water; the consumption check is a follow-up.
		protected override StatusId StatusId => StatusId.Aspersio;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60 + 30 * level);
	}

	[SkillHandler(SkillId.PR_KYRIE)]
	public class KyrieEleisonHandler : BuffSkillHandler
	{
		// eAthena PR_KYRIE: damage shield with HP cap = (BaseLv + level*2)%.
		// 2 minutes or until shield is broken.
		protected override StatusId StatusId => StatusId.KyrieEleison;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromMinutes(2);
	}

	[SkillHandler(SkillId.PR_MAGNIFICAT)]
	public class MagnificatHandler : BuffSkillHandler
	{
		// eAthena PR_MAGNIFICAT: SP regen *2 for 30s * level.
		protected override StatusId StatusId => StatusId.Magnificat;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30 * level);
	}

	[SkillHandler(SkillId.PR_GLORIA)]
	public class GloriaHandler : BuffSkillHandler
	{
		// eAthena PR_GLORIA: +30 LUK for a short window (30s).
		protected override StatusId StatusId => StatusId.Gloria;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30);
	}

	[SkillHandler(SkillId.PR_LEXAETERNA)]
	public class LexAeternaHandler : BuffSkillHandler
	{
		// eAthena PR_LEXAETERNA: target takes 2x damage on next hit.
		// Consumes itself when the next attack lands; if no attack
		// arrives, it persists indefinitely (capped at 1h here).
		protected override StatusId StatusId => StatusId.LexAeterna;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromHours(1);
	}

	[SkillHandler(SkillId.PR_LEXDIVINA)]
	public class LexDivinaHandler : BuffSkillHandler
	{
		// eAthena PR_LEXDIVINA: Silence for 30s + 30s/level.
		protected override StatusId StatusId => StatusId.Silence;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30 + 30 * level);
	}
}
