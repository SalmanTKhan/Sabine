using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sabine.Shared.Skills.Refining;
using Yggdrasil.Data.JSON;
using Yggdrasil.Extensions;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// Refine equipment group: <c>Armor</c> for the chest piece slot,
	/// <c>Weapon</c> for any weapon. Mirrors eAthena's row groups in
	/// <c>db/refine_db.txt</c>.
	/// </summary>
	public enum RefineGroup
	{
		Armor = 0,
		Weapon = 1,
	}

	/// <summary>
	/// Cost type for a single refine attempt. Mirrors rAthena's
	/// <c>refine.yml</c> Type field. Classic data only uses
	/// <see cref="Normal"/>; renewal adds <see cref="HD"/> (which
	/// downgrades on failure instead of breaking) and
	/// <see cref="Enriched"/> (higher base success).
	/// </summary>
	public enum RefineCostType
	{
		Normal = 0,
		HD = 1,
		Enriched = 2,
	}

	/// <summary>
	/// One cost-type option for refining a specific level.
	/// </summary>
	public class RefineChance
	{
		public RefineCostType Type { get; set; } = RefineCostType.Normal;

		/// <summary>Success chance in percent (0..100).</summary>
		public int Rate { get; set; }

		/// <summary>Material item id consumed per attempt.</summary>
		public int MaterialItemId { get; set; }

		/// <summary>Zeny consumed per attempt.</summary>
		public int Price { get; set; }

		/// <summary>
		/// Chance in percent (0..100) that the item breaks on FAILURE.
		/// 0 means failure leaves the item intact.
		/// </summary>
		public int BreakingRate { get; set; }

		/// <summary>
		/// Number of refine levels lost on FAILURE when not breaking.
		/// 0 means the refine just doesn't go through.
		/// </summary>
		public int DowngradeAmount { get; set; }
	}

	/// <summary>
	/// Per-refine-level data: the cumulative bonus at this level and
	/// the cost-type options available for attempting it.
	/// </summary>
	public class RefineLevelData
	{
		/// <summary>Refine level being attempted (e.g. 1 for +0 -&gt; +1).</summary>
		public int Level { get; set; }

		/// <summary>
		/// Cumulative bonus at this refine level. Weapons: ATK points.
		/// Armor: hundredths of a DEF point (matches eAthena/rAthena
		/// storage scale).
		/// </summary>
		public int Bonus { get; set; }

		public List<RefineChance> Chances { get; set; } = new();
	}

	/// <summary>
	/// One row of the refine database, covering one tier within a
	/// group: a single weapon level (1..4) for weapons, a single
	/// armor level (1..4) for armor. Storage is per-refine-level
	/// (<see cref="Levels"/>); the loader normalizes the older flat
	/// format into per-level data so callers see one shape.
	/// </summary>
	public class RefineEntry
	{
		public RefineGroup Group { get; set; }

		/// <summary>
		/// Within-group tier: weapon level (1..4) for weapons, armor
		/// level (1..4) for armor. Always &gt;= 1; the loader
		/// normalizes the legacy <c>weaponLevel: 0</c> armor row to 1.
		/// </summary>
		public int Tier { get; set; }

		/// <summary>
		/// Per-refine-level data, ordered by <see cref="RefineLevelData.Level"/>.
		/// </summary>
		public List<RefineLevelData> Levels { get; set; } = new();

		/// <summary>
		/// Highest refine level with data. Acts as the hard cap.
		/// </summary>
		public int MaxRefineLevel => this.Levels.Count == 0 ? 0 : this.Levels[^1].Level;

		/// <summary>
		/// Returns the cumulative refine bonus at the given refine
		/// level. Weapons add this to ATK directly; armor scales by
		/// 100 for displayed DEF.
		/// </summary>
		public int GetRawBonus(int refineLevel)
		{
			if (refineLevel <= 0 || this.Levels.Count == 0)
				return 0;

			var capped = Math.Min(refineLevel, this.MaxRefineLevel);
			var entry = this.GetLevel(capped);
			return entry?.Bonus ?? 0;
		}

		/// <summary>
		/// Returns the success chance (in percent) for refining from
		/// <paramref name="currentLevel"/> to <c>currentLevel + 1</c>
		/// using <paramref name="costType"/>. Returns 0 when the next
		/// level is past the cap or the cost type isn't supported at
		/// that level.
		/// </summary>
		public int GetSuccessRate(int currentLevel, RefineCostType costType = RefineCostType.Normal)
			=> this.GetChance(currentLevel, costType)?.Rate ?? 0;

		/// <summary>
		/// Looks up the cost-type chance for refining from
		/// <paramref name="currentLevel"/> to <c>currentLevel + 1</c>.
		/// Returns null if the level or cost type is missing.
		/// </summary>
		public RefineChance GetChance(int currentLevel, RefineCostType costType)
		{
			var lvl = this.GetLevel(currentLevel + 1);
			return lvl?.Chances.FirstOrDefault(c => c.Type == costType);
		}

		/// <summary>
		/// Returns the cost types available for the next refine
		/// attempt, in data order. Empty if the next level is past
		/// the cap.
		/// </summary>
		public IReadOnlyList<RefineCostType> GetAvailableCostTypes(int currentLevel)
		{
			var lvl = this.GetLevel(currentLevel + 1);
			if (lvl == null)
				return Array.Empty<RefineCostType>();
			return lvl.Chances.Select(c => c.Type).ToList();
		}

		private RefineLevelData GetLevel(int level)
		{
			// Linear scan; max ~20 entries per row, called once per
			// refine attempt — not worth indexing.
			foreach (var l in this.Levels)
			{
				if (l.Level == level)
					return l;
			}
			return null;
		}
	}

	/// <summary>
	/// Refine database. Indexed by (<see cref="RefineGroup"/>,
	/// weaponLevel) packed into a single int key so the standard
	/// <see cref="DatabaseJsonIndexed{TKey, TValue}"/> machinery applies
	/// without a custom collection.
	/// </summary>
	public class RefineDb : DatabaseJsonIndexed<int, RefineEntry>
	{
		/// <summary>
		/// Looks up the armor row for the given armor level. Falls
		/// back to level 1 when the requested level isn't present;
		/// returns null only if armor is missing entirely.
		/// </summary>
		public RefineEntry GetArmor(int armorLevel = 1)
		{
			var resolved = armorLevel < 1 ? 1 : armorLevel;
			return this.Find(MakeKey(RefineGroup.Armor, resolved))
				?? this.Find(MakeKey(RefineGroup.Armor, 1));
		}

		/// <summary>Looks up the entry for the given weapon level (1..4).</summary>
		public RefineEntry GetWeapon(int weaponLevel)
			=> this.Find(MakeKey(RefineGroup.Weapon, weaponLevel));

		/// <inheritdoc/>
		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("group");

			var group = entry.ReadEnum<RefineGroup>("group");
			var tier = ReadTier(entry, group);

			var data = new RefineEntry
			{
				Group = group,
				Tier = tier,
			};

			if (entry.ContainsKey("levels"))
				ReadExpanded(entry, data);
			else
				ReadCompact(entry, data);

			this.AddOrReplace(MakeKey(data.Group, data.Tier), data);
		}

		// Accepts either "tier", "armorLevel" (armor rows), or
		// "weaponLevel" (weapon rows / legacy classic armor rows).
		// Armor normalizes 0 -> 1 for backwards compat with older
		// data files that wrote weaponLevel: 0 on armor rows.
		private static int ReadTier(JObject entry, RefineGroup group)
		{
			var tier = entry.ReadInt("tier", 0);
			if (tier == 0)
				tier = entry.ReadInt(group == RefineGroup.Armor ? "armorLevel" : "weaponLevel", 0);

			if (group == RefineGroup.Armor && tier <= 0)
				tier = 1;

			return tier;
		}

		// Per-refine-level data with explicit chances. Used by the
		// renewal file; matches rAthena refine.yml shape.
		private static void ReadExpanded(JObject entry, RefineEntry data)
		{
			foreach (var lvlEntry in entry.ForEachObject("levels"))
			{
				lvlEntry.AssertNotMissing("level", "chances");

				var lvl = new RefineLevelData
				{
					Level = lvlEntry.ReadInt("level"),
					Bonus = lvlEntry.ReadInt("bonus", 0),
				};

				foreach (var ch in lvlEntry.ForEachObject("chances"))
				{
					ch.AssertNotMissing("rate");
					lvl.Chances.Add(new RefineChance
					{
						Type = ch.ReadEnum("type", RefineCostType.Normal),
						Rate = ch.ReadInt("rate"),
						MaterialItemId = ch.ReadInt("material", 0),
						Price = ch.ReadInt("price", 0),
						BreakingRate = ch.ReadInt("breakingRate", 0),
						DowngradeAmount = ch.ReadInt("downgradeAmount", 0),
					});
				}

				data.Levels.Add(lvl);
			}

			data.Levels.Sort((a, b) => a.Level.CompareTo(b.Level));
		}

		// Compact eAthena-shaped row: safe/risky bonuses + per-level
		// success-rate array. Expanded into the per-level shape so
		// callers see one model. The classic Hollgrehenn cost
		// (RefineCostTable) is folded in as the synthetic Normal
		// chance for each level.
		private static void ReadCompact(JObject entry, RefineEntry data)
		{
			entry.AssertNotMissing("safeBonus", "riskyBonus", "safeLimit", "successRates");

			var safeBonus = entry.ReadInt("safeBonus");
			var riskyBonus = entry.ReadInt("riskyBonus");
			var safeLimit = entry.ReadInt("safeLimit");
			var rates = entry.ReadArray<int>("successRates");

			// Classic only ships armor lv1 and weapon lv1..4, so the
			// tier maps cleanly onto RefineCostTable's weapon-level
			// argument for weapon rows; armor ignores it.
			var costWeaponLevel = data.Group == RefineGroup.Weapon ? data.Tier : 0;
			var cost = RefineCostTable.Get(data.Group, costWeaponLevel);

			for (var i = 0; i < rates.Length; i++)
			{
				var level = i + 1;
				var bonus = level <= safeLimit
					? level * safeBonus
					: safeLimit * safeBonus + (level - safeLimit) * riskyBonus;

				var chance = new RefineChance
				{
					Type = RefineCostType.Normal,
					Rate = rates[i],
					MaterialItemId = cost.MaterialItemId,
					Price = cost.Zeny,
					// Classic semantics: above the safe limit, failure
					// destroys the item (modeled here as a 100% break).
					BreakingRate = level > safeLimit ? 100 : 0,
					DowngradeAmount = 0,
				};

				data.Levels.Add(new RefineLevelData
				{
					Level = level,
					Bonus = bonus,
					Chances = { chance },
				});
			}
		}

		// Pack (group, weaponLevel) into a stable key. Group occupies
		// the high bits; weaponLevel the low byte. Plenty of room.
		private static int MakeKey(RefineGroup group, int weaponLevel)
			=> ((int)group << 8) | (weaponLevel & 0xff);
	}
}
