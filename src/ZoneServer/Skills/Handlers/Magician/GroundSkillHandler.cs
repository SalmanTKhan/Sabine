using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Magician
{
    public abstract class GroundSkillHandler : ISkillHandler
    {
        public Task HandleAsync(Character caster, IEntity target, Skill skill)
        {
            // Ground skills use the target's position, not the target itself.
            if (target == null) return Task.CompletedTask;

            var position = target.Position;

            // TODO: Create a skill unit object on the map at the target position.
            // This unit would have its own update logic (e.g., damaging enemies who walk into a firewall).
            // For now, we just send the packet to create the visual effect.
            Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, position.X, position.Y, 0);

            if (caster is PlayerCharacter pc)
            {
                pc.ServerMessage($"{skill.Id} is not fully implemented yet.");
            }

            return Task.CompletedTask;
        }
    }

	[SkillHandler(SkillId.MG_FIREWALL)]
	public class FireWallHandler : GroundSkillHandler { }

    [SkillHandler(SkillId.MG_SAFETYWALL)]
	public class SafetyWallHandler : GroundSkillHandler { }

	[SkillHandler(SkillId.MG_THUNDERSTORM)]
	public class ThunderStormHandler : GroundSkillHandler { }
}