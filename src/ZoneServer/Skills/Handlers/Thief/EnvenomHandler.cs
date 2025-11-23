using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_POISON)]
	public class EnvenomHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return Task.CompletedTask;
			}

			// Envenom deals a small amount of damage and has a chance to poison.
			var damage = 15 + caster.Parameters.Attack; // Base poison damage

			// TODO: Add proper status effect system for Poison.
			// var poisonChance = 20 + (5 * skill.Level);
			// if (Random.Next(100) < poisonChance) { targetCharacter.Status.Apply(StatusType.Poison); }

			targetCharacter.TakeDamage(damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, ActionType.Attack);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Envenom's poison effect is not implemented yet.");
			}

			return Task.CompletedTask;
		}
	}
}