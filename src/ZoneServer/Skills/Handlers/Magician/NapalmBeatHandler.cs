using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_NAPALMBEAT)]
	public class NapalmBeatHandler : ISkillHandler
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
				SkillRatio = 0.6f + 0.1f * skill.Level,
				AttackElement = ElementType.Ghost,
			};
			var result = BattleCalculator.Calc(ctx);
			var damage = result.IsMiss ? 0 : result.Damage;

			if (!result.IsMiss)
				targetCharacter.TakeDamage(damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, result.ActionType);

			return Task.CompletedTask;
		}
	}
}
