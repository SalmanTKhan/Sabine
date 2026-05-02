using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Wizard
{
	[SkillHandler(SkillId.WZ_EARTHSPIKE)]
	public class EarthSpikeHandler : ITargetedSkillHandler
	{
		// eAthena WZ_EARTHSPIKE: 1 hit Earth per level, 100% MATK each.
		// Renewal: 100%*lv per single hit.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var renewal = BattleCalculator.IsRenewal();
			var hits = renewal ? 1 : level;
			var ratio = renewal ? 1.0f * level : 1.0f;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Magic,
				SkillRatio = ratio,
				HitCount = hits,
				AttackElement = ElementType.Earth,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, hits, result.ActionType);
		}
	}

	[SkillHandler(SkillId.WZ_JUPITEL)]
	public class JupitelThunderHandler : ITargetedSkillHandler
	{
		// eAthena WZ_JUPITEL: ranged Wind magic, 1 hit per level at
		// 1.0 ratio. Knockback 2 cells per hit.
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
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f,
				HitCount = level,
				AttackElement = ElementType.Wind,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				target.TakeDamage(result.Damage, caster);
				target.Controller?.Knockback(caster.Position, 2 * level);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, result.HitCount, result.ActionType);
		}
	}

	[SkillHandler(SkillId.WZ_WATERBALL)]
	public class WaterBallHandler : ITargetedSkillHandler
	{
		// eAthena WZ_WATERBALL: requires water cells nearby; each ball
		// is a Water magic hit at 100% MATK. v1 fires one ball per
		// level without the cell consumption (water-tile gating is a
		// follow-up).
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var balls = level;
			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f,
				HitCount = balls,
				AttackElement = ElementType.Water,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, balls, result.ActionType);
		}
	}
}
