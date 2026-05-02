using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_HEAL)]
	public class HealHandler : ITargetedSkillHandler
	{
		// eAthena AL_HEAL: heal = (BaseLv + Int)/8 * (4 + 8*Lv).
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			var healAmount = (caster.Parameters.BaseLevel + caster.Parameters.Int) / 8 * (4 + (8 * level));

			// Heal cast on an Undead-element / Undead-race target deals
			// Holy magic damage instead. The healAmount is reused as the
			// base damage so the magnitude of healing/damage stays
			// comparable. Mirrors eAthena pc_heal undead branch.
			if (IsUndead(target))
			{
				var ctx = new AttackContext(caster, target)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					AttackElement = ElementType.Holy,
					SkillRatio = 1.0f,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				var damage = healAmount;
				if (!result.IsMiss)
					target.TakeDamage(damage, caster);

				Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 0, ActionType.Skill);
				return;
			}

			target.HealHp(healAmount);

			if (target is PlayerCharacter targetPlayer)
				Send.ZC_RECOVERY(targetPlayer, ParameterType.Hp, healAmount);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, healAmount, 0, 0, ActionType.Skill);
		}

		private static bool IsUndead(Character target)
		{
			if (target is not Monster monster) return false;
			return monster.Data.Element == ElementType.Undead || monster.Data.Race == RaceType.Undead;
		}
	}
}
