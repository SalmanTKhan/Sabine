using System;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills
{
	/// <summary>
	/// Applies the Merchant / Rogue price-adjustment passives to NPC shop
	/// transactions. Mirrors eAthena pc.c <c>pc_modifybuyvalue</c> and
	/// <c>pc_modifysellvalue</c>:
	/// <list type="bullet">
	/// <item>MC_DISCOUNT  — buy rate = 5 + 2*lv (-1 at lv10), max 24%</item>
	/// <item>RG_COMPULSION — buy rate = 5 + 4*lv, stacks via max() with Discount</item>
	/// <item>MC_OVERCHARGE — sell rate = 5 + 2*lv (-1 at lv10), max 24%</item>
	/// </list>
	/// </summary>
	public static class MerchantPricing
	{
		/// <summary>
		/// Returns the buy price after Discount / Compulsion. Identical to
		/// <paramref name="basePrice"/> when no skill applies. Clamps to a
		/// floor of 1 zeny so a heavily discounted but priced item never
		/// becomes free.
		/// </summary>
		public static int GetBuyPrice(PlayerCharacter character, int basePrice)
		{
			if (character == null || basePrice <= 0)
				return basePrice;

			var rate = GetBuyDiscountRate(character);
			if (rate <= 0)
				return basePrice;

			var adjusted = (int)((long)basePrice * (100 - rate) / 100);
			return Math.Max(1, adjusted);
		}

		/// <summary>
		/// Returns the sell price after Overcharge. Identical to
		/// <paramref name="basePrice"/> when no skill applies.
		/// </summary>
		public static int GetSellPrice(PlayerCharacter character, int basePrice)
		{
			if (character == null || basePrice <= 0)
				return basePrice;

			var rate = GetSellMarkupRate(character);
			if (rate <= 0)
				return basePrice;

			var adjusted = (int)((long)basePrice * (100 + rate) / 100);
			return Math.Max(1, adjusted);
		}

		private static int GetBuyDiscountRate(PlayerCharacter character)
		{
			var skills = character.Skills;
			if (skills == null)
				return 0;

			var rate = 0;

			var discount = skills.GetLevel(SkillId.MC_DISCOUNT);
			if (discount > 0)
				rate = 5 + 2 * discount - (discount == 10 ? 1 : 0);

			var compulsion = skills.GetLevel(SkillId.RG_COMPULSION);
			if (compulsion > 0)
			{
				var compRate = 5 + 4 * compulsion;
				if (compRate > rate)
					rate = compRate;
			}

			return rate;
		}

		private static int GetSellMarkupRate(PlayerCharacter character)
		{
			var skills = character.Skills;
			if (skills == null)
				return 0;

			var overcharge = skills.GetLevel(SkillId.MC_OVERCHARGE);
			if (overcharge <= 0)
				return 0;

			return 5 + 2 * overcharge - (overcharge == 10 ? 1 : 0);
		}
	}
}
