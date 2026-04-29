using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_PROVOKE)]
	public class ProvokeHandler : ISkillHandler
	{
		// eAthena classic SM_PROVOKE: 30s flat duration, never resists
		// when cast on a player target; BL_MOB has a level-scaled resist
		// roll which we skip in v1 (always lands on monsters).
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return Task.CompletedTask;

			// Bosses are immune in eAthena; closest analogue here is the
			// IsHostileTo check — provoke must be cast on an enemy.
			if (!caster.IsHostileTo(targetCharacter))
				return Task.CompletedTask;

			var duration = TimeSpan.FromSeconds(30);
			targetCharacter.StatusEffects.Start(StatusId.Provoke, skill.Level, duration, caster, skill.Level);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			return Task.CompletedTask;
		}
	}
}
