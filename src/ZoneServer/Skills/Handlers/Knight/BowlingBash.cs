using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Knight
{
	[SkillHandler(SkillId.KN_BOWLINGBASH)]
	public class BowlingBashHandler : ISkillHandler
	{
		// rAthena pre-renewal: 100% + 40%*lv per hit, single hit on
		// the primary plus a splash hit on neighbors (the "gutter
		// strike" mechanic emulated here as a 2-cell radius splash).
		// rAthena renewal: 200% + 100%*lv per hit, twice on the
		// primary target (no gutter splash; affects line cells with
		// knockback).
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var renewal = BattleCalculator.IsRenewal();
			var ratio = renewal
				? 2.0f + 1.0f * skill.Level
				: 1.0f + 0.40f * skill.Level;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				HitCount = renewal ? 2 : 1,
				WeaponRequired = true,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				target.TakeDamage(result.Damage, caster);
				target.Controller?.Knockback(caster.Position, 1);
			}

			// Pre-renewal splash: 2-cell radius at half ratio.
			if (!renewal)
			{
				foreach (var other in caster.Map.GetCharactersInRange(target.Position, 2))
				{
					if (other == target || other == caster) continue;
					if (!other.IsHostileTo(caster)) continue;

					var splash = new AttackContext(caster, other)
					{
						SkillId = skill.Id,
						SkillLevel = skill.Level,
						Kind = AttackKind.Physical,
						SkillRatio = ratio,
						WeaponRequired = true,
					};
					var splashResult = BattleCalculator.Calc(splash);
					if (!splashResult.IsMiss)
					{
						other.TakeDamage(splashResult.Damage, caster);
						other.Controller?.Knockback(caster.Position, 1);
					}
				}
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, result.HitCount, result.ActionType);
			return Task.CompletedTask;
		}
	}
}
