using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Blacksmith
{
	internal static class ForgeStub
	{
		public static void Run(UseSkillParams parameters, string what)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.ServerMessage($"{what} crafting UI is not yet available.");
		}
	}

	[SkillHandler(SkillId.BS_ORIDEOCON)]
	public class OrideoconResearchHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeStub.Run(p, "Oridecon refinement");
	}

	[SkillHandler(SkillId.BS_DAGGER)]
	public class ForgeDaggerHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeStub.Run(p, "Dagger forging");
	}

	[SkillHandler(SkillId.BS_SWORD)]
	public class ForgeSwordHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeStub.Run(p, "Sword forging");
	}

	[SkillHandler(SkillId.BS_TWOHANDSWORD)]
	public class ForgeTwoHandSwordHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeStub.Run(p, "Two-Handed Sword forging");
	}

	[SkillHandler(SkillId.BS_AXE)]
	public class ForgeAxeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeStub.Run(p, "Axe forging");
	}

	[SkillHandler(SkillId.BS_MACE)]
	public class ForgeMaceHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeStub.Run(p, "Mace forging");
	}

	[SkillHandler(SkillId.BS_KNUCKLE)]
	public class ForgeKnuckleHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeStub.Run(p, "Knuckle forging");
	}

	[SkillHandler(SkillId.BS_SPEAR)]
	public class ForgeSpearHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeStub.Run(p, "Spear forging");
	}

	[SkillHandler(SkillId.BS_REPAIRWEAPON)]
	public class RepairWeaponHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeStub.Run(p, "Weapon Repair");
	}
}
