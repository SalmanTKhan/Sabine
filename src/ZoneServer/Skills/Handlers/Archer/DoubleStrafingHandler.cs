using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_DOUBLE)]
	public class DoubleStrafingHandler : ISkillHandler
	{
		public async Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return;

			var skillRatio = 0.9f + 0.1f * skill.Level;

			for (var i = 0; i < 2; i++)
			{
				var ctx = new AttackContext(caster, targetCharacter)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Physical,
					SkillRatio = skillRatio,
				};
				var result = BattleCalculator.Calc(ctx);
				var damage = result.IsMiss ? 0 : result.Damage;

				if (!result.IsMiss)
					targetCharacter.TakeDamage(damage, caster);

				Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, result.ActionType);

				if (i == 0)
					await Task.Delay(100);
			}
		}
	}
}
