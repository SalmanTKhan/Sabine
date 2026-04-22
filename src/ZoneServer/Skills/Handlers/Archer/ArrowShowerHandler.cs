using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_SHOWER)]
	public class ArrowShowerHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return Task.CompletedTask;
			}

			// TODO: Check if caster has arrows equipped.

			// Damage formula: ATK * (0.7 + (0.05 * skill.Level)) - Example: 75% to 120%
			var damageMultiplier = 0.7f + (0.05f * skill.Level);
			var baseDamage = (int)(caster.Parameters.Attack * damageMultiplier);

			Send.ZC_NOTIFY_SKILL_POSITION(caster, target.Handle, skill.Id, skill.Level, target.Position, baseDamage, 0, 0, ActionType.Skill);
			
			// Find all characters in a 3x3 radius around the target
			var targets = caster.Map.GetCharactersInRange(target.Position, 3)
				.Where(c => c.IsHostileTo(caster));

			foreach (var aoeTarget in targets)
			{
				aoeTarget.TakeDamage(baseDamage, caster);
			}

			return Task.CompletedTask;
		}
	}
}