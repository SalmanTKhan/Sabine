using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Champion
{
	/// <summary>
	/// CH_PALMSTRIKE (Raging Palm Strike) — single-hit physical with a
	/// big skill ratio and knockback. eAthena battle.c:1597 sets
	/// <c>skillratio += 100 + 100*level</c>, so the multiplier
	/// (<c>(100 + add) / 100</c>) becomes <c>2.0 + 1.0*level</c>.
	/// Knockback isn't part of the skill ratio in Sabine yet (no
	/// AttackContext knockback field), so the displacement is omitted —
	/// the doc-comment marks the spot.
	/// </summary>
	[SkillHandler(SkillId.CH_PALMSTRIKE)]
	public class PalmStrikeHandler : ITargetedSkillHandler
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
				SkillRatio = 2.0f + 1.0f * level,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	/// <summary>
	/// CH_TIGERFIST (Glacier Fist) — combo finisher with a chance to
	/// inflict Stun. eAthena battle.c:1591 sets
	/// <c>skillratio += 100*level - 60</c>, so the multiplier becomes
	/// <c>0.4 + 1.0*level</c>. The Stun roll lives on the on-hit
	/// pipeline; Sabine doesn't have a per-skill status-inflict
	/// dispatcher yet, so the Stun arm is deferred.
	/// </summary>
	[SkillHandler(SkillId.CH_TIGERFIST)]
	public class TigerFistHandler : ITargetedSkillHandler
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
				SkillRatio = 0.4f + 1.0f * level,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	/// <summary>
	/// CH_CHAINCRUSH (Chain Crush Combo) — late-combo big hit. eAthena
	/// battle.c:1594 sets <c>skillratio += 300 + 100*level</c>, so the
	/// multiplier becomes <c>4.0 + 1.0*level</c>. Combo state gating
	/// (must follow CH_TIGERFIST) lives on the client today; Sabine has
	/// the ChainCombo / ComboFinish status pair but no Champion-side
	/// combo extension, so the gate is left to client cooperation.
	/// </summary>
	[SkillHandler(SkillId.CH_CHAINCRUSH)]
	public class ChainCrushHandler : ITargetedSkillHandler
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
				SkillRatio = 4.0f + 1.0f * level,
				HitCount = 7,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, result.HitCount, result.ActionType);
		}
	}

	/// <summary>
	/// CH_SOULCOLLECT (Zen) — instantly fills the caster's Spirit Sphere
	/// count to the maximum allowed by their MO_CALLSPIRITS level.
	/// Sabine stores the ball count on <c>StatusId.Spirits</c> Val1 (see
	/// <see cref="Monk.CallSpiritsHandler"/>). Fills to 5 unconditionally
	/// here, matching the eAthena classic max; a per-MO_CALLSPIRITS gate
	/// can land later if alpha-era fairness is wanted. Duration: 600s
	/// (skill_cast_db.txt:673).
	/// </summary>
	[SkillHandler(SkillId.CH_SOULCOLLECT)]
	public class SoulCollectHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			caster.StatusEffects.Stop(StatusId.Spirits);
			caster.StatusEffects.Start(StatusId.Spirits, level, TimeSpan.FromMinutes(10), caster, 5);

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}
}
