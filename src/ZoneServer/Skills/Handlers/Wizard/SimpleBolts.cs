using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Wizard
{
	[SkillHandler(SkillId.WZ_EARTHSPIKE)]
	public class EarthSpikeHandler : ISkillHandler
	{
		// eAthena WZ_EARTHSPIKE: 1 hit Earth per level, 100% MATK each.
		// Renewal: 100%*lv per single hit.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var renewal = BattleCalculator.IsRenewal();
			var hits = renewal ? 1 : skill.Level;
			var ratio = renewal ? 1.0f * skill.Level : 1.0f;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Magic,
				SkillRatio = ratio,
				HitCount = hits,
				AttackElement = ElementType.Earth,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, hits, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_JUPITEL)]
	public class JupitelThunderHandler : ISkillHandler
	{
		// eAthena WZ_JUPITEL: ranged Wind magic, 1 hit per level at
		// 1.0 ratio. Knockback 2 cells per hit. Renewal unchanged.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f,
				HitCount = skill.Level,
				AttackElement = ElementType.Wind,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				target.TakeDamage(result.Damage, caster);
				target.Controller?.Knockback(caster.Position, 2 * skill.Level);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, result.HitCount, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_WATERBALL)]
	public class WaterBallHandler : ISkillHandler
	{
		// eAthena WZ_WATERBALL: requires water cells nearby; each ball
		// is a Water magic hit at 100% MATK. v1 fires one ball per
		// level without the cell consumption (water-tile gating is a
		// follow-up).
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var balls = skill.Level;
			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f,
				HitCount = balls,
				AttackElement = ElementType.Water,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, balls, result.ActionType);
			return Task.CompletedTask;
		}
	}
}
