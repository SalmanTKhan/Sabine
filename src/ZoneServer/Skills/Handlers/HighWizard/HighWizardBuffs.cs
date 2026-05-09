using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.HighWizard
{
	/// <summary>
	/// HW_MAGICPOWER — +5%/level MATK on the next spell cast, then
	/// the status ends. eAthena status.c SC_MAGICPOWER. The MATK%
	/// rides the existing BonusMatkRate bucket through the status
	/// handler; the "single-shot, breaks on cast" semantics need a
	/// post-cast hook in CalcMagic and are deferred. Duration: 30s.
	/// </summary>
	[SkillHandler(SkillId.HW_MAGICPOWER)]
	public class MagicPowerHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.MagicPower;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30);
	}
}
