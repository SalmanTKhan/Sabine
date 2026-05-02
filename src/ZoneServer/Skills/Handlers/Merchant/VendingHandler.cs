using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_VENDING)]
	public class VendingHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			if (parameters.Character is not PlayerCharacter pc)
				return;

			// The number of items that can be vended depends on the skill level.
			var itemCount = 2 + parameters.SkillLevel;

			// This packet opens the vending setup window on the client.
			Send.ZC_OPENSTORE(pc, itemCount);
		}
	}
}
