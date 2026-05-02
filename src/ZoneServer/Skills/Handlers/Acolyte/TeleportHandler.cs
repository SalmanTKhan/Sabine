using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_TELEPORT)]
	public class TeleportHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not PlayerCharacter pc)
				return;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (level == 1)
			{
				// Level 1 teleports to a random walkable cell on the current map.
				var randomPos = caster.Map.GetRandomWalkablePosition();
				caster.Warp(new Location(caster.Map.Id, randomPos));
			}
			else
			{
				// Level 2 teleports to the character's save point.
				caster.Warp(pc.SaveLocation);
			}
		}
	}
}
