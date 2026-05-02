using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_IDENTIFY)]
	public class IdentifyHandler : ITargetedSkillHandler
	{
		// Item Identification is a special skill. Using it from the skill
		// bar changes the cursor; the player then clicks an unidentified
		// item and the client sends CZ_REQ_ITEMIDENTIFY. The dispatch
		// model here doesn't drive that flow — see PacketHandler.Skills.cs.
		public void Handle(UseSkillParams parameters)
		{
			if (parameters.Character is PlayerCharacter pc)
				pc.ServerMessage("Please use the skill then click on an unidentified item in your inventory.");
		}
	}
}
