using System.Collections.Generic;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Outcome of a <see cref="RefinementService.TryRefine"/> call.
	/// </summary>
	public enum RefineResult
	{
		Success,
		/// <summary>Failure that left the item unchanged.</summary>
		Failed,
		/// <summary>Failure that destroyed the item.</summary>
		Broken,
		/// <summary>Failure that reduced the item's refine level.</summary>
		Downgraded,
		AtMaxRefine,
		MissingMaterial,
		MissingZeny,
		NotRefinable,
		CostTypeUnavailable,
	}

	/// <summary>
	/// Computes refine bonuses, success rates, and per-attempt costs
	/// for equipped items. Backed by <see cref="RefineDb"/>, which is
	/// loaded from the era-specific refine_db file at startup.
	/// </summary>
	/// <remarks>
	/// Armor bonuses use eAthena's hundredths-of-a-DEF scale: the
	/// per-refine number stored in the db is divided by 100 when
	/// applied, so refine +4 on classic armor (4 * 70 / 100) yields
	/// +2 DEF.
	/// </remarks>
	public static class RefinementService
	{
		// Refine level at which a successful upgrade is broadcast to
		// the whole server (matches eAthena's "Praise the Almighty"
		// announcement on +10 weapons).
		private const int BroadcastThreshold = 10;

		private const int ArmorBonusDivisor = 100;

		/// <summary>
		/// Returns the additional ATK granted by refining a weapon to
		/// the given refine level. Returns 0 for invalid inputs (e.g.
		/// unrefined, missing data row).
		/// </summary>
		public static int GetWeaponAtkBonus(int weaponLevel, int refineLevel)
		{
			if (weaponLevel < 1 || weaponLevel > 4 || refineLevel <= 0)
				return 0;

			var entry = ZoneServer.Instance.Data.Refine.GetWeapon(weaponLevel);
			return entry?.GetRawBonus(refineLevel) ?? 0;
		}

		/// <summary>
		/// Returns the additional DEF granted by refining an armor
		/// piece to the given refine level. <paramref name="armorLevel"/>
		/// selects the renewal armor tier (1..4); the lookup falls
		/// back to lv1 when the requested tier isn't in the data.
		/// </summary>
		public static int GetArmorDefBonus(int armorLevel, int refineLevel)
		{
			if (refineLevel <= 0)
				return 0;

			var entry = ZoneServer.Instance.Data.Refine.GetArmor(armorLevel);
			return (entry?.GetRawBonus(refineLevel) ?? 0) / ArmorBonusDivisor;
		}

		/// <summary>
		/// Returns the maximum refine level the active era's data
		/// allows for the given group and tier (weapon level for
		/// weapons, armor level for armor). 0 if the row is missing.
		/// </summary>
		public static int GetMaxRefine(RefineGroup group, int tier)
			=> Lookup(group, tier)?.MaxRefineLevel ?? 0;

		/// <summary>
		/// Returns the percent chance of the next refine attempt
		/// (taking <paramref name="currentRefine"/> to
		/// <c>currentRefine + 1</c>) succeeding via the given cost
		/// type. Returns 0 when the next step is past the cap or the
		/// cost type isn't supported at that level.
		/// </summary>
		public static int GetSuccessRate(RefineGroup group, int tier, int currentRefine, RefineCostType costType = RefineCostType.Normal)
			=> Lookup(group, tier)?.GetSuccessRate(currentRefine, costType) ?? 0;

		/// <summary>
		/// Returns the cost types available for the next refine
		/// attempt on the given item, in data order.
		/// </summary>
		public static IReadOnlyList<RefineCostType> GetAvailableCostTypes(Item item)
		{
			var group = ResolveGroup(item);
			if (group == null)
				return System.Array.Empty<RefineCostType>();

			var tier = ResolveTier(item, group.Value);
			var entry = Lookup(group.Value, tier);
			return entry?.GetAvailableCostTypes(item.RefineLevel) ?? System.Array.Empty<RefineCostType>();
		}

		/// <summary>
		/// Returns the per-group tier for an item: weapon level for
		/// weapons, armor level for armor (defaulting to 1).
		/// </summary>
		public static int ResolveTier(Item item, RefineGroup group)
			=> group == RefineGroup.Armor
				? System.Math.Max(1, item.Data.ArmorLevel)
				: item.Data.WeaponLevel;

		/// <summary>
		/// Returns the equipment group (Armor / Weapon) the given item
		/// belongs to for refining purposes, or null if the item is
		/// not refinable.
		/// </summary>
		public static RefineGroup? ResolveGroup(Item item)
		{
			if (item == null)
				return null;

			var type = item.Data.Type;
			if (type == ItemType.Weapon || type == ItemType.RangedWeapon || type == ItemType.Weapon3)
			{
				if (item.Data.WeaponLevel >= 1 && item.Data.WeaponLevel <= 4)
					return RefineGroup.Weapon;
				return null;
			}

			return type == ItemType.Armor ? RefineGroup.Armor : null;
		}

		/// <summary>
		/// Attempts a single refine on the given item using the
		/// specified cost type. Consumes material and zeny on every
		/// attempt; on success the refine level is incremented (and
		/// broadcast at <c>+10</c>); on failure the chance's
		/// <c>BreakingRate</c> and <c>DowngradeAmount</c> determine
		/// whether the item breaks, downgrades, or is left unchanged.
		/// </summary>
		/// <remarks>
		/// The full client crafting UI flow (CZ_REQ_ITEMREFINING /
		/// ZC_ACK_ITEMREFINING) is later than the alpha clients Sabine
		/// targets by default, so this is driven via NPC dialog. The
		/// service is the single resolver for both flows.
		/// </remarks>
		public static RefineResult TryRefine(PlayerCharacter player, Item item, RefineCostType costType = RefineCostType.Normal)
		{
			var group = ResolveGroup(item);
			if (group == null)
				return RefineResult.NotRefinable;

			var tier = ResolveTier(item, group.Value);
			var entry = Lookup(group.Value, tier);
			if (entry == null)
				return RefineResult.NotRefinable;

			if (item.RefineLevel >= entry.MaxRefineLevel)
				return RefineResult.AtMaxRefine;

			var chance = entry.GetChance(item.RefineLevel, costType);
			if (chance == null || chance.MaterialItemId == 0)
				return RefineResult.CostTypeUnavailable;

			if (player.Inventory.CountItem(chance.MaterialItemId) < 1)
				return RefineResult.MissingMaterial;
			if (player.Parameters.Zeny < chance.Price)
				return RefineResult.MissingZeny;

			ConsumeOne(player, chance.MaterialItemId);
			if (chance.Price > 0)
				player.Parameters.Modify(ParameterType.Zeny, -chance.Price);

			var rng = RandomProvider.Get();
			if (rng.Next(100) < chance.Rate)
			{
				item.RefineLevel = (byte)(item.RefineLevel + 1);
				player.Inventory.RefreshClient();
				player.Parameters.RecalculateAll();
				Send.ZC_NOTIFY_EFFECT(player, 3); // RefineUp

				if (item.RefineLevel >= BroadcastThreshold)
				{
					Send.ZC_BROADCAST(string.Format(
						"Praise the Almighty! {0} has refined a +{1} {2}!",
						player.Name, item.RefineLevel, item.Data.Name));
				}

				return RefineResult.Success;
			}

			Send.ZC_NOTIFY_EFFECT(player, 2); // RefineFail

			// Failure handling is data-driven: BreakingRate models
			// classic's "destroy above safe limit" and renewal's
			// per-level break tables; DowngradeAmount is the renewal
			// HD-ore alternative that drops the refine instead of
			// destroying the item.
			if (chance.BreakingRate > 0 && rng.Next(100) < chance.BreakingRate)
			{
				// Two paths, gated by world.conf:
				//   refine_break_destroys = true  → legacy behaviour, the
				//     item is removed from the inventory entirely. Kept
				//     for back-compat with servers that don't want a
				//     repair-weapon flow.
				//   refine_break_destroys = false (default) → durability
				//     model: force-unequip the item, flag it as broken,
				//     and persist. The item stays in the inventory and
				//     can be restored via BS_REPAIRWEAPON / @repair.
				if (ZoneServer.Instance.Conf.World.RefineBreakDestroys)
				{
					player.Inventory.RemoveItem(item);
				}
				else
				{
					if (item.IsEquipped)
						player.Inventory.UnequipItem(item);
					item.IsBroken = true;
					player.Inventory.RefreshClient();
					player.Parameters.RecalculateAll();
				}
				return RefineResult.Broken;
			}

			if (chance.DowngradeAmount > 0)
			{
				var newLevel = System.Math.Max(0, item.RefineLevel - chance.DowngradeAmount);
				item.RefineLevel = (byte)newLevel;
				player.Inventory.RefreshClient();
				player.Parameters.RecalculateAll();
				return RefineResult.Downgraded;
			}

			return RefineResult.Failed;
		}

		private static RefineEntry Lookup(RefineGroup group, int tier)
			=> group == RefineGroup.Armor
				? ZoneServer.Instance.Data.Refine.GetArmor(tier)
				: ZoneServer.Instance.Data.Refine.GetWeapon(tier);

		private static void ConsumeOne(PlayerCharacter player, int itemId)
		{
			foreach (var stack in player.Inventory.GetItems(i => i.ClassId == itemId))
			{
				player.Inventory.DecrementItem(stack, 1);
				return;
			}
		}
	}
}
