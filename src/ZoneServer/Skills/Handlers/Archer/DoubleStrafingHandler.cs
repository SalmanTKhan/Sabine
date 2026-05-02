using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_DOUBLE)]
	public class DoubleStrafingHandler : ITargetedSkillHandler
	{
		// TODO: re-introduce ~100ms inter-hit delay via a scheduler now
		// that Handle is sync void.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			var skillRatio = 0.9f + 0.1f * level;

			for (var i = 0; i < 2; i++)
			{
				var ctx = new AttackContext(caster, target)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Physical,
					SkillRatio = skillRatio,
				};
				var result = BattleCalculator.Calc(ctx);
				var damage = result.IsMiss ? 0 : result.Damage;

				if (!result.IsMiss)
					target.TakeDamage(damage, caster);

				Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 0, result.ActionType);
			}
		}
	}
}
