using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_ENDURE)]
	public class EndureHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// Endure is a self-buff.
			// TODO: Implement a status effect system to grant "Endure" state.
			// This state prevents the character's attack/cast animation from being interrupted by damage.
			// Duration: 10 + (3 * skill.level) seconds (example)
			var duration = 10000 + (3000 * skill.Level);

			// Send skill usage animation
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Endure is not fully implemented yet.");
			}

			return Task.CompletedTask;
		}
	}
}