using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_HOLYWATER)]
	public class AquaBenedictaHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// TODO: Check if caster is standing on a water cell.
			// TODO: Check for and consume 1 Empty Bottle, give 1 Holy Water.
			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Aqua Benedicta is not fully implemented yet.");
			}
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AL_CURE)]
	public class CureHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (target is not Character targetCharacter) return Task.CompletedTask;
			// TODO: Remove Silence, Chaos, Darkness status effects from target.
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Cure is not fully implemented yet.");
			}
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AL_RUWACH)]
	public class RuwachHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// TODO: Reveal hidden enemies and deal small Holy damage.
			Send.ZC_SKILL_ENTRY(caster, skill.Id, caster.Position);
			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Ruwach is not fully implemented yet.");
			}
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AL_PNEUMA)]
	public class PneumaHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			// TODO: Create a skill unit that blocks ranged attacks.
			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Pneuma is not fully implemented yet.");
			}
			return Task.CompletedTask;
		}
	}
}