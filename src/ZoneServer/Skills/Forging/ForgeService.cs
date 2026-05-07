using System.Linq;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Shared.Skills.Forging;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Forging
{
	/// <summary>
	/// Resolves a Blacksmith forge attempt: validates materials, rolls
	/// success via the era-active <see cref="IForgeSuccessStrategy"/>,
	/// consumes inputs, produces the weapon.
	/// </summary>
	/// <remarks>
	/// Forging in classic clients is a two-step packet exchange
	/// (ZC_MAKABLEITEMLIST → CZ_REQMAKINGITEM). The full UI flow is not
	/// wired up yet, so v1 picks the highest-tier recipe whose
	/// materials the player has and crafts that. Once the packets are
	/// implemented, the recipe selection moves client-side and this
	/// service stays as the resolver.
	/// </remarks>
	public static class ForgeService
	{
		// eAthena item ids referenced in the forge formula.
		private const int StarCrumbId = 1000;
		private const int FlameHeartId = 994;
		private const int MysticFrozenId = 995;
		private const int RoughWindId = 996;
		private const int GreatNatureId = 997;
		private const int IronAnvilId = 986;
		private const int OrideoconAnvilId = 987;
		private const int GoldenAnvilId = 988;
		private const int EmperiumAnvilId = 989;

		private static readonly IForgeSuccessStrategy ClassicStrategy = new ClassicForgeSuccess();
		private static readonly IForgeSuccessStrategy RenewalStrategy = new RenewalForgeSuccess();

		/// <summary>
		/// Returns the strategy matching the active era. Resolved per
		/// call so era reloads (in tests) are honored without process
		/// restart; callers that hot-loop should cache locally.
		/// </summary>
		public static IForgeSuccessStrategy GetStrategy()
			=> Game.Era == Era.Renewal ? RenewalStrategy : ClassicStrategy;

		/// <summary>
		/// Picks the highest-tier recipe craftable by the given forge
		/// skill at the given level whose materials the player has.
		/// Returns null if nothing is craftable.
		/// </summary>
		public static ProduceData ResolveRecipe(PlayerCharacter pc, SkillId forgeSkill, int skillLevel)
		{
			var produceDb = ZoneServer.Instance.Data.Produce;

			foreach (var recipe in produceDb.FindBySkill(forgeSkill, skillLevel)
				.OrderByDescending(r => r.ItemLevel))
			{
				if (HasMaterials(pc, recipe))
					return recipe;
			}

			return null;
		}

		/// <summary>
		/// Performs a forge attempt: consumes all materials and
		/// optional bonus items (star crumbs, element stone), rolls
		/// success, produces the weapon on success. Returns true on
		/// success, false on failure (materials are still consumed).
		/// </summary>
		public static bool Forge(PlayerCharacter pc, SkillId forgeSkill, int skillLevel, ProduceData recipe)
		{
			if (!HasMaterials(pc, recipe))
				return false;

			foreach (var mat in recipe.Materials)
			{
				if (mat.Amount <= 0)
					continue; // "must own" guides; not consumed
				ConsumeFromInventory(pc, mat.ItemId, mat.Amount);
			}

			var starCrumbs = ConsumeUpTo(pc, StarCrumbId, 3);
			var elementStone = TryConsumeAnyElementStone(pc);
			var isRenewal = Game.Era == Era.Renewal;
			var anvilBonus = ResolveAnvilBonus(pc, isRenewal);
			// Renewal mixes a per-cast random term into the formula;
			// classic ignores it.
			var randomBoost = isRenewal ? RandomProvider.Get().Next(1, 101) * 10 : 0;

			var ctx = new ForgeContext(
				dex: pc.Parameters.Dex,
				luk: pc.Parameters.Luk,
				jobLevel: pc.Parameters.JobLevel,
				weaponLevel: recipe.ItemLevel,
				forgeSkillLevel: pc.Skills.GetLevel(forgeSkill),
				weaponResearchLevel: pc.Skills.GetLevel(SkillId.BS_WEAPONRESEARCH),
				oridecoResearchLevel: pc.Skills.GetLevel(SkillId.BS_ORIDEOCON),
				starCrumbs: starCrumbs,
				elementStone: elementStone,
				anvilBonus: anvilBonus,
				randomBoost: randomBoost
			);

			var perTenThousand = GetStrategy().CalcSuccessPer10000(ctx);
			var roll = RandomProvider.Get().Next(10000);
			if (roll >= perTenThousand)
				return false;

			pc.Inventory.AddItem(new Item(recipe.TargetItemId, 1));
			return true;
		}

		private static bool HasMaterials(PlayerCharacter pc, ProduceData recipe)
		{
			foreach (var mat in recipe.Materials)
			{
				if (mat.Amount <= 0)
				{
					if (pc.Inventory.CountItem(mat.ItemId) <= 0)
						return false;
				}
				else if (pc.Inventory.CountItem(mat.ItemId) < mat.Amount)
				{
					return false;
				}
			}
			return true;
		}

		private static void ConsumeFromInventory(PlayerCharacter pc, int itemId, int amount)
		{
			foreach (var item in pc.Inventory.GetItems(i => i.ClassId == itemId))
			{
				if (amount <= 0) break;
				var take = System.Math.Min(item.Amount, amount);
				pc.Inventory.DecrementItem(item, take);
				amount -= take;
			}
		}

		private static int ConsumeUpTo(PlayerCharacter pc, int itemId, int max)
		{
			var available = pc.Inventory.CountItem(itemId);
			var take = System.Math.Min(available, max);
			if (take > 0)
				ConsumeFromInventory(pc, itemId, take);
			return take;
		}

		private static bool TryConsumeAnyElementStone(PlayerCharacter pc)
		{
			foreach (var id in new[] { FlameHeartId, MysticFrozenId, RoughWindId, GreatNatureId })
			{
				if (pc.Inventory.CountItem(id) > 0)
				{
					ConsumeFromInventory(pc, id, 1);
					return true;
				}
			}
			return false;
		}

		private static int ResolveAnvilBonus(PlayerCharacter pc, bool isRenewal)
		{
			// Anvils are not consumed, only checked for presence. Best
			// anvil wins. Matches eAthena's else-if chain. Oridecon
			// Anvil pays out 300 in classic eAthena and 250 in rAthena
			// renewal; everything else is identical across branches.
			if (pc.Inventory.CountItem(EmperiumAnvilId) > 0) return 1000;
			if (pc.Inventory.CountItem(GoldenAnvilId) > 0) return 500;
			if (pc.Inventory.CountItem(OrideoconAnvilId) > 0) return isRenewal ? 250 : 300;
			if (pc.Inventory.CountItem(IronAnvilId) > 0) return 0;
			return 0;
		}
	}
}
