using System.Linq;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Brewing
{
	/// <summary>
	/// Resolves a Pharmacy attempt: validates ingredients, rolls
	/// success, consumes inputs, produces output. Called from
	/// <c>AM_PHARMACY</c>'s skill handler.
	/// </summary>
	public static class PharmacyService
	{
		/// <summary>
		/// Returns the first recipe the player can attempt at the
		/// given skill level. v1 picks the highest-level recipe whose
		/// ingredients are fully present in the inventory.
		/// </summary>
		public static PharmacyRecipe ResolveRecipe(PlayerCharacter pc, int skillLevel)
		{
			foreach (var recipe in PharmacyRecipes.All
				.Where(r => r.RequiredLevel <= skillLevel)
				.OrderByDescending(r => r.RequiredLevel))
			{
				if (HasIngredients(pc, recipe))
					return recipe;
			}
			return null;
		}

		public static bool HasIngredients(PlayerCharacter pc, PharmacyRecipe recipe)
		{
			foreach (var (itemId, amount) in recipe.Ingredients)
			{
				var owned = pc.Inventory.GetItems(i => i.ClassId == itemId).Sum(i => i.Amount);
				if (owned < amount) return false;
			}
			return true;
		}

		/// <summary>
		/// Pre-renewal-style success rate: <c>baseRate + 5*skillLv +
		/// dex/4 + int/8 + jobLv/2</c>, clamped 5..95.
		/// </summary>
		public static int SuccessChance(PlayerCharacter pc, int skillLevel)
		{
			var rate = 30
				+ 5 * skillLevel
				+ pc.Parameters.Dex / 4
				+ pc.Parameters.Int / 8
				+ pc.Parameters.JobLevel / 2;
			return System.Math.Clamp(rate, 5, 95);
		}

		/// <summary>
		/// Performs the attempt. Always consumes ingredients;
		/// produces output on success.
		/// </summary>
		public static bool Brew(PlayerCharacter pc, PharmacyRecipe recipe, int skillLevel)
		{
			if (!HasIngredients(pc, recipe))
				return false;

			foreach (var (itemId, amount) in recipe.Ingredients)
				ConsumeFromInventory(pc, itemId, amount);

			var rate = SuccessChance(pc, skillLevel);
			if (RandomProvider.Get().Next(100) >= rate)
				return false;

			pc.Inventory.AddItem(new Item(recipe.OutputId, recipe.OutputAmount));
			return true;
		}

		private static void ConsumeFromInventory(PlayerCharacter pc, int itemId, int amount)
		{
			foreach (var item in pc.Inventory.GetItems(i => i.ClassId == itemId).ToList())
			{
				if (amount <= 0) break;
				var take = System.Math.Min(item.Amount, amount);
				pc.Inventory.DecrementItem(item, take);
				amount -= take;
			}
		}
	}
}
