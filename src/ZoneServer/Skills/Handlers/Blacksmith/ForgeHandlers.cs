using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.Skills.Forging;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Blacksmith
{
	/// <summary>
	/// Shared dispatch for the seven Blacksmith forge skills. Routes to
	/// <see cref="ForgeService"/> after looking up a craftable recipe.
	/// The full client crafting UI is not yet wired up; until it is,
	/// the server picks the highest-tier recipe the player can make
	/// from their inventory and crafts that.
	/// </summary>
	internal static class ForgeDispatch
	{
		public static void Run(UseSkillParams parameters, SkillId forgeSkill, string what)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (caster is not PlayerCharacter pc)
				return;

			var recipe = ForgeService.ResolveRecipe(pc, forgeSkill, level);
			if (recipe == null)
			{
				pc.ServerMessage($"You don't have the materials to forge any {what.ToLowerInvariant()}.");
				return;
			}

			var success = ForgeService.Forge(pc, forgeSkill, level, recipe);
			if (success)
				pc.ServerMessage($"You successfully forged a weapon (item id {recipe.TargetItemId}).");
			else
				pc.ServerMessage($"Forging failed; materials were lost.");
		}
	}

	[SkillHandler(SkillId.BS_DAGGER)]
	public class ForgeDaggerHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeDispatch.Run(p, SkillId.BS_DAGGER, "Dagger");
	}

	[SkillHandler(SkillId.BS_SWORD)]
	public class ForgeSwordHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeDispatch.Run(p, SkillId.BS_SWORD, "Sword");
	}

	[SkillHandler(SkillId.BS_TWOHANDSWORD)]
	public class ForgeTwoHandSwordHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeDispatch.Run(p, SkillId.BS_TWOHANDSWORD, "Two-Handed Sword");
	}

	[SkillHandler(SkillId.BS_AXE)]
	public class ForgeAxeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeDispatch.Run(p, SkillId.BS_AXE, "Axe");
	}

	[SkillHandler(SkillId.BS_MACE)]
	public class ForgeMaceHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeDispatch.Run(p, SkillId.BS_MACE, "Mace");
	}

	[SkillHandler(SkillId.BS_KNUCKLE)]
	public class ForgeKnuckleHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeDispatch.Run(p, SkillId.BS_KNUCKLE, "Knuckle");
	}

	[SkillHandler(SkillId.BS_SPEAR)]
	public class ForgeSpearHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => ForgeDispatch.Run(p, SkillId.BS_SPEAR, "Spear");
	}

	[SkillHandler(SkillId.BS_ORIDEOCON)]
	public class OrideoconResearchHandler : ITargetedSkillHandler
	{
		// Passive in classic; using it just acknowledges. The +100/lv
		// chance bonus is consumed inside ForgeService for wlv >= 3.
		public void Handle(UseSkillParams p)
		{
			var caster = p.Character;
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, p.Skill.Id, p.SkillLevel, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.BS_REPAIRWEAPON)]
	public class RepairWeaponHandler : ITargetedSkillHandler
	{
		// TODO(repair-weapon): Full implementation requires modeling a
		// "broken" state on Item (refine-break currently deletes the
		// item, so there are no broken weapons to repair) and wiring
		// the ZC_REPAIRITEMLIST / CZ_REQ_ITEMREPAIR / ZC_ACK_ITEMREPAIR
		// exchange. The CZ_REQ_ITEMREPAIR opcode is EP4+ (0x01FD); the
		// alpha client this server primarily targets does not carry
		// the repair packet at all, so even a partial wire-up would
		// only benefit higher-version clients. Until both prerequisites
		// land we acknowledge the cast and tell the player it is not
		// available.
		public void Handle(UseSkillParams p)
		{
			var caster = p.Character;
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, p.Skill.Id, p.SkillLevel, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.ServerMessage("Weapon Repair is not yet available.");
		}
	}
}
