using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_MAKINGARROW)]
	public class MakingArrowHandler : ISkillHandler
	{
		// eAthena AC_MAKINGARROW: opens an arrow-creation UI that
		// converts eligible source items (bones, branches, gemstones,
		// element ores) into typed arrows. The recipe table and
		// crafting packet are a follow-up; v1 plays the animation
		// and tells the player.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
				pc.ServerMessage("Arrow Crafting UI is not yet available.");

			return Task.CompletedTask;
		}
	}
}
