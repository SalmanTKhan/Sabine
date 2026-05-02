using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_LOUD)]
	public class LoudExclamationHandler : ITargetedSkillHandler
	{
		// eAthena classic MC_LOUD: 300s flat duration, +4 STR.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var duration = TimeSpan.FromSeconds(300);
			caster.StatusEffects.Start(StatusId.LoudExclamation, level, duration, caster);
		}
	}
}
