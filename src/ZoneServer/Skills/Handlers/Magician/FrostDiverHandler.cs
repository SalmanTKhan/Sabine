using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_FROSTDIVER)]
	public class FrostDiverHandler : ITargetedSkillHandler
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
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f + 0.1f * level,
				AttackElement = ElementType.Water,
			};
			var result = BattleCalculator.Calc(ctx);
			var damage = result.IsMiss ? 0 : result.Damage;

			if (!result.IsMiss)
				target.TakeDamage(damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 0, result.ActionType);

			// eAthena MG_FROSTDIVER: 30 + 3*level freeze chance, 3s + 2s/level duration.
			var rnd = RandomProvider.Get();
			var freezeChance = 30 + 3 * level;
			if (rnd.Next(100) < freezeChance)
			{
				var duration = TimeSpan.FromSeconds(3 + 2 * level);
				target.StatusEffects.Start(StatusId.Freeze, level, duration, caster);
			}
		}
	}
}
