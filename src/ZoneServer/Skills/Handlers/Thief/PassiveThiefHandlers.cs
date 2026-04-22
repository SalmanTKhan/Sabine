using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_DOUBLE)]
	public class DoubleAttackHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// This is a passive skill. Its logic is triggered during normal attacks.
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.TF_MISS)]
	public class DodgeIncreaseHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// This is a passive skill. Its effects are applied during character stat recalculation.
			return Task.CompletedTask;
		}
	}
}