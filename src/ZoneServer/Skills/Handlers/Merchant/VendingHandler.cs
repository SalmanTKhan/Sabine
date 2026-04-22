using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_VENDING)]
	public class VendingHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not PlayerCharacter pc)
			{
				return Task.CompletedTask;
			}

			// The number of items that can be vended depends on the skill level.
			var itemCount = 2 + skill.Level;

			// This packet opens the vending setup window on the client.
			Send.ZC_OPENSTORE(pc, itemCount);

			return Task.CompletedTask;
		}
	}
}