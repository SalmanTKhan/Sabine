using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_HEAL)]
	public class HealHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return Task.CompletedTask;
			}

			// Heal formula based on Base Level and INT. Example:
			var healAmount = (caster.Parameters.BaseLevel + caster.Parameters.Int) / 8 * (4 + (8 * skill.Level));

			// TODO: If target is Undead, deal Holy damage instead of healing.
			// if (targetCharacter.Property == Property.Undead) { ... }

			targetCharacter.HealHp(healAmount);

			// Client needs a specific packet to show the green heal number.
			if (target is PlayerCharacter targetPlayer)
			{
				Send.ZC_RECOVERY(targetPlayer, ParameterType.Hp, healAmount);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, healAmount, 0, 0, ActionType.Skill);

			return Task.CompletedTask;
		}
	}
}