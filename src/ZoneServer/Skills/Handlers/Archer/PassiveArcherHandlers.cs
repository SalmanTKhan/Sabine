using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_OWL)]
	public class OwlsEyeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// This is a passive skill. Its effects are applied during character stat recalculation.
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AC_VULTURE)]
	public class VulturesEyeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// This is a passive skill. Its effects are applied during character stat recalculation.
			return Task.CompletedTask;
		}
	}
}