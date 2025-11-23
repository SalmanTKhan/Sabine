using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
    // A base class for simple buff skills
    public abstract class BuffSkillHandler : ISkillHandler
    {
        public Task HandleAsync(Character caster, IEntity target, Skill skill)
        {
            if (target is not Character targetCharacter)
            {
                return Task.CompletedTask;
            }

            // TODO: Implement a proper status effect system.
            // For now, we just show the animation and a message.

            Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

            if (caster is PlayerCharacter pc)
            {
                pc.ServerMessage($"{skill.Id} is not fully implemented yet.");
            }

            return Task.CompletedTask;
        }
    }

	[SkillHandler(SkillId.AL_ANGELUS)]
	public class AngelusHandler : BuffSkillHandler { }

	[SkillHandler(SkillId.AL_BLESSING)]
	public class BlessingHandler : BuffSkillHandler { }

	[SkillHandler(SkillId.AL_INCAGI)]
	public class IncreaseAgiHandler : BuffSkillHandler { }

	[SkillHandler(SkillId.AL_DECAGI)]
	public class DecreaseAgiHandler : BuffSkillHandler { }
}