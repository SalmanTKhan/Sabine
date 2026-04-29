using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_HIDING)]
	public class HidingHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// Toggling: if Hiding is already active, end it.
			if (caster.StatusEffects.Has(StatusId.Hiding))
			{
				caster.StatusEffects.Stop(StatusId.Hiding);
				return Task.CompletedTask;
			}

			// eAthena classic TF_HIDING: 30s + 30s/level (clamped).
			var duration = TimeSpan.FromSeconds(30 * skill.Level);
			caster.StatusEffects.Start(StatusId.Hiding, skill.Level, duration, caster);

			Send.ZC_NOTIFY_VANISH(caster, DisappearType.Vanish);

			return Task.CompletedTask;
		}
	}
}
