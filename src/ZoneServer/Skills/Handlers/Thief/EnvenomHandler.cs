using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_POISON)]
	public class EnvenomHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return Task.CompletedTask;

			var ctx = new AttackContext(caster, targetCharacter)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.15f * skill.Level,
				AttackElement = ElementType.Poison,
			};
			var result = BattleCalculator.Calc(ctx);
			var damage = result.IsMiss ? 0 : result.Damage;

			if (!result.IsMiss)
				targetCharacter.TakeDamage(damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, result.ActionType);

			// eAthena TF_POISON: 4*level poison chance, 30s + 30s/level duration.
			var rnd = RandomProvider.Get();
			var poisonChance = 4 * skill.Level;
			if (rnd.Next(100) < poisonChance)
			{
				var duration = TimeSpan.FromSeconds(30 + 30 * skill.Level);
				targetCharacter.StatusEffects.Start(StatusId.Poison, skill.Level, duration, caster);
			}

			return Task.CompletedTask;
		}
	}
}
