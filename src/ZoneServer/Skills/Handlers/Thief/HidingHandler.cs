using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_HIDING)]
	public class HidingHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// Hiding is a self-buff that makes the character invisible.
			// TODO: Apply 'Hiding' status effect. This should make the character vanish.

			// For now, we manually make the character vanish.
			Send.ZC_NOTIFY_VANISH(caster, DisappearType.Vanish);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Hiding is not fully implemented yet.");
			}

			return Task.CompletedTask;
		}
	}
}