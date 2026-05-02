using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Monk
{
	[SkillHandler(SkillId.MO_TRIPLEATTACK)]
	public class TripleAttackHandler : ITargetedSkillHandler
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
				HitCount = 3,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			caster.StatusEffects.Start(StatusId.ChainCombo, level, TimeSpan.FromSeconds(2), caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 3, result.ActionType);
		}
	}

	[SkillHandler(SkillId.MO_CHAINCOMBO)]
	public class ChainComboHandler : ITargetedSkillHandler
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
				SkillRatio = 1.5f + 0.50f * level,
				HitCount = 4,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			caster.StatusEffects.Stop(StatusId.ChainCombo);
			caster.StatusEffects.Start(StatusId.ComboFinish, level, TimeSpan.FromSeconds(2), caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 4, result.ActionType);
		}
	}

	[SkillHandler(SkillId.MO_COMBOFINISH)]
	public class ComboFinishHandler : ITargetedSkillHandler
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
				SkillRatio = 2.0f + 0.50f * level + caster.Parameters.Str / 100.0f,
				HitCount = 5,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			caster.StatusEffects.Stop(StatusId.ComboFinish);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 5, result.ActionType);
		}
	}

	[SkillHandler(SkillId.MO_INVESTIGATE)]
	public class InvestigateHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var defFactor = Math.Max(1, target.Parameters.MeleeDefense + target.Parameters.MeleeDefenseBonus);
			var ratio = (1.0f + 0.50f * level) * (defFactor / 50.0f);

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				IgnoreDefense = true,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	[SkillHandler(SkillId.MO_FINGEROFFENSIVE)]
	public class FingerOffensiveHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var balls = caster.StatusEffects.TryGet(StatusId.Spirits, out var spirits) ? spirits.Val1 : 1;
			if (balls < 1) balls = 1;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.50f * level,
				HitCount = balls,
				IsLongRange = true,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			caster.StatusEffects.Stop(StatusId.Spirits);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, balls, result.ActionType);
		}
	}

	[SkillHandler(SkillId.MO_EXTREMITYFIST)]
	public class AsuraStrikeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var sp = caster.Parameters.Sp;
			var balls = caster.StatusEffects.TryGet(StatusId.Spirits, out var spirits) ? spirits.Val1 : 0;
			if (balls < 4)
			{
				if (caster is PlayerCharacter pc)
					pc.ServerMessage("Asura Strike requires at least 4 spirit balls.");
				return;
			}

			caster.Parameters.Modify(ParameterType.Sp, -sp);
			caster.StatusEffects.Stop(StatusId.Spirits);

			var damage = (8 + level) * (sp / 5) + caster.Parameters.Str + 250;
			target.TakeDamage(damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 1, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.MO_BODYRELOCATION)]
	public class BodyRelocationHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			caster.Warp(new Sabine.Shared.World.Location(caster.Map.Id, pos));
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.MO_BLADESTOP)]
	public class BladeStopHandler : ITargetedSkillHandler
	{
		// eAthena MO_BLADESTOP: opens a brief catch window. The next
		// incoming melee physical hit is intercepted by
		// BladeStopListener (Battle/Listeners/BladeStopListener.cs):
		// the hit is cancelled, both attacker and caster are stunned
		// for a couple of seconds. Window length scales with level.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			// 2s + 1s/level catch window; consumed by the listener on
			// first incoming melee hit.
			var window = System.TimeSpan.FromSeconds(2 + level);
			caster.StatusEffects.Stop(StatusId.BladeStop);
			caster.StatusEffects.Start(StatusId.BladeStop, level, window, caster);
		}
	}
}
