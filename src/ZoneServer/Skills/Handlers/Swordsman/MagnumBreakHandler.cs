using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_MAGNUM)]
	public class MagnumBreakHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// Magnum Break is an AoE skill centered on the caster. The target parameter is ignored.
			// It costs HP to use.
			var hpCost = (int)(caster.Parameters.HpMax * 0.05f); // Example: 5% of Max HP
			if (caster.Parameters.Hp <= hpCost)
			{
				// Not enough HP
				return Task.CompletedTask;
			}
			caster.Parameters.Modify(ParameterType.Hp, -hpCost);

			// Damage formula: ATK * (1 + (0.2 * skill.Level)) - Example: 120% to 300%
			var damageMultiplier = 1.0f + (0.2f * skill.Level);
			var baseDamage = (int)(caster.Parameters.Attack * damageMultiplier);

			// TODO: Add skill animation for the caster
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			// Find all characters in a 3x3 radius around the caster
			var targets = caster.Map.GetCharactersInRange(caster.Position, 3)
				.Where(c => c != caster && c.IsHostileTo(caster));

			foreach (var aoeTarget in targets)
			{
				// TODO: Damage should have element (Fire) and be reduced by defense
				var finalDamage = baseDamage;
				aoeTarget.TakeDamage(finalDamage, caster);
			}

			return Task.CompletedTask;
		}
	}
}