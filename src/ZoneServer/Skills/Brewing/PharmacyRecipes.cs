using System.Collections.Generic;

namespace Sabine.Zone.Skills.Brewing
{
	/// <summary>
	/// Single brewing recipe: consume one Medicine Bowl plus the
	/// recipe ingredients, produce <see cref="OutputId"/> on success.
	/// Counts apply per ingredient slot.
	/// </summary>
	public class PharmacyRecipe
	{
		public int OutputId { get; init; }
		public int OutputAmount { get; init; } = 1;
		public List<(int ItemId, int Amount)> Ingredients { get; init; } = new();

		/// <summary>
		/// Skill-level threshold: this recipe only appears in the
		/// craft list when AM_PHARMACY is at this level or higher.
		/// </summary>
		public int RequiredLevel { get; init; } = 1;
	}

	/// <summary>
	/// Static recipe table. v1 covers the four base potions and a
	/// condensed red potion as a higher-level option. A JSON-driven
	/// recipe DB is a follow-up.
	/// </summary>
	public static class PharmacyRecipes
	{
		// Item ids from eAthena classic db.
		private const int MedicineBowl = 7136;
		private const int EmptyBottle = 713;
		private const int RedHerb = 507;
		private const int YellowHerb = 508;
		private const int WhiteHerb = 509;
		private const int BlueHerb = 510;

		private const int RedPotion = 501;
		private const int YellowPotion = 503;
		private const int WhitePotion = 504;
		private const int BluePotion = 505;
		private const int CondensedRedPotion = 545;

		public static readonly List<PharmacyRecipe> All = new()
		{
			new PharmacyRecipe
			{
				OutputId = RedPotion, OutputAmount = 3, RequiredLevel = 1,
				Ingredients = { (RedHerb, 1), (EmptyBottle, 1), (MedicineBowl, 1) },
			},
			new PharmacyRecipe
			{
				OutputId = YellowPotion, OutputAmount = 3, RequiredLevel = 3,
				Ingredients = { (YellowHerb, 1), (EmptyBottle, 1), (MedicineBowl, 1) },
			},
			new PharmacyRecipe
			{
				OutputId = WhitePotion, OutputAmount = 3, RequiredLevel = 5,
				Ingredients = { (WhiteHerb, 1), (EmptyBottle, 1), (MedicineBowl, 1) },
			},
			new PharmacyRecipe
			{
				OutputId = BluePotion, OutputAmount = 3, RequiredLevel = 6,
				Ingredients = { (BlueHerb, 1), (EmptyBottle, 1), (MedicineBowl, 1) },
			},
			new PharmacyRecipe
			{
				OutputId = CondensedRedPotion, OutputAmount = 1, RequiredLevel = 7,
				Ingredients = { (RedPotion, 1), (RedHerb, 1), (MedicineBowl, 1) },
			},
		};
	}
}
