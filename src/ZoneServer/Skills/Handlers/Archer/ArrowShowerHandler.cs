using System.Linq;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_SHOWER)]
	public class ArrowShowerHandler : ITargetedSkillHandler
	{
		// Sabine skill data declares AC_SHOWER target=Enemy (not Ground),
		// so this is an enemy-targeted AOE centered on the target.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			var skillRatio = 0.7f + 0.05f * level;

			Send.ZC_NOTIFY_SKILL_POSITION(caster, target.Handle, skill.Id, level, target.Position, 0, 0, 0, ActionType.Skill);

			var targets = caster.Map.GetCharactersInRange(target.Position, 3)
				.Where(c => c.IsHostileTo(caster));

			foreach (var aoeTarget in targets)
			{
				var ctx = new AttackContext(caster, aoeTarget)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Physical,
					SkillRatio = skillRatio,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					aoeTarget.TakeDamage(result.Damage, caster);
			}
		}
	}
}
