using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

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

			if (!caster.IsHostileTo(target))
				return;

			// eAthena flags SC_PROVOKE with SI_BOSS-immune; bosses and
			// MVPs ignore the status outright and the aggro switch.
			if (target is Monster bossCheck && (bossCheck.Data.IsBoss || bossCheck.Data.MvpExp > 0))
				return;

			var duration = TimeSpan.FromSeconds(30);
			target.StatusEffects.Start(StatusId.Provoke, level, duration, caster, level);

			// Force-target the caster, matching skill.c's mob_target call
			// for SM_PROVOKE — the AI polls AttackerHandleTest each tick and
			// switches into the Combat routine on the next pass.
			if (target is Monster mob)
				mob.AttackerHandleTest = caster.Handle;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}
}
