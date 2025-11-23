using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_DOUBLE)]
	public class DoubleStrafingHandler : ISkillHandler
	{
		public async Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return;
			}

			// TODO: Check if caster has arrows equipped.

			// Damage formula: ATK * (0.9 + (0.1 * skill.Level)) per hit. Total 2 hits.
			var damageMultiplier = 0.9f + (0.1f * skill.Level);
			var damagePerHit = (int)(caster.Parameters.Attack * damageMultiplier);

			// First hit
			targetCharacter.TakeDamage(damagePerHit, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damagePerHit, 0, 0, ActionType.Attack);
			
			// Small delay between hits
			await Task.Delay(100);

			// Second hit
			targetCharacter.TakeDamage(damagePerHit, caster);
			// Reuse the skill notification packet, possibly with a different damage value if crits etc. are handled.
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damagePerHit, 0, 0, ActionType.Attack);
		}
	}
}