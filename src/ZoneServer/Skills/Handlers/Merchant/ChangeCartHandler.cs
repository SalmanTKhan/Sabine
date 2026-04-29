using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_CHANGECART)]
	public class ChangeCartHandler : ISkillHandler
	{
		// eAthena MC_CHANGECART: swaps the cart sprite (1..5 by skill
		// level). Cart selection UI / per-cart inventory volumes are a
		// follow-up; v1 just plays the animation.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
				pc.ServerMessage("Change Cart selection UI is not yet available.");

			return Task.CompletedTask;
		}
	}
}
