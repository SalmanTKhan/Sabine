using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_NAPALMBEAT)]
	public class NapalmBeatHandler : ITargetedSkillHandler
	{
		// TODO: eAthena MG_NAPALMBEAT splits damage across up to 3 enemies
		// in a 1-cell radius around the primary target — currently single-
		// target. See ref/eAthena/src/map/skill.c (search MG_NAPALMBEAT).
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Magic,
				SkillRatio = 0.6f + 0.1f * level,
				AttackElement = ElementType.Ghost,
			};
			var result = BattleCalculator.Calc(ctx);
			var damage = result.IsMiss ? 0 : result.Damage;

			if (!result.IsMiss)
				target.TakeDamage(damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 0, result.ActionType);
		}
	}
}
