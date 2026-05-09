using System.Collections.Generic;
using System.Linq;
using Sabine.Shared;
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

	/// <summary>
	/// BS_REPAIRWEAPON — Blacksmith repair-weapon skill. Targets a
	/// player (typically the caster, but party members are valid),
	/// shows the caster a picker of the target's broken items, and on
	/// confirm consumes one same-tier ore to clear Item.IsBroken and
	/// re-equip the item if it was equipped before the break.
	/// Mirrors eAthena skill.c:skill_repairweapon plus the
	/// clif_item_repair_list / clif_item_repaireffect exchange.
	/// </summary>
	[SkillHandler(SkillId.BS_REPAIRWEAPON)]
	public class RepairWeaponHandler : ITargetedSkillHandler
	{
		/// <summary>
		/// Vars.Temp key on the caster pointing at the account id of
		/// the target whose broken items the picker is showing. Cleared
		/// on completion or when the caster casts again.
		/// </summary>
		internal const string PendingTargetVar = "Sabine.Repair.PendingTargetAid";

		public void Handle(UseSkillParams p)
		{
			var caster = p.Character;
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, p.Skill.Id, p.SkillLevel, 0, 0, 0, ActionType.Skill);

			if (caster is not PlayerCharacter pc)
				return;

			// EP4+ only: the alpha / Beta1 / Beta2 clients don't carry
			// the repair opcodes (first appear in EP4 / packet table
			// 0600). On those, keep the original "not yet available"
			// path so the existing tests / smoke flows are unaffected.
			if (Game.Version < Versions.S600)
			{
				pc.ServerMessage("Weapon Repair is not yet available on this client version.");
				return;
			}

			if (p.Target is not PlayerCharacter targetPc)
			{
				pc.ServerMessage("Weapon Repair must target a player.");
				return;
			}

			var broken = targetPc.Inventory.GetItems(static i => i.IsBroken).ToList();
			if (broken.Count == 0)
			{
				pc.ServerMessage("{0} has no broken items.", targetPc == pc ? "You" : targetPc.Name);
				return;
			}

			pc.Vars.Temp.SetInt(PendingTargetVar, targetPc.Connection.Account.Id);
			Send.ZC_REPAIRITEMLIST(pc, broken);
		}
	}
}
