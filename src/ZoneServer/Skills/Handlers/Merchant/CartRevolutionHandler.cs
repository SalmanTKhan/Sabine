using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_CARTREVOLUTION)]
	public class CartRevolutionHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return Task.CompletedTask;

			if (caster is not PlayerCharacter pc)
				return Task.CompletedTask;

			// Requirement: Must have a cart
			if (pc.Parameters.Cart == 0)
				return Task.CompletedTask;

			// Damage Formula: (150% + (CartWeight / 8000) * 100%) ATK
			// Note: Cart Weight in params is usually divided by 10 for display, ensure unit consistency.
			// Assuming pc.Inventory.GetWeight() returns total weight.
			// We need Cart Weight specifically. For now, using a placeholder calculation.

			// Placeholder cart weight (max 80000 in legacy units)
			var cartWeight = 8000;

			var weightBonus = (float)cartWeight / 8000f;
			var damageMultiplier = 1.5f + weightBonus;
			var damage = (int)(caster.Parameters.Attack * damageMultiplier);

			// Visual Effect on Caster
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			// AoE Logic: 3x3 area around the TARGET
			var targets = caster.Map.GetCharactersInRange(target.Position, 1) // Range 1 = 3x3
				.Where(c => c != caster && c.IsHostileTo(caster));

			foreach (var aoeTarget in targets)
			{
				// Deal Damage
				aoeTarget.TakeDamage(damage, caster);

				// Knockback 2 cells
				aoeTarget.Controller.Knockback(caster.Position, 2);
			}

			return Task.CompletedTask;
		}
	}
}
