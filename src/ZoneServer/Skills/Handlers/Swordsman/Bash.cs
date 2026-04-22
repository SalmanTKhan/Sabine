using System.Numerics;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_BASH)]
	public class Bash : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				// Bash must have a character target.
				return Task.CompletedTask;
			}

			// Base ATK is 100%. Bash adds damage.
			// Let's assume a simple formula: ATK * (1 + (0.3 * skill.Level))
			// This means at level 10, it's ATK * (1 + 3) = 400% damage.
			var damageMultiplier = 1.0f + 0.3f * skill.Level;
			var damage = (int)(caster.Parameters.Attack * damageMultiplier);

			// Apply damage to the target.
			// The TakeDamage method would handle defense, miss chance, etc.
			targetCharacter.TakeDamage(damage, caster);

			// TODO: Add skill animation packet send
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, ActionType.Skill);

			return Task.CompletedTask;
		}
	}
}
