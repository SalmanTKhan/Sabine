using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_SWORD)]
	public class SwordMasteryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// This is a passive skill. Its effects are applied during character stat recalculation.
			return Task.CompletedTask;
		}
	}
}