using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.Hunter
{
	internal static class TrapPlacement
	{
		public static void Spawn(Character caster, Sabine.Shared.World.Position pos, TrapUnit unit, Skill skill, int level)
		{
			caster.Map.AddSkillUnit(unit);
			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}

	[SkillHandler(SkillId.HT_SKIDTRAP)]
	public class SkidTrapHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> TrapPlacement.Spawn(p.Character, p.TargetPosition, new SkidTrapUnit(p.Character, p.TargetPosition, p.SkillLevel), p.Skill, p.SkillLevel);
	}

	[SkillHandler(SkillId.HT_LANDMINE)]
	public class LandMineHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> TrapPlacement.Spawn(p.Character, p.TargetPosition, new LandMineUnit(p.Character, p.TargetPosition, p.SkillLevel), p.Skill, p.SkillLevel);
	}

	[SkillHandler(SkillId.HT_ANKLESNARE)]
	public class AnkleSnareHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> TrapPlacement.Spawn(p.Character, p.TargetPosition, new AnkleSnareUnit(p.Character, p.TargetPosition, p.SkillLevel), p.Skill, p.SkillLevel);
	}

	[SkillHandler(SkillId.HT_SHOCKWAVE)]
	public class ShockwaveTrapHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> TrapPlacement.Spawn(p.Character, p.TargetPosition, new ShockwaveTrapUnit(p.Character, p.TargetPosition, p.SkillLevel), p.Skill, p.SkillLevel);
	}

	[SkillHandler(SkillId.HT_SANDMAN)]
	public class SandmanHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> TrapPlacement.Spawn(p.Character, p.TargetPosition, new SandmanUnit(p.Character, p.TargetPosition, p.SkillLevel), p.Skill, p.SkillLevel);
	}

	[SkillHandler(SkillId.HT_FLASHER)]
	public class FlasherHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> TrapPlacement.Spawn(p.Character, p.TargetPosition, new FlasherUnit(p.Character, p.TargetPosition, p.SkillLevel), p.Skill, p.SkillLevel);
	}

	[SkillHandler(SkillId.HT_FREEZINGTRAP)]
	public class FreezingTrapHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> TrapPlacement.Spawn(p.Character, p.TargetPosition, new FreezingTrapUnit(p.Character, p.TargetPosition, p.SkillLevel), p.Skill, p.SkillLevel);
	}

	[SkillHandler(SkillId.HT_BLASTMINE)]
	public class BlastMineHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> TrapPlacement.Spawn(p.Character, p.TargetPosition, new BlastMineUnit(p.Character, p.TargetPosition, p.SkillLevel), p.Skill, p.SkillLevel);
	}

	[SkillHandler(SkillId.HT_CLAYMORETRAP)]
	public class ClaymoreTrapHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> TrapPlacement.Spawn(p.Character, p.TargetPosition, new ClaymoreTrapUnit(p.Character, p.TargetPosition, p.SkillLevel), p.Skill, p.SkillLevel);
	}

	[SkillHandler(SkillId.HT_TALKIEBOX)]
	public class TalkieBoxHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> TrapPlacement.Spawn(p.Character, p.TargetPosition, new TalkieBoxUnit(p.Character, p.TargetPosition, p.SkillLevel, "Hello"), p.Skill, p.SkillLevel);
	}
}
