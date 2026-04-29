using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_CONCENTRATION)]
	public class AttentionConcentrateHandler : ISkillHandler
	{
		// eAthena classic AC_CONCENTRATION duration: 30s + 10s/level.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			var duration = TimeSpan.FromSeconds(30 + 10 * skill.Level);
			caster.StatusEffects.Start(StatusId.Concentration, skill.Level, duration, caster, skill.Level);

			return Task.CompletedTask;
		}
	}
}
