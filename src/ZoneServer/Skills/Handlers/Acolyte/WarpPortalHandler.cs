using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_WARP)]
	public class WarpPortalHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			// TODO: Check for and consume a Blue Gemstone.
			// TODO: Check if the caster has a warp point memo for the target map.
			// TODO: Create a Warp Portal skill unit on the ground.
			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Warp Portal is not fully implemented yet.");
			}

			return Task.CompletedTask;
		}
	}
}