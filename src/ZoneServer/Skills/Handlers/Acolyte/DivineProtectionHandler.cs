using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_DP)]
	public class DivineProtectionHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// This is a passive skill. Its effects are applied during character stat recalculation.
			return Task.CompletedTask;
		}
	}
}