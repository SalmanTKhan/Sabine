using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Novice
{
	[SkillHandler(SkillId.NV_TRICKDEAD)]
	public class TrickDeadHandler : ISkillHandler
	{
		// eAthena NV_TRICKDEAD: toggles a play-dead state. Mob aggro
		// drops, the caster cannot act until the status is removed,
		// and HP/SP regen pauses. Single-level skill in classic.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster.StatusEffects.Has(StatusId.TrickDead))
			{
				caster.StatusEffects.Stop(StatusId.TrickDead);
				return Task.CompletedTask;
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			caster.StatusEffects.Start(StatusId.TrickDead, skill.Level, TimeSpan.FromHours(1), caster);
			return Task.CompletedTask;
		}
	}
}
