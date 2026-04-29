using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_LOUD)]
	public class LoudExclamationHandler : ISkillHandler
	{
		// eAthena classic MC_LOUD: 300s flat duration, +4 STR.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			var duration = TimeSpan.FromSeconds(300);
			caster.StatusEffects.Start(StatusId.LoudExclamation, skill.Level, duration, caster);

			return Task.CompletedTask;
		}
	}
}
