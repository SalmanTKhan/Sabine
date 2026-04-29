using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.Hunter
{
	// Trap-deployment handlers all share the same shape: spawn a
	// TrapUnit subclass on the target cell and play the animation.
	// Trap-item consumption (item id 1065) is a follow-up; v1
	// allows free placement.

	internal static class TrapPlacement
	{
		public static Task Spawn(Character caster, Character target, TrapUnit unit, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			caster.Map.AddSkillUnit(unit);
			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HT_SKIDTRAP)]
	public class SkidTrapHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> TrapPlacement.Spawn(caster, target, new SkidTrapUnit(caster, target.Position, skill.Level), skill);
	}

	[SkillHandler(SkillId.HT_LANDMINE)]
	public class LandMineHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> TrapPlacement.Spawn(caster, target, new LandMineUnit(caster, target.Position, skill.Level), skill);
	}

	[SkillHandler(SkillId.HT_ANKLESNARE)]
	public class AnkleSnareHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> TrapPlacement.Spawn(caster, target, new AnkleSnareUnit(caster, target.Position, skill.Level), skill);
	}

	[SkillHandler(SkillId.HT_SHOCKWAVE)]
	public class ShockwaveTrapHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> TrapPlacement.Spawn(caster, target, new ShockwaveTrapUnit(caster, target.Position, skill.Level), skill);
	}

	[SkillHandler(SkillId.HT_SANDMAN)]
	public class SandmanHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> TrapPlacement.Spawn(caster, target, new SandmanUnit(caster, target.Position, skill.Level), skill);
	}

	[SkillHandler(SkillId.HT_FLASHER)]
	public class FlasherHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> TrapPlacement.Spawn(caster, target, new FlasherUnit(caster, target.Position, skill.Level), skill);
	}

	[SkillHandler(SkillId.HT_FREEZINGTRAP)]
	public class FreezingTrapHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> TrapPlacement.Spawn(caster, target, new FreezingTrapUnit(caster, target.Position, skill.Level), skill);
	}

	[SkillHandler(SkillId.HT_BLASTMINE)]
	public class BlastMineHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> TrapPlacement.Spawn(caster, target, new BlastMineUnit(caster, target.Position, skill.Level), skill);
	}

	[SkillHandler(SkillId.HT_CLAYMORETRAP)]
	public class ClaymoreTrapHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> TrapPlacement.Spawn(caster, target, new ClaymoreTrapUnit(caster, target.Position, skill.Level), skill);
	}

	[SkillHandler(SkillId.HT_TALKIEBOX)]
	public class TalkieBoxHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> TrapPlacement.Spawn(caster, target, new TalkieBoxUnit(caster, target.Position, skill.Level, "Hello!"), skill);
	}
}
