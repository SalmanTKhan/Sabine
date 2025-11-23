using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_FIREBALL)]
	public class FireBallHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return Task.CompletedTask;
			}

			// Damage formula: MATK * (0.8 + (0.1 * skill.Level)) - Example: 90% to 180%
			var damageMultiplier = 0.8f + (0.1f * skill.Level);
			var baseDamage = (int)(caster.Parameters.MagicAttack * damageMultiplier);

			// Show skill effect at target's location
			Send.ZC_NOTIFY_SKILL_POSITION(caster, target.Handle, skill.Id, skill.Level, target.Position, baseDamage, 0, 0, ActionType.Skill);

			// Find all characters in a 3x3 radius around the target
			var targets = caster.Map.GetCharactersInRange(target.Position, 3)
				.Where(c => c.IsHostileTo(caster));

			foreach (var aoeTarget in targets)
			{
				// TODO: Damage should have element (Fire) and be reduced by MDEF.
				var finalDamage = baseDamage;
				aoeTarget.TakeDamage(finalDamage, caster);
			}

			return Task.CompletedTask;
		}
	}
}