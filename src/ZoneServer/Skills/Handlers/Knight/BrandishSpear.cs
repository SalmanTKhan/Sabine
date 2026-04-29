using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Knight
{
	[SkillHandler(SkillId.KN_BRANDISHSPEAR)]
	public class BrandishSpearHandler : ISkillHandler
	{
		// rAthena pre-renewal: 100% + 20%*lv per hit; with bonus hits
		// at lv >3 (+ratio/2), >6 (+ratio/4), >9 (+ratio/8).
		// rAthena renewal:  300% + 100%*lv + 3*STR (single hit).
		// Mount-only (Riding); v1 doesn't enforce that yet.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			float ratio;
			if (BattleCalculator.IsRenewal())
			{
				ratio = 3.0f + 1.0f * skill.Level + 0.03f * caster.Parameters.Str;
			}
			else
			{
				var baseRatio = 1.0f + 0.20f * skill.Level;
				ratio = baseRatio;
				if (skill.Level > 3) ratio += baseRatio / 2;
				if (skill.Level > 6) ratio += baseRatio / 4;
				if (skill.Level > 9) ratio += baseRatio / 8;
			}

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				WeaponRequired = true,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			// AoE: 4 cells in front of the caster also take the hit at
			// 50% damage. Simple radius-2 splash around the primary
			// target as an approximation.
			foreach (var other in caster.Map.GetCharactersInRange(target.Position, 2))
			{
				if (other == target || other == caster) continue;
				if (!other.IsHostileTo(caster)) continue;

				var splashCtx = new AttackContext(caster, other)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Physical,
					SkillRatio = ratio * 0.5f,
					WeaponRequired = true,
				};
				var splashResult = BattleCalculator.Calc(splashCtx);
				if (!splashResult.IsMiss)
					other.TakeDamage(splashResult.Damage, caster);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, result.HitCount, result.ActionType);
			return Task.CompletedTask;
		}
	}
}
