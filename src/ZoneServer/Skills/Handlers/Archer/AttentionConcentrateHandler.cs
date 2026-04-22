using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_CONCENTRATION)]
	public class AttentionConcentrateHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// This is a self-buff.
			// TODO: Implement a status effect system to grant AGI and DEX bonuses.
			// Also reveals hidden enemies in a small radius.
			var duration = 40000 + (10000 * (skill.Level / 2)); // Example duration

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Attention Concentrate is not fully implemented yet.");
			}

			return Task.CompletedTask;
		}
	}
}