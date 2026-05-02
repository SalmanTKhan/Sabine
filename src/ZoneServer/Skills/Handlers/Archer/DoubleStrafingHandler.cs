using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_DOUBLE)]
	public class DoubleStrafingHandler : ITargetedSkillHandler
	{
		// Two-hit ranged physical. The second arrow follows ~100ms
		// after the first to give the visible double-shot pulse;
		// scheduled fire-and-forget since Handle is sync void.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			var skillRatio = 0.9f + 0.1f * level;

			FireArrow(caster, target, skill, level, skillRatio);
			_ = FireDelayedAsync(caster, target, skill, level, skillRatio);
		}

		private static async Task FireDelayedAsync(Character caster, Character target, Skill skill, int level, float skillRatio)
		{
			await Task.Delay(100);
			if (target == null || target.IsDead) return;
			if (caster == null || caster.IsDead) return;
			FireArrow(caster, target, skill, level, skillRatio);
		}

		private static void FireArrow(Character caster, Character target, Skill skill, int level, float skillRatio)
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
