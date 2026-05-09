using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.HighPriest
{
	/// <summary>
	/// HP_ASSUMPTIO — halves physical damage taken on the target
	/// (eAthena status.c SC_ASSUMPTIO; the 50% reduction lives in
	/// the damage pipeline so the handler stays marker-only here).
	/// Duration: 20s/level. Targetable on allies; the BuffSkillHandler
	/// base routes the status onto parameters.Target.
	/// </summary>
	[SkillHandler(SkillId.HP_ASSUMPTIO)]
	public class AssumptioHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.Assumptio;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(20 * level);
	}
}
