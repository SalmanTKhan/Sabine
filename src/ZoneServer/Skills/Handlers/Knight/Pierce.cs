using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Knight
{
	[SkillHandler(SkillId.KN_PIERCE)]
	public class PierceHandler : ITargetedSkillHandler
	{
		// eAthena KN_PIERCE: 100% + 10%/lv per hit. Hit count scales
		// with target size — 1 hit Small, 2 hits Medium, 3 hits Large.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var hits = ResolveHitCount(target);
			var ratioPerHit = 1.0f + 0.1f * level;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = ratioPerHit,
				HitCount = hits,
				WeaponRequired = true,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, result.HitCount, result.ActionType);
		}

		private static int ResolveHitCount(Character target)
		{
			if (target is not Monster monster) return 2;
			return monster.Data.Size switch
			{
				SizeType.Small => 1,
				SizeType.Medium => 2,
				SizeType.Large => 3,
				_ => 2,
			};
		}
	}
}
