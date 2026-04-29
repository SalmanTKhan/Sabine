using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Blacksmith
{
	// Forging / refining handlers. v1 plays the animation and
	// notifies the player; the per-recipe item-creation pipeline
	// (success rolls, ingredient consumption, refine deltas) is
	// deferred to a dedicated forging epic.

	internal static class ForgeStub
	{
		public static Task Run(Character caster, Skill skill, string what)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.ServerMessage($"{what} crafting UI is not yet available.");
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.BS_ORIDEOCON)]
	public class OrideoconResearchHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => ForgeStub.Run(caster, skill, "Oridecon refinement");
	}

	[SkillHandler(SkillId.BS_DAGGER)]
	public class ForgeDaggerHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => ForgeStub.Run(caster, skill, "Dagger forging");
	}

	[SkillHandler(SkillId.BS_SWORD)]
	public class ForgeSwordHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => ForgeStub.Run(caster, skill, "Sword forging");
	}

	[SkillHandler(SkillId.BS_TWOHANDSWORD)]
	public class ForgeTwoHandSwordHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => ForgeStub.Run(caster, skill, "Two-Handed Sword forging");
	}

	[SkillHandler(SkillId.BS_AXE)]
	public class ForgeAxeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => ForgeStub.Run(caster, skill, "Axe forging");
	}

	[SkillHandler(SkillId.BS_MACE)]
	public class ForgeMaceHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => ForgeStub.Run(caster, skill, "Mace forging");
	}

	[SkillHandler(SkillId.BS_KNUCKLE)]
	public class ForgeKnuckleHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => ForgeStub.Run(caster, skill, "Knuckle forging");
	}

	[SkillHandler(SkillId.BS_SPEAR)]
	public class ForgeSpearHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => ForgeStub.Run(caster, skill, "Spear forging");
	}

	[SkillHandler(SkillId.BS_REPAIRWEAPON)]
	public class RepairWeaponHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => ForgeStub.Run(caster, skill, "Weapon Repair");
	}
}
