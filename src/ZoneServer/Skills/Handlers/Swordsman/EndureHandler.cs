using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_ENDURE)]
	public class EndureHandler : ISkillHandler
	{
		// eAthena classic SM_ENDURE: ~10s + 3s per level.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			var duration = TimeSpan.FromSeconds(10 + 3 * skill.Level);
			caster.StatusEffects.Start(StatusId.Endure, skill.Level, duration, caster, skill.Level);

			return Task.CompletedTask;
		}
	}
}
