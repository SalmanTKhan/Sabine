using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Assassin
{
	[SkillHandler(SkillId.AS_SONICBLOW)]
	public class SonicBlowHandler : ISkillHandler
	{
		// rAthena pre-renewal: 8 hits at 50% + 50%*lv each.
		// rAthena renewal:  8 hits at 100% + 100%*lv each, ignores
		// 50% DEF, +STR/10 ratio.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var renewal = BattleCalculator.IsRenewal();
			var ratio = renewal
				? 1.0f + 1.0f * skill.Level + caster.Parameters.Str / 1000.0f
				: 0.5f + 0.50f * skill.Level;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				HitCount = 8,
				DefRatio = renewal ? 50 : 100,
				WeaponRequired = true,
				MinDamage = 1,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 8, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AS_GRIMTOOTH)]
	public class GrimtoothHandler : ISkillHandler
	{
		// eAthena AS_GRIMTOOTH: lunging melee, 100% + 20%*lv.
		// Hides reveal extends reach by skill level. Renewal same.
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
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, result.HitCount, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AS_VENOMDUST)]
	public class VenomDustHandler : ISkillHandler
	{
		// eAthena AS_VENOMDUST: ground unit, applies Poison on touch.
		// Costs 1 Red Gemstone. Stub the ingredient consumption.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var unit = new Sabine.Zone.World.Maps.SkillUnits.VenomDustUnit(caster, target.Position, skill.Level);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AS_SPLASHER)]
	public class VenomSplasherHandler : ISkillHandler
	{
		// eAthena AS_SPLASHER (Venom Splasher): tags the target with
		// a 5s timer; on expiry it explodes for AoE damage. Damage
		// depends on the target's missing HP.
		// Simplified v1: deals immediate ratio-based Poison damage.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ratio = 4.0f + 0.50f * skill.Level;
			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				AttackElement = ElementType.Poison,
				WeaponRequired = true,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			// Splash to neighbours.
			foreach (var enemy in caster.Map.GetCharactersInRange(target.Position, 2))
			{
				if (enemy == target || enemy == caster) continue;
				if (!enemy.IsHostileTo(caster)) continue;

				var splash = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Physical,
					SkillRatio = ratio * 0.5f,
					AttackElement = ElementType.Poison,
				};
				var splashResult = BattleCalculator.Calc(splash);
				if (!splashResult.IsMiss)
					enemy.TakeDamage(splashResult.Damage, caster);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, result.HitCount, result.ActionType);
			return Task.CompletedTask;
		}
	}
}
