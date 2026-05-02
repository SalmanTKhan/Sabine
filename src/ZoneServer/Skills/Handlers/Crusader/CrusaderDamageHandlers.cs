using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Crusader
{
	[SkillHandler(SkillId.CR_SHIELDCHARGE)]
	public class ShieldChargeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.20f * level,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				target.TakeDamage(result.Damage, caster);
				if (RandomProvider.Get().Next(100) < 60)
					target.StatusEffects.Start(StatusId.Stun, level, System.TimeSpan.FromSeconds(2 + level), caster);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	[SkillHandler(SkillId.CR_SHIELDBOOMERANG)]
	public class ShieldBoomerangHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.30f * level,
				IsLongRange = true,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	[SkillHandler(SkillId.CR_HOLYCROSS)]
	public class HolyCrossHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var ratio = BattleCalculator.IsRenewal()
				? 2.0f + 0.50f * level
				: 1.0f + 0.35f * level;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				HitCount = 2,
				AttackElement = ElementType.Holy,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 2, result.ActionType);
		}
	}

	[SkillHandler(SkillId.CR_GRANDCROSS)]
	public class GrandCrossHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var ratio = BattleCalculator.IsRenewal()
				? 3.0f + 0.50f * level
				: 1.4f + 0.40f * level;

			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, 2))
			{
				if (enemy == caster) continue;
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Holy,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					enemy.TakeDamage(result.Damage, caster);
			}

			// Self-recoil: 20% of caster's max HP.
			var selfDamage = caster.Parameters.HpMax / 5;
			caster.TakeDamage(selfDamage, caster);

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}
}
