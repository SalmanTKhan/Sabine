using System.Collections.Generic;
using System.Linq;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// On-hit equip-break roller. Reads attacker-side SC_MELTDOWN
	/// (WS_MELTDOWN) and rolls the per-1000 weapon / armor break
	/// chances stored on its val2 / val3 (eAthena status.c:5867).
	/// CR_FULLPROTECTION on the target shields all four equip slots
	/// from the roll, mirroring eAthena's pc_skill_isequip_break
	/// guard.
	///
	/// Works through the existing Item.IsDamaged flag (already
	/// persisted as the <c>damaged</c> column); flipping it triggers
	/// stat-bonus skip on the next equip recalc, and BS_REPAIRWEAPON
	/// (already wired in ChatCommands /repair) clears it.
	/// </summary>
	public static class EquipBreakListener
	{
		public static void Subscribe()
		{
			CombatEvents.AttackResolved += OnAttackResolved;
		}

		private static void OnAttackResolved(AttackContext ctx, AttackResult result)
		{
			if (result.IsMiss) return;
			if (result.Damage <= 0) return;

			var attacker = ctx.Attacker;
			if (attacker?.StatusEffects == null) return;

			var meltDown = attacker.StatusEffects.Get(StatusId.MeltDown);
			if (meltDown == null) return;

			var target = ctx.Target;
			if (target is not PlayerCharacter pc) return;

			// CR_FULLPROTECTION shields all four equip slots from the
			// break roll. The actual eA flag is per-slot CP_*; Sabine
			// collapsed those into a single status, so the guard is
			// blanket here too.
			if (target.StatusEffects?.Has(StatusId.FullProtection) == true) return;

			var rnd = RandomProvider.Get();
			var weaponRate = meltDown.Val2; // per-1000
			var armorRate = meltDown.Val3;  // per-1000

			if (weaponRate > 0 && rnd.Next(1000) < weaponRate)
				BreakRandomEquip(pc, WeaponSlots);

			if (armorRate > 0 && rnd.Next(1000) < armorRate)
				BreakRandomEquip(pc, ArmorSlots);
		}

		public static readonly EquipSlots[] WeaponSlots = new[] { EquipSlots.RightHand };
		public static readonly EquipSlots[] ArmorSlots = new[] { EquipSlots.Body, EquipSlots.HeadTop, EquipSlots.HeadMiddle, EquipSlots.HeadBottom, EquipSlots.Shoes, EquipSlots.Robe };

		/// <summary>
		/// Breaks a random equipped item belonging to one of the given
		/// slots. Public so other on-hit paths (Tarot Hanged Man,
		/// future skill_break_equip callers) can reuse the same
		/// pick / unequip / flag flow.
		/// </summary>
		public static void BreakRandomEquip(PlayerCharacter pc, IReadOnlyList<EquipSlots> candidateSlots)
		{
			var equipped = pc.Inventory.GetItems(static i => i.IsEquipped && !i.IsDamaged).ToList();
			var matches = equipped.Where(i => candidateSlots.Any(s => (i.EquippedOn & s) != 0)).ToList();
			if (matches.Count == 0) return;

			var pick = matches[RandomProvider.Get().Next(matches.Count)];
			pc.Inventory.UnequipItem(pick);
			pick.IsDamaged = true;
			pc.ServerMessage("Your {0} broke!", pick.Data.Name);
		}
	}
}
