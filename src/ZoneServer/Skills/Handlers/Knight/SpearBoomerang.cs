using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Knight
{
	[SkillHandler(SkillId.KN_SPEARBOOMERANG)]
	public class SpearBoomerangHandler : ITargetedSkillHandler
	{
		// eAthena KN_SPEARBOOMERANG: 100% + 50%*lv ranged physical.
		// Treated as long-range for card interactions even when the
		// caster wields a melee spear.
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
				SkillRatio = 1.0f + 0.50f * level,
				IsLongRange = true,
				WeaponRequired = true,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, result.HitCount, result.ActionType);
		}
	}
}
