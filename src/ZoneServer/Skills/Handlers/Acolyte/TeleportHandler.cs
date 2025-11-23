using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_TELEPORT)]
	public class TeleportHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (caster is not PlayerCharacter pc)
			{
				return Task.CompletedTask;
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (skill.Level == 1)
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

			return Task.CompletedTask;
		}
	}
}
