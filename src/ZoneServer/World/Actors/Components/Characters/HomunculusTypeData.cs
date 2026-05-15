using System.Collections.Generic;
using Sabine.Shared.Const;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// Static per-type tables: stat-growth coefficients, favored
	/// foods, and the list of skills a given homunculus type can
	/// learn. v1 hardcodes the values; promoting to a JSON db
	/// (system/data/homunculus.txt) is a follow-up that doesn't
	/// require touching consumers.
	/// </summary>
	public static class HomunculusTypeData
	{
		public class GrowthCurve
		{
			public int HpPerLevel { get; init; }
			public int AtkPerLevel { get; init; }
			public int DefPerLevel { get; init; }

			// Stat growth — averages from eAthena classic tables,
			// rounded to integers per level. Real homunculi roll
			// each stat from a min/max range; these flat rates are
			// the median outcome.
			public int StrPerLevel { get; init; }
			public int AgiPerLevel { get; init; }
			public int VitPerLevel { get; init; }
			public int IntPerLevel { get; init; }
			public int DexPerLevel { get; init; }
			public int LukPerLevel { get; init; }
		}

		public class FoodEntry
		{
			public int ItemId { get; init; }
			public int Hunger { get; init; } = 11;
			public int IntimacyBonus { get; init; } = 1;
		}

		public class SkillEntry
		{
			public SkillId SkillId { get; init; }
			public int MaxLevel { get; init; } = 5;
			public int RequiredHomunLevel { get; init; } = 1;
			public SkillId Prerequisite { get; init; } = SkillId.None;
			public int PrerequisiteLevel { get; init; }
		}

		// Item ids from eAthena classic db.
		private const int PetFood = 537;
		private const int Apple = 512;
		private const int Banana = 513;
		private const int Carrot = 515;
		private const int FreshFish = 544;
		private const int Mushroom = 521;

		private static readonly Dictionary<HomunculusType, GrowthCurve> _growth = new()
		{
			// Lif: support / healer — INT/DEX heavy, low STR.
			[HomunculusType.Lif] = new GrowthCurve
			{
				HpPerLevel = 22, AtkPerLevel = 1, DefPerLevel = 1,
				StrPerLevel = 0, AgiPerLevel = 1, VitPerLevel = 1,
				IntPerLevel = 2, DexPerLevel = 1, LukPerLevel = 1,
			},
			// Amistr: tank — VIT/STR heavy, slow.
			[HomunculusType.Amistr] = new GrowthCurve
			{
				HpPerLevel = 30, AtkPerLevel = 2, DefPerLevel = 3,
				StrPerLevel = 2, AgiPerLevel = 0, VitPerLevel = 2,
				IntPerLevel = 0, DexPerLevel = 1, LukPerLevel = 1,
			},
			// Filir: glass cannon — AGI/DEX heavy, fragile.
			[HomunculusType.Filir] = new GrowthCurve
			{
				HpPerLevel = 16, AtkPerLevel = 3, DefPerLevel = 1,
				StrPerLevel = 1, AgiPerLevel = 2, VitPerLevel = 0,
				IntPerLevel = 0, DexPerLevel = 2, LukPerLevel = 1,
			},
			// Vanilmirth: balanced caster — INT/STR mixed.
			[HomunculusType.Vanilmirth] = new GrowthCurve
			{
				HpPerLevel = 20, AtkPerLevel = 2, DefPerLevel = 2,
				StrPerLevel = 1, AgiPerLevel = 1, VitPerLevel = 1,
				IntPerLevel = 2, DexPerLevel = 1, LukPerLevel = 0,
			},

			// Evolved forms — eAthena classic gives ~30% steeper growth
			// across the board. Stat profile stays the same as the
			// base family (Lif2 still INT-leaning, Amistr2 still tank).
			[HomunculusType.Lif2] = new GrowthCurve
			{
				HpPerLevel = 28, AtkPerLevel = 2, DefPerLevel = 2,
				StrPerLevel = 1, AgiPerLevel = 1, VitPerLevel = 1,
				IntPerLevel = 3, DexPerLevel = 2, LukPerLevel = 1,
			},
			[HomunculusType.Amistr2] = new GrowthCurve
			{
				HpPerLevel = 40, AtkPerLevel = 3, DefPerLevel = 4,
				StrPerLevel = 3, AgiPerLevel = 1, VitPerLevel = 3,
				IntPerLevel = 0, DexPerLevel = 1, LukPerLevel = 1,
			},
			[HomunculusType.Filir2] = new GrowthCurve
			{
				HpPerLevel = 22, AtkPerLevel = 4, DefPerLevel = 1,
				StrPerLevel = 1, AgiPerLevel = 3, VitPerLevel = 1,
				IntPerLevel = 0, DexPerLevel = 3, LukPerLevel = 1,
			},
			[HomunculusType.Vanilmirth2] = new GrowthCurve
			{
				HpPerLevel = 26, AtkPerLevel = 3, DefPerLevel = 2,
				StrPerLevel = 2, AgiPerLevel = 1, VitPerLevel = 1,
				IntPerLevel = 3, DexPerLevel = 1, LukPerLevel = 1,
			},

			// S-class — renewal-era. Bumped per-level numbers reflect
			// the +stat advantage S-class homunculi have over their
			// evolved counterparts in rA's homunculus_db.yml.
			[HomunculusType.Eira] = new GrowthCurve
			{
				HpPerLevel = 32, AtkPerLevel = 3, DefPerLevel = 2,
				StrPerLevel = 2, AgiPerLevel = 2, VitPerLevel = 2,
				IntPerLevel = 4, DexPerLevel = 2, LukPerLevel = 2,
			},
			[HomunculusType.Bayeri] = new GrowthCurve
			{
				HpPerLevel = 46, AtkPerLevel = 4, DefPerLevel = 5,
				StrPerLevel = 4, AgiPerLevel = 1, VitPerLevel = 4,
				IntPerLevel = 1, DexPerLevel = 2, LukPerLevel = 2,
			},
			[HomunculusType.Sera] = new GrowthCurve
			{
				HpPerLevel = 26, AtkPerLevel = 5, DefPerLevel = 2,
				StrPerLevel = 2, AgiPerLevel = 4, VitPerLevel = 1,
				IntPerLevel = 1, DexPerLevel = 4, LukPerLevel = 2,
			},
			[HomunculusType.Dieter] = new GrowthCurve
			{
				HpPerLevel = 30, AtkPerLevel = 4, DefPerLevel = 3,
				StrPerLevel = 3, AgiPerLevel = 2, VitPerLevel = 2,
				IntPerLevel = 4, DexPerLevel = 2, LukPerLevel = 1,
			},
		};

		// Per-type favored foods: the first entry is the canonical
		// food (largest intimacy bonus); Pet Food works for everyone
		// at the baseline.
		private static readonly Dictionary<HomunculusType, List<FoodEntry>> _foods = new()
		{
			[HomunculusType.Lif] = new()
			{
				new FoodEntry { ItemId = PetFood, Hunger = 11, IntimacyBonus = 2 },
				new FoodEntry { ItemId = Apple, Hunger = 8, IntimacyBonus = 3 },
			},
			[HomunculusType.Amistr] = new()
			{
				new FoodEntry { ItemId = PetFood, Hunger = 11, IntimacyBonus = 2 },
				new FoodEntry { ItemId = Carrot, Hunger = 8, IntimacyBonus = 3 },
			},
			[HomunculusType.Filir] = new()
			{
				new FoodEntry { ItemId = PetFood, Hunger = 11, IntimacyBonus = 2 },
				new FoodEntry { ItemId = Banana, Hunger = 8, IntimacyBonus = 3 },
				new FoodEntry { ItemId = FreshFish, Hunger = 11, IntimacyBonus = 2 },
			},
			[HomunculusType.Vanilmirth] = new()
			{
				new FoodEntry { ItemId = PetFood, Hunger = 11, IntimacyBonus = 2 },
				new FoodEntry { ItemId = Mushroom, Hunger = 8, IntimacyBonus = 3 },
			},
		};

		// Per-type learnable skills + prereqs. Levels and prereqs
		// follow the eAthena homun_skill_db structure abridged.
		private static readonly Dictionary<HomunculusType, List<SkillEntry>> _skills = new()
		{
			[HomunculusType.Lif] = new()
			{
				new SkillEntry { SkillId = SkillId.HLIF_HEAL, MaxLevel = 5, RequiredHomunLevel = 1 },
				new SkillEntry { SkillId = SkillId.HLIF_AVOID, MaxLevel = 5, RequiredHomunLevel = 9, Prerequisite = SkillId.HLIF_HEAL, PrerequisiteLevel = 3 },
				new SkillEntry { SkillId = SkillId.HLIF_BRAIN, MaxLevel = 5, RequiredHomunLevel = 16, Prerequisite = SkillId.HLIF_HEAL, PrerequisiteLevel = 5 },
				new SkillEntry { SkillId = SkillId.HLIF_CHANGE, MaxLevel = 5, RequiredHomunLevel = 24, Prerequisite = SkillId.HLIF_BRAIN, PrerequisiteLevel = 3 },
			},
			[HomunculusType.Amistr] = new()
			{
				new SkillEntry { SkillId = SkillId.HAMI_CASTLE, MaxLevel = 5, RequiredHomunLevel = 1 },
				new SkillEntry { SkillId = SkillId.HAMI_DEFENCE, MaxLevel = 5, RequiredHomunLevel = 9, Prerequisite = SkillId.HAMI_CASTLE, PrerequisiteLevel = 3 },
				new SkillEntry { SkillId = SkillId.HAMI_SKIN, MaxLevel = 5, RequiredHomunLevel = 16, Prerequisite = SkillId.HAMI_DEFENCE, PrerequisiteLevel = 3 },
				new SkillEntry { SkillId = SkillId.HAMI_BLOODLUST, MaxLevel = 5, RequiredHomunLevel = 24, Prerequisite = SkillId.HAMI_SKIN, PrerequisiteLevel = 3 },
			},
			[HomunculusType.Filir] = new()
			{
				new SkillEntry { SkillId = SkillId.HFLI_MOON, MaxLevel = 5, RequiredHomunLevel = 1 },
				new SkillEntry { SkillId = SkillId.HFLI_FLEET, MaxLevel = 5, RequiredHomunLevel = 9, Prerequisite = SkillId.HFLI_MOON, PrerequisiteLevel = 3 },
				new SkillEntry { SkillId = SkillId.HFLI_SPEED, MaxLevel = 5, RequiredHomunLevel = 16, Prerequisite = SkillId.HFLI_FLEET, PrerequisiteLevel = 3 },
				new SkillEntry { SkillId = SkillId.HFLI_SBR44, MaxLevel = 1, RequiredHomunLevel = 24, Prerequisite = SkillId.HFLI_SPEED, PrerequisiteLevel = 3 },
			},
			[HomunculusType.Vanilmirth] = new()
			{
				new SkillEntry { SkillId = SkillId.HVAN_CAPRICE, MaxLevel = 5, RequiredHomunLevel = 1 },
				new SkillEntry { SkillId = SkillId.HVAN_CHAOTIC, MaxLevel = 5, RequiredHomunLevel = 9, Prerequisite = SkillId.HVAN_CAPRICE, PrerequisiteLevel = 3 },
				new SkillEntry { SkillId = SkillId.HVAN_INSTRUCT, MaxLevel = 5, RequiredHomunLevel = 16, Prerequisite = SkillId.HVAN_CHAOTIC, PrerequisiteLevel = 3 },
				new SkillEntry { SkillId = SkillId.HVAN_EXPLOSION, MaxLevel = 1, RequiredHomunLevel = 24, Prerequisite = SkillId.HVAN_INSTRUCT, PrerequisiteLevel = 3 },
			},
		};

		public static GrowthCurve GetGrowth(HomunculusType type)
			=> _growth.TryGetValue(type, out var g) ? g : new GrowthCurve { HpPerLevel = 20, AtkPerLevel = 1, DefPerLevel = 1 };

		public static FoodEntry GetFood(HomunculusType type, int itemId)
		{
			var family = ResolveFamily(type);
			if (!_foods.TryGetValue(family, out var list)) return null;
			foreach (var entry in list)
				if (entry.ItemId == itemId) return entry;
			return null;
		}

		/// <summary>
		/// All food entries this homunculus type accepts, in the order
		/// they were declared. Used by the feed path to pick whichever
		/// food the player happens to be carrying. Evolved and S-class
		/// forms reuse their base family's food list — eAthena handles
		/// it the same way.
		/// </summary>
		public static IReadOnlyList<FoodEntry> GetFoods(HomunculusType type)
			=> _foods.TryGetValue(ResolveFamily(type), out var list) ? list : new List<FoodEntry>();

		/// <summary>
		/// Per-form learnable skill list. Evolved / S-class forms
		/// inherit the base family's tree (caller-side max-level
		/// bumps for evolved tiers are handled in
		/// <see cref="HomunculusComponent.TryLearn"/>).
		/// </summary>
		public static IReadOnlyList<SkillEntry> GetSkills(HomunculusType type)
			=> _skills.TryGetValue(ResolveFamily(type), out var list) ? list : new List<SkillEntry>();

		private static HomunculusType ResolveFamily(HomunculusType type)
			=> type.GetBaseFamily() is var family && family != HomunculusType.None ? family : type;

		public static SkillEntry FindSkill(HomunculusType type, SkillId skillId)
		{
			foreach (var entry in GetSkills(type))
				if (entry.SkillId == skillId) return entry;
			return null;
		}

		/// <summary>
		/// Max-level cap bonus for evolved (+5) and S-class (+10)
		/// forms over the base entry's <see cref="SkillEntry.MaxLevel"/>.
		/// Mirrors rA's skill-tree bump on evolution: a Lif can take
		/// HLIF_HEAL to 5; Lif2 to 10; Eira to 10 as well.
		/// </summary>
		public static int GetMaxLevelBonus(HomunculusType type)
		{
			if (type.IsSClass()) return 10;
			if (type.IsEvolved()) return 5;
			return 0;
		}
	}
}
