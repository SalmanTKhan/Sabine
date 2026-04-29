using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
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

			if (pc.Parameters.Cart == 0)
				return Task.CompletedTask;

			// Cart Revolution damage: (150% + (CartWeight / 8000) * 100%) ATK.
			// Empty cart = 1.5x; full cart = 2.5x. Reads the live cart
			// weight from the player's inventory.
			var cartWeight = pc.Inventory.CartWeight;
			var weightBonus = cartWeight / 8000f;
			var skillRatio = 1.5f + weightBonus;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			var targets = caster.Map.GetCharactersInRange(target.Position, 1)
				.Where(c => c != caster && c.IsHostileTo(caster));

			foreach (var aoeTarget in targets)
			{
				var ctx = new AttackContext(caster, aoeTarget)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Physical,
					SkillRatio = skillRatio,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					aoeTarget.TakeDamage(result.Damage, caster);

				aoeTarget.Controller.Knockback(caster.Position, 2);
			}

			return Task.CompletedTask;
		}
	}
}
