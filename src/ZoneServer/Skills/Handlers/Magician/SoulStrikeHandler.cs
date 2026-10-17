using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_SOULSTRIKE)]
	public class SoulStrikeHandler : ITargetedSkillHandler
	{
		// Multi-hit Ghost magic. Hits fire ~100ms apart for the
		// staggered visual; the first hit lands synchronously and
		// the rest are scheduled fire-and-forget.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			var hitCount = (level + 1) / 2;

			Send.ZC_USE_SKILL(caster as PlayerCharacter, skill.Id, level, target.Handle, true);

			FireOnce(caster, target, skill, level);
			if (hitCount > 1)
				_ = FireRemainingAsync(caster, target, skill, level, hitCount - 1);
		}

		private static async Task FireRemainingAsync(Character caster, Character target, Skill skill, int level, int remaining)
		{
			for (var i = 0; i < remaining; i++)
			{
				await Task.Delay(100);
				if (target == null || target.IsDead) return;
				if (caster == null || caster.IsDead) return;
				FireOnce(caster, target, skill, level);
			}
		}

		private static void FireOnce(Character caster, Character target, Skill skill, int level)
		{
			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f,
				AttackElement = ElementType.Ghost,
			};
			var result = BattleCalculator.Calc(ctx);
			var damage = result.IsMiss ? 0 : result.Damage;

			if (damage > 0)
				target.TakeDamage(damage, caster);

			// Sent per hit, including misses: this is the only packet that tells the
			// client a hit landed, so without it there are no damage numbers and no
			// impact effect. The 100ms spacing between hits carries the stagger.
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 0, result.ActionType);
		}
	}
}
