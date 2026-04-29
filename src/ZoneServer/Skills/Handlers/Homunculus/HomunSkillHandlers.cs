using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.HomunculusSkills
{
	[SkillHandler(SkillId.HLIF_HEAL)]
	public class HealingTouchHandler : ISkillHandler
	{
		// eAthena HLIF_HEAL: Lif heals its owner. Heal amount scales
		// with INT and skill level, mirrors AL_HEAL's shape.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not Homunculus homun) return Task.CompletedTask;
			var owner = homun.Owner;
			if (owner == null || owner.IsDead) return Task.CompletedTask;

			var amount = (homun.Parameters.BaseLevel + homun.Parameters.Int) / 5 * (4 + 8 * skill.Level);
			owner.HealHp(amount);
			Send.ZC_RECOVERY(owner, ParameterType.Hp, amount);
			Send.ZC_NOTIFY_SKILL(homun, owner.Handle, skill.Id, skill.Level, amount, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HAMI_CASTLE)]
	public class CastlingHandler : ISkillHandler
	{
		// eAthena HAMI_CASTLE (Castling): Amistr swaps positions with
		// its owner. Used to rescue the owner out of a melee.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not Homunculus homun) return Task.CompletedTask;
			var owner = homun.Owner;
			if (owner == null) return Task.CompletedTask;

			var ownerPos = owner.Position;
			var homunPos = homun.Position;
			owner.Warp(new Sabine.Shared.World.Location(owner.Map.Id, homunPos));
			homun.Warp(homun.Map.Id, ownerPos);

			Send.ZC_NOTIFY_SKILL(homun, owner.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HVAN_CAPRICE)]
	public class CapriceHandler : ISkillHandler
	{
		// eAthena HVAN_CAPRICE: Vanilmirth picks one of the four
		// elemental bolts at random and casts it on the target.
		// Damage = 100% MATK, hit count = skill level.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null || target == caster) return Task.CompletedTask;

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
				SkillLevel = skill.Level,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f,
				HitCount = Math.Max(1, skill.Level),
				AttackElement = pick.elem,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, pick.id, skill.Level, result.Damage, 0, result.HitCount, result.ActionType);
			return Task.CompletedTask;
		}
	}
}
