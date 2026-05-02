using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Novice
{
	[SkillHandler(SkillId.NV_TRICKDEAD)]
	public class TrickDeadHandler : ITargetedSkillHandler
	{
		// eAthena NV_TRICKDEAD: toggles a play-dead state. Mob aggro
		// drops, the caster cannot act until the status is removed,
		// and HP/SP regen pauses. Single-level skill in classic.
		// TODO: break-on-move/attack/damage hooks per eAthena
		// skill.c:4025, 8174 — needs StatusEffects plumbing changes
		// outside this file.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster.StatusEffects.Has(StatusId.TrickDead))
			{
				caster.StatusEffects.Stop(StatusId.TrickDead);
				return;
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			caster.StatusEffects.Start(StatusId.TrickDead, level, TimeSpan.FromHours(1), caster);
		}
	}
}
