using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.HomunculusSkills
{
	[SkillHandler(SkillId.HLIF_HEAL)]
	public class HealingTouchHandler : ITargetedSkillHandler
	{
		// eAthena HLIF_HEAL: Lif heals its owner. Heal amount scales
		// with INT and skill level, mirrors AL_HEAL's shape.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not Homunculus homun) return;
			var owner = homun.Owner;
			if (owner == null || owner.IsDead) return;

			var amount = (homun.Parameters.BaseLevel + homun.Parameters.Int) / 5 * (4 + 8 * level);
			owner.HealHp(amount);
			Send.ZC_RECOVERY(owner, ParameterType.Hp, amount);
			Send.ZC_NOTIFY_SKILL(homun, owner.Handle, skill.Id, level, amount, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.HAMI_CASTLE)]
	public class CastlingHandler : ITargetedSkillHandler
	{
		// eAthena HAMI_CASTLE (Castling): Amistr swaps positions with
		// its owner.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not Homunculus homun) return;
			var owner = homun.Owner;
			if (owner == null) return;

			var ownerPos = owner.Position;
			var homunPos = homun.Position;
			owner.Warp(new Sabine.Shared.World.Location(owner.Map.Id, homunPos));
			homun.Warp(homun.Map.Id, ownerPos);

			Send.ZC_NOTIFY_SKILL(homun, owner.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.HVAN_CAPRICE)]
	public class CapriceHandler : ITargetedSkillHandler
	{
		// eAthena HVAN_CAPRICE: Vanilmirth picks one of the four
		// elemental bolts at random and casts it on the target.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null || target == caster) return;

			var rolls = new (SkillId id, ElementType elem)[]
			{
				(SkillId.MG_FIREBOLT, ElementType.Fire),
				(SkillId.MG_COLDBOLT, ElementType.Water),
				(SkillId.MG_LIGHTNINGBOLT, ElementType.Wind),
				(SkillId.WZ_EARTHSPIKE, ElementType.Earth),
			};
			var pick = rolls[RandomProvider.Get().Next(rolls.Length)];

			var ctx = new AttackContext(caster, target)
			{
				SkillId = pick.id,
				SkillLevel = level,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f,
				HitCount = Math.Max(1, level),
				AttackElement = pick.elem,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, pick.id, level, result.Damage, 0, result.HitCount, result.ActionType);
		}
	}
}
