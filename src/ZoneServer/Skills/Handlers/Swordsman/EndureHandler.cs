using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_ENDURE)]
	public class EndureHandler : ITargetedSkillHandler
	{
		// eAthena classic SM_ENDURE: ~10s + 3s per level.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var duration = TimeSpan.FromSeconds(10 + 3 * level);
			caster.StatusEffects.Start(StatusId.Endure, level, duration, caster, level);
		}
	}
}
