using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Knight
{
	[SkillHandler(SkillId.KN_SPEARSTAB)]
	public class SpearStabHandler : ITargetedSkillHandler
	{
		// eAthena KN_SPEARSTAB: 100% + 15%*lv physical with knockback
		// of 6 cells.
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
				SkillRatio = 1.0f + 0.15f * level,
				WeaponRequired = true,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				target.TakeDamage(result.Damage, caster);
				target.Controller?.Knockback(caster.Position, 6);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, result.HitCount, result.ActionType);
		}
	}
}
