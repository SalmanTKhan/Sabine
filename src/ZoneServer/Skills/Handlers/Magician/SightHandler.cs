using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_SIGHT)]
	public class SightHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// Sight is a ground-based AoE that reveals hidden enemies.
			// The skill is cast on the caster themselves.
			var radius = 3 + (skill.Level / 2); // Example radius

			// TODO: Implement "Hiding" status and reveal logic.
			// var hiddenEntities = caster.Map.GetEntitiesInRange(caster.Position, radius)
			//     .Where(e => e.Status.Has(StatusType.Hiding));
			// foreach(var entity in hiddenEntities) { entity.Status.Remove(StatusType.Hiding); }

			// Show a visual effect on the ground around the caster.
			Send.ZC_SKILL_ENTRY(caster, skill.Id, caster.Position);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Sight is not fully implemented yet.");
			}

			return Task.CompletedTask;
		}
	}
}