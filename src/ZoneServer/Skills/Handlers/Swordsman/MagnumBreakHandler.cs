using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_MAGNUM)]
	public class MagnumBreakHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			var hpCost = (int)(caster.Parameters.HpMax * 0.05f);
			if (caster.Parameters.Hp <= hpCost)
				return Task.CompletedTask;

			caster.Parameters.Modify(ParameterType.Hp, -hpCost);

			var skillRatio = 1.0f + 0.2f * skill.Level;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			var targets = caster.Map.GetCharactersInRange(caster.Position, 3)
				.Where(c => c != caster && c.IsHostileTo(caster));

			foreach (var aoeTarget in targets)
			{
				var ctx = new AttackContext(caster, aoeTarget)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Physical,
					SkillRatio = skillRatio,
					AttackElement = ElementType.Fire,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					aoeTarget.TakeDamage(result.Damage, caster);
			}

			return Task.CompletedTask;
		}
	}
}
