using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_CHANGECART)]
	public class ChangeCartHandler : ITargetedSkillHandler
	{
		// eAthena MC_CHANGECART: swaps the cart sprite (1..5 by skill
		// level). Cart selection UI / per-cart inventory volumes are a
		// follow-up; v1 just plays the animation.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
				pc.ServerMessage("Change Cart selection UI is not yet available.");
		}
	}
}
