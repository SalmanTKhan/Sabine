using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_FROSTDIVER)]
	public class FrostDiverHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return Task.CompletedTask;
			}

			// Damage formula: MATK * (1.0 + (0.1 * skill.Level))
			var damageMultiplier = 1.0f + (0.1f * skill.Level);
			var damage = (int)(caster.Parameters.MagicAttack * damageMultiplier);

			// TODO: Add proper status effect system for Freeze.
			// This has a chance to apply the 'Frozen' status.
			// var freezeChance = 30 + (3 * skill.Level); // 33% to 60% chance
			// if (Random.Next(100) < freezeChance) { targetCharacter.Status.Apply(StatusType.Frozen); }

			targetCharacter.TakeDamage(damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Frost Diver's freeze effect is not implemented yet.");
			}

			return Task.CompletedTask;
		}
	}
}