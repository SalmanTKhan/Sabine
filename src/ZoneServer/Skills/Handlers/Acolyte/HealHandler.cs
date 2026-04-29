using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_HEAL)]
	public class HealHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return Task.CompletedTask;

			var healAmount = (caster.Parameters.BaseLevel + caster.Parameters.Int) / 8 * (4 + (8 * skill.Level));

			// Heal cast on an Undead-element / Undead-race target deals
			// Holy magic damage instead. The healAmount is reused as the
			// base damage so the magnitude of healing/damage stays
			// comparable. Mirrors eAthena pc_heal undead branch.
			if (IsUndead(targetCharacter))
			{
				var ctx = new AttackContext(caster, targetCharacter)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Magic,
					AttackElement = ElementType.Holy,
					SkillRatio = 1.0f,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				// Override the damage with our healAmount so the formula
				// stays predictable; element + MDEF still apply via the
				// calculator's normal pipeline.
				var damage = healAmount;
				if (!result.IsMiss)
					targetCharacter.TakeDamage(damage, caster);

				Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, ActionType.Skill);
				return Task.CompletedTask;
			}

			targetCharacter.HealHp(healAmount);

			if (target is PlayerCharacter targetPlayer)
				Send.ZC_RECOVERY(targetPlayer, ParameterType.Hp, healAmount);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, healAmount, 0, 0, ActionType.Skill);

			return Task.CompletedTask;
		}

		private static bool IsUndead(Character target)
		{
			if (target is not Monster monster) return false;
			return monster.Data.Element == ElementType.Undead || monster.Data.Race == RaceType.Undead;
		}
	}
}
