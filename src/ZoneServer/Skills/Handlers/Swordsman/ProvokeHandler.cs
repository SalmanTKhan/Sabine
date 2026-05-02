using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_PROVOKE)]
	public class ProvokeHandler : ITargetedSkillHandler
	{
		// eAthena classic SM_PROVOKE: 30s flat duration, never resists
		// when cast on a player target; BL_MOB has a level-scaled resist
		// roll which we skip in v1 (always lands on monsters).
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			// Bosses are immune in eAthena; closest analogue here is the
			// IsHostileTo check — provoke must be cast on an enemy.
			if (!caster.IsHostileTo(target))
				return;

			var duration = TimeSpan.FromSeconds(30);
			target.StatusEffects.Start(StatusId.Provoke, level, duration, caster, level);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}
}
