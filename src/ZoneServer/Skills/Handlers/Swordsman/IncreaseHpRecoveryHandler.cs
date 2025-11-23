using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_RECOVERY)]
	public class IncreaseHpRecoveryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// This is a passive skill. Its effects are handled by the RecoveryComponent.
			return Task.CompletedTask;
		}
	}
}