using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_IDENTIFY)]
	public class IdentifyHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// Item Identification is a special skill. Using it from the skill bar
			// changes the cursor, and the player then clicks an unidentified item.
			// The `HandleAsync` model doesn't fit well.
			// The client sends CZ_REQ_ITEMIDENTIFY when an item is clicked.
			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Please use the skill then click on an unidentified item in your inventory.");
			}
			return Task.CompletedTask;
		}
	}
}