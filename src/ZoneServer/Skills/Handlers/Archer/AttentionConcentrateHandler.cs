using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_CONCENTRATION)]
	public class AttentionConcentrateHandler : ITargetedSkillHandler
	{
		// eAthena classic AC_CONCENTRATION duration: 30s + 10s/level.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var duration = TimeSpan.FromSeconds(30 + 10 * level);
			caster.StatusEffects.Start(StatusId.Concentration, level, duration, caster, level);
		}
	}
}
