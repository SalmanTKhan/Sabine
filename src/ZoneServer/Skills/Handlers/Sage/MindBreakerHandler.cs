using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Sage
{
	/// <summary>
	/// PF_MINDBREAKER — target debuff that exposes the enemy to magic by
	/// boosting their outgoing MATK while crushing their MDEF. eAthena
	/// skill.c also flips a Confusion onto the target and Sleep onto the
	/// caster as the cast's psychic cost; Sabine ports just the stat
	/// payload in this slice. Status duration: 60s + 30s/level.
	/// </summary>
	[SkillHandler(SkillId.PF_MINDBREAKER)]
	public class MindBreakerHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;
			if (!caster.IsHostileTo(target)) return;

			var duration = TimeSpan.FromSeconds(60 + 30 * level);
			target.StatusEffects.Start(StatusId.MindBreaker, level, duration, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}
}
