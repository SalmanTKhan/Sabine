using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_FROSTDIVER)]
	public class FrostDiverHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return Task.CompletedTask;

			var ctx = new AttackContext(caster, targetCharacter)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f + 0.1f * skill.Level,
				AttackElement = ElementType.Water,
			};
			var result = BattleCalculator.Calc(ctx);
			var damage = result.IsMiss ? 0 : result.Damage;

			if (!result.IsMiss)
				targetCharacter.TakeDamage(damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, result.ActionType);

			// eAthena MG_FROSTDIVER: 30 + 3*level freeze chance, 3s + 2s/level duration.
			var rnd = RandomProvider.Get();
			var freezeChance = 30 + 3 * skill.Level;
			if (rnd.Next(100) < freezeChance)
			{
				var duration = TimeSpan.FromSeconds(3 + 2 * skill.Level);
				targetCharacter.StatusEffects.Start(StatusId.Freeze, skill.Level, duration, caster);
			}

			return Task.CompletedTask;
		}
	}
}
