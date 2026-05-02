using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_POISON)]
	public class EnvenomHandler : ITargetedSkillHandler
	{
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
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.15f * level,
				AttackElement = ElementType.Poison,
			};
			var result = BattleCalculator.Calc(ctx);
			var damage = result.IsMiss ? 0 : result.Damage;

			if (!result.IsMiss)
				target.TakeDamage(damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 0, result.ActionType);

			// eAthena TF_POISON: 4*level poison chance, 30s + 30s/level duration.
			var rnd = RandomProvider.Get();
			var poisonChance = 4 * level;
			if (rnd.Next(100) < poisonChance)
			{
				var duration = TimeSpan.FromSeconds(30 + 30 * level);
				target.StatusEffects.Start(StatusId.Poison, level, duration, caster);
			}
		}
	}
}
