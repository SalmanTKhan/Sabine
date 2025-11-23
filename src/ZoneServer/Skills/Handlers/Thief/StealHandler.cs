using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_STEAL)]
	public class StealHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (target is not Monster monster)
			{
				return Task.CompletedTask;
			}

			// TODO: Implement proper monster drop tables and steal rates.
			// var stealChance = 10 + (4 * skill.Level); // Example
			// if (Random.Next(100) < stealChance) { ... steal an item ... }

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Steal is not fully implemented yet.");
			}

			return Task.CompletedTask;
		}
	}
}