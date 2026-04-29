using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Monk
{
	[SkillHandler(SkillId.MO_TRIPLEATTACK)]
	public class TripleAttackHandler : ISkillHandler
	{
		// eAthena MO_TRIPLEATTACK: 3-hit physical, opens the
		// combo window for Chain Combo. v1: damage only; combo
		// state plumbing is wired by ChainCombo's check.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.20f * skill.Level,
				HitCount = 3,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			caster.StatusEffects.Start(StatusId.ChainCombo, skill.Level, TimeSpan.FromSeconds(2), caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 3, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MO_CHAINCOMBO)]
	public class ChainComboHandler : ISkillHandler
	{
		// eAthena MO_CHAINCOMBO: 4 hits, requires the Triple Attack
		// combo window. Opens Combo Finish.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.5f + 0.50f * skill.Level,
				HitCount = 4,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			caster.StatusEffects.Stop(StatusId.ChainCombo);
			caster.StatusEffects.Start(StatusId.ComboFinish, skill.Level, TimeSpan.FromSeconds(2), caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 4, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MO_COMBOFINISH)]
	public class ComboFinishHandler : ISkillHandler
	{
		// eAthena MO_COMBOFINISH: 5 hits, requires the Chain Combo
		// window. Final piece of the basic combo chain.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Physical,
					SkillRatio = 2.0f + 0.50f * skill.Level + caster.Parameters.Str / 100.0f,
					HitCount = 5,
					WeaponRequired = true,
				};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			caster.StatusEffects.Stop(StatusId.ComboFinish);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 5, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MO_INVESTIGATE)]
	public class InvestigateHandler : ISkillHandler
	{
		// eAthena MO_INVESTIGATE: damage scales with target DEF
		// (200% + 100*lv * (DEF/100)). Renewal: similar shape with
		// MATK contribution.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var defFactor = Math.Max(1, target.Parameters.MeleeDefense + target.Parameters.MeleeDefenseBonus);
			var ratio = (1.0f + 0.50f * skill.Level) * (defFactor / 50.0f);

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				IgnoreDefense = true,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 1, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MO_FINGEROFFENSIVE)]
	public class FingerOffensiveHandler : ISkillHandler
	{
		// eAthena MO_FINGEROFFENSIVE: 1 hit per spirit ball.
		// 100% + 50%*lv per ball.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var balls = caster.StatusEffects.TryGet(StatusId.Spirits, out var spirits) ? spirits.Val1 : 1;
			if (balls < 1) balls = 1;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.50f * skill.Level,
				HitCount = balls,
				IsLongRange = true,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			caster.StatusEffects.Stop(StatusId.Spirits);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, balls, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MO_EXTREMITYFIST)]
	public class AsuraStrikeHandler : ISkillHandler
	{
		// eAthena MO_EXTREMITYFIST (Asura Strike): consumes all SP,
		// all spirit balls; damage = (8 + lv) * (SP / 5) + STR + 250.
		// Caster cannot regen SP for ~30s afterward.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var sp = caster.Parameters.Sp;
			var balls = caster.StatusEffects.TryGet(StatusId.Spirits, out var spirits) ? spirits.Val1 : 0;
			if (balls < 4)
			{
				if (caster is PlayerCharacter pc)
					pc.ServerMessage("Asura Strike requires at least 4 spirit balls.");
				return Task.CompletedTask;
			}

			caster.Parameters.Modify(ParameterType.Sp, -sp);
			caster.StatusEffects.Stop(StatusId.Spirits);

			var damage = (8 + skill.Level) * (sp / 5) + caster.Parameters.Str + 250;
			target.TakeDamage(damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 1, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MO_BODYRELOCATION)]
	public class BodyRelocationHandler : ISkillHandler
	{
		// eAthena MO_BODYRELOCATION (Snap): teleports the caster to
		// the target cell within range. v1: simple warp.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			caster.Warp(new Sabine.Shared.World.Location(caster.Map.Id, target.Position));
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MO_BLADESTOP)]
	public class BladeStopHandler : ISkillHandler
	{
		// eAthena MO_BLADESTOP: catches an enemy melee attack mid-strike,
		// freezing both combatants. v1 stub.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.ServerMessage("Blade Stop trigger pipeline is not yet wired.");
			return Task.CompletedTask;
		}
	}
}
