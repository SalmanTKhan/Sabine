using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Knight
{
	[SkillHandler(SkillId.KN_SPEARBOOMERANG)]
	public class SpearBoomerangHandler : ISkillHandler
	{
		// eAthena KN_SPEARBOOMERANG: 100% + 50%*lv ranged physical.
		// Treated as long-range for card interactions even when the
		// caster wields a melee spear. Renewal unchanged.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.50f * skill.Level,
				IsLongRange = true,
				WeaponRequired = true,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, result.HitCount, result.ActionType);
			return Task.CompletedTask;
		}
	}
}
