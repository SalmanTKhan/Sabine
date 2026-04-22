using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_STONECURSE)]
	public class StoneCurseHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return Task.CompletedTask;
			}

			// TODO: Check for and consume a Red Gemstone from caster's inventory.

			// TODO: Add proper status effect system for Stone Curse.
			// This has a chance to apply the 'Stone' status.
			// var curseChance = 25 + (3 * skill.Level);
			// if (Random.Next(100) < curseChance) { targetCharacter.Status.Apply(StatusType.Stone); }

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Stone Curse is not fully implemented yet.");
			}

			return Task.CompletedTask;
		}
	}
}