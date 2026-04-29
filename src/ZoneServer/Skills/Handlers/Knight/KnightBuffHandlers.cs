using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Knight
{
	[SkillHandler(SkillId.KN_TWOHANDQUICKEN)]
	public class TwoHandQuickenHandler : BuffSkillHandler
	{
		// eAthena KN_TWOHANDQUICKEN: 30s * level ASPD buff. Self-only.
		protected override StatusId StatusId => StatusId.TwoHandQuicken;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30 * level);
	}

	[SkillHandler(SkillId.KN_AUTOCOUNTER)]
	public class AutoCounterHandler : BuffSkillHandler
	{
		// eAthena KN_AUTOCOUNTER: 1s window in which the next melee
		// attack against the caster is countered with a guaranteed
		// critical. Short status that expires on its own.
		protected override StatusId StatusId => StatusId.AutoCounter;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(1);
	}

	[SkillHandler(SkillId.KN_RIDING)]
	public class RidingHandler : BuffSkillHandler
	{
		// eAthena KN_RIDING: PecoPeco mount toggle. Movement speed
		// boost while active. v1 omits the sprite swap; the speed
		// bonus comes from the status handler. Recasting toggles.
		protected override StatusId StatusId => StatusId.Riding;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromHours(24);
	}
}
