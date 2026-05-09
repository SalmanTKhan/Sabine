using System.Collections.Generic;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// Static eA-style pet definition. Mirrors a row from eAthena's
	/// <c>db/pet_db.txt</c>; only the fields Sabine consumes today are
	/// modelled — taming-rate, performance, attack/retaliate hooks, and
	/// pet/loyal scripts will land alongside the companion entity and
	/// the script bridge in later commits.
	/// </summary>
	public class PetEntry
	{
		/// <summary>Mob class id (also the pet's display sprite).</summary>
		public int MobId { get; set; }

		/// <summary>Display name on hatch (eA <c>JName</c>).</summary>
		public string Name { get; set; }

		/// <summary>Tame item id; consumed on capture (eA <c>LureID</c>).</summary>
		public int LureItemId { get; set; }

		/// <summary>Egg item id; placed in inventory on hatch and removed on summon.</summary>
		public int EggItemId { get; set; }

		/// <summary>Accessory item id; the only item the pet can equip.</summary>
		public int EquipItemId { get; set; }

		/// <summary>Default food item id (eA <c>FoodID</c>).</summary>
		public int FoodItemId { get; set; }

		/// <summary>Hunger restored per food (eA <c>Fullness</c>).</summary>
		public int FoodFullness { get; set; } = 80;

		/// <summary>Seconds between hunger ticks (eA <c>HungryDelay</c>).</summary>
		public int HungerIntervalSec { get; set; } = 60;

		/// <summary>Intimacy delta when fed at hunger ≤ R_Hungry threshold (eA <c>R_Hungry</c>).</summary>
		public int IntimacyHungryFeed { get; set; } = 50;

		/// <summary>Intimacy delta when fed at hunger ≥ R_Full threshold (eA <c>R_Full</c>).</summary>
		public int IntimacyFullFeed { get; set; } = -100;

		/// <summary>Intimacy on hatch (eA <c>Intimate</c>).</summary>
		public int InitialIntimacy { get; set; } = 250;

		/// <summary>Intimacy penalty when the pet runs / dies (eA <c>Die</c>).</summary>
		public int DeathIntimacyLoss { get; set; } = 20;

		/// <summary>Walk speed; alpha mob default 150ms per tile.</summary>
		public int Speed { get; set; } = 150;
	}

	/// <summary>
	/// In-memory pet catalog. Today this is hard-coded with a starter
	/// set so the rest of the pet system (hatch, summon, feed) has
	/// something to lookup against. The full eAthena <c>db/pet_db.txt</c>
	/// will land as a parsed data file in a follow-up; the schema
	/// below already matches the field set so the loader can be a
	/// straightforward txt-to-PetEntry mapper.
	/// </summary>
	public static class PetTypeData
	{
		private static readonly Dictionary<int, PetEntry> _byMobId = new();

		static PetTypeData()
		{
			Register(new PetEntry { MobId = 1002, Name = "Poring",   LureItemId = 619, EggItemId = 9001, EquipItemId = 10013, FoodItemId = 531, FoodFullness = 80, HungerIntervalSec = 60, IntimacyHungryFeed = 50, IntimacyFullFeed = 100, InitialIntimacy = 250, DeathIntimacyLoss = 20, Speed = 150 });
			Register(new PetEntry { MobId = 1113, Name = "Drops",    LureItemId = 620, EggItemId = 9002, EquipItemId = 10013, FoodItemId = 508, FoodFullness = 80, HungerIntervalSec = 60, IntimacyHungryFeed = 40, IntimacyFullFeed = 100, InitialIntimacy = 250, DeathIntimacyLoss = 20, Speed = 150 });
			Register(new PetEntry { MobId = 1031, Name = "Poporing", LureItemId = 621, EggItemId = 9003, EquipItemId = 10013, FoodItemId = 511, FoodFullness = 80, HungerIntervalSec = 60, IntimacyHungryFeed = 30, IntimacyFullFeed = 100, InitialIntimacy = 250, DeathIntimacyLoss = 20, Speed = 150 });
			Register(new PetEntry { MobId = 1063, Name = "Lunatic",  LureItemId = 622, EggItemId = 9004, EquipItemId = 10007, FoodItemId = 534, FoodFullness = 80, HungerIntervalSec = 60, IntimacyHungryFeed = 40, IntimacyFullFeed = 100, InitialIntimacy = 250, DeathIntimacyLoss = 20, Speed = 150 });
			Register(new PetEntry { MobId = 1049, Name = "Picky",    LureItemId = 623, EggItemId = 9005, EquipItemId = 10012, FoodItemId = 507, FoodFullness = 80, HungerIntervalSec = 60, IntimacyHungryFeed = 40, IntimacyFullFeed = 100, InitialIntimacy = 250, DeathIntimacyLoss = 20, Speed = 150 });
			Register(new PetEntry { MobId = 1011, Name = "ChonChon", LureItemId = 624, EggItemId = 9006, EquipItemId = 10002, FoodItemId = 537, FoodFullness = 80, HungerIntervalSec = 60, IntimacyHungryFeed = 30, IntimacyFullFeed = 100, InitialIntimacy = 250, DeathIntimacyLoss = 20, Speed = 150 });
		}

		private static void Register(PetEntry entry) => _byMobId[entry.MobId] = entry;

		public static PetEntry GetByMobId(int mobId)
			=> _byMobId.TryGetValue(mobId, out var e) ? e : null;

		public static PetEntry GetByEggItemId(int eggItemId)
		{
			foreach (var e in _byMobId.Values)
				if (e.EggItemId == eggItemId) return e;
			return null;
		}

		public static PetEntry GetByLureItemId(int lureItemId)
		{
			foreach (var e in _byMobId.Values)
				if (e.LureItemId == lureItemId) return e;
			return null;
		}
	}
}
