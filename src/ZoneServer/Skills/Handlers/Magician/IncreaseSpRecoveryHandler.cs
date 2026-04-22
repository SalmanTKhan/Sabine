using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_SRECOVERY)]
	public class IncreaseSpRecoveryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// This is a passive skill. Its effects are handled by the RecoveryComponent.
			return Task.CompletedTask;
		}
	}
}