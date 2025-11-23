using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_MAMMONITE)]
	public class MammoniteHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return Task.CompletedTask;
			}

			var zenyCost = skill.Level * 100;
			if (caster.Parameters.Zeny < zenyCost)
			{
				// Not enough zeny
				if (caster is PlayerCharacter pc)
				{
					// TODO: Send a proper "not enough zeny" message/packet.
					pc.ServerMessage("Not enough Zeny to use Mammonite.");
				}
				return Task.CompletedTask;
			}

			caster.Parameters.Modify(ParameterType.Zeny, -zenyCost);

			// Damage formula: ATK * (1.0 + (0.5 * skill.Level))
			var damageMultiplier = 1.0f + (0.5f * skill.Level);
			var damage = (int)(caster.Parameters.Attack * damageMultiplier);

			targetCharacter.TakeDamage(damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, ActionType.Attack);

			return Task.CompletedTask;
		}
	}
}