using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Ais;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_PROVOKE)]
	public class ProvokeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return Task.CompletedTask;
			}

			// TODO: Add proper status effect system for Provoke.
			// Effect: Increase target's ATK, decrease target's DEF.
			// For now, this is a placeholder direct modification.
			var defenseReduction = 5 + (5 * skill.Level); // 10% to 55% DEF reduction
			var attackIncrease = 2 + (3 * skill.Level);   // 5% to 32% ATK increase

			// This is not how it should be implemented long-term.
			// These should be temporary bonuses/penalties.
			// targetCharacter.Bonuses.Add(BonusType.DefPercent, -defenseReduction, 10000);
			// targetCharacter.Bonuses.Add(BonusType.AtkPercent, attackIncrease, 10000);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Provoke is not fully implemented yet.");
			}

			// Make the target aggressive towards the caster
			// targetCharacter.OnAttacked(caster);

			return Task.CompletedTask;
		}
	}
}
