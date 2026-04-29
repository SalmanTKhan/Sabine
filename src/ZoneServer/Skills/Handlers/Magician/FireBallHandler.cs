using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_FIREBALL)]
	public class FireBallHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return Task.CompletedTask;

			var skillRatio = 0.8f + 0.1f * skill.Level;

			Send.ZC_NOTIFY_SKILL_POSITION(caster, target.Handle, skill.Id, skill.Level, target.Position, 0, 0, 0, ActionType.Skill);

			var targets = caster.Map.GetCharactersInRange(target.Position, 3)
				.Where(c => c.IsHostileTo(caster));

			foreach (var aoeTarget in targets)
			{
				var ctx = new AttackContext(caster, aoeTarget)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Magic,
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
