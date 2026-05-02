using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_HIDING)]
	public class HidingHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var level = parameters.SkillLevel;

			// Toggling: if Hiding is already active, end it.
			if (caster.StatusEffects.Has(StatusId.Hiding))
			{
				caster.StatusEffects.Stop(StatusId.Hiding);
				return;
			}

			// eAthena classic TF_HIDING: 30s + 30s/level (clamped).
			var duration = TimeSpan.FromSeconds(30 * level);
			caster.StatusEffects.Start(StatusId.Hiding, level, duration, caster);

			Send.ZC_NOTIFY_VANISH(caster, DisappearType.Vanish);
		}
	}
}
