using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_SIGHT)]
	public class SightHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// eAthena classic MG_SIGHT: ~10s buff that reveals hidden
			// enemies in radius 7. The reveal sweep happens at cast time
			// (before the status is started) so anyone hiding nearby is
			// dispelled immediately even if they're caught walking in
			// before our visibility tick.
			var radius = 7;
			foreach (var other in caster.Map.GetCharactersInRange(caster.Position, radius))
			{
				if (other == caster) continue;
				if (other.IsHidden)
					other.StatusEffects.Stop(StatusId.Hiding);
			}

			var duration = TimeSpan.FromSeconds(10);
			caster.StatusEffects.Start(StatusId.Sight, skill.Level, duration, caster);

			Send.ZC_SKILL_ENTRY(caster, skill.Id, caster.Position);

			return Task.CompletedTask;
		}
	}
}
