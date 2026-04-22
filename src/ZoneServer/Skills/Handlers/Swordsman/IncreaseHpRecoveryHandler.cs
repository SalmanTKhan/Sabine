using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_RECOVERY)]
	public class IncreaseHpRecoveryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// This is a passive skill. Its effects are handled by the RecoveryComponent.
			return Task.CompletedTask;
		}
	}
}