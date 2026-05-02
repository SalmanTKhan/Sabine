using System.Linq;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_MAGNUM)]
	public class MagnumBreakHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var hpCost = (int)(caster.Parameters.HpMax * 0.05f);
			if (caster.Parameters.Hp <= hpCost)
				return;

			caster.Parameters.Modify(ParameterType.Hp, -hpCost);

			var skillRatio = 1.0f + 0.2f * level;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var targets = caster.Map.GetCharactersInRange(caster.Position, 3)
				.Where(c => c != caster && c.IsHostileTo(caster));

			foreach (var aoeTarget in targets)
			{
				var ctx = new AttackContext(caster, aoeTarget)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Physical,
					SkillRatio = skillRatio,
					AttackElement = ElementType.Fire,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					aoeTarget.TakeDamage(result.Damage, caster);
			}
		}
	}
}
