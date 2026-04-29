using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Crusader
{
	[SkillHandler(SkillId.CR_SHIELDCHARGE)]
	public class ShieldChargeHandler : ISkillHandler
	{
		// eAthena CR_SHIELDCHARGE: 100% + 20%*lv physical melee
		// using shield DEF as bonus damage. 60% chance to Stun.
		// Renewal unchanged.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.20f * skill.Level,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				target.TakeDamage(result.Damage, caster);
				if (Yggdrasil.Util.RandomProvider.Get().Next(100) < 60)
					target.StatusEffects.Start(StatusId.Stun, skill.Level, System.TimeSpan.FromSeconds(2 + skill.Level), caster);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 1, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.CR_SHIELDBOOMERANG)]
	public class ShieldBoomerangHandler : ISkillHandler
	{
		// eAthena CR_SHIELDBOOMERANG: 100% + 30%*lv ranged physical,
		// shield-required. Treated as long-range for cards.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.30f * skill.Level,
				IsLongRange = true,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 1, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.CR_HOLYCROSS)]
	public class HolyCrossHandler : ISkillHandler
	{
		// eAthena CR_HOLYCROSS: 100% + 35%*lv physical, Holy element,
		// 2 hits with spear. Pre-renewal: 2 hits when spear, else 1.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ratio = BattleCalculator.IsRenewal()
				? 2.0f + 0.50f * skill.Level
				: 1.0f + 0.35f * skill.Level;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				HitCount = 2,
				AttackElement = ElementType.Holy,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 2, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.CR_GRANDCROSS)]
	public class GrandCrossHandler : ISkillHandler
	{
		// eAthena CR_GRANDCROSS: AoE Holy magic at 140% + 40%*lv MATK
		// over a 5x5 area, dealing self-damage equal to a fraction
		// of the casting cost. Renewal tunes the formula slightly.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			var ratio = BattleCalculator.IsRenewal()
				? 3.0f + 0.50f * skill.Level
				: 1.4f + 0.40f * skill.Level;

			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, 2))
			{
				if (enemy == caster) continue;
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
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

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}
}
