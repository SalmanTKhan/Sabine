using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_CRUCIS)]
	public class SignumCrucisHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// This skill debuffs all Undead property monsters on screen.
			var radius = 10; // "On screen" radius
			var targets = caster.Map.GetCharactersInRange(caster.Position, radius)
				.Where(c => c.IsHostileTo(caster)); // TODO: Add check for Undead property

			// TODO: Implement status effect to reduce DEF.
			var defReduction = 10 + (4 * skill.Level); // 14% to 50%
			foreach (var tgt in targets)
			{
				tgt.Parameters.Defense -= defReduction;
				// Apply DEF reduction effect to tgt
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Signum Crucis is not fully implemented yet.");
			}

			return Task.CompletedTask;
		}
	}
}
