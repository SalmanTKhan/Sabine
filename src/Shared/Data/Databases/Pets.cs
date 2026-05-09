using Newtonsoft.Json.Linq;
using Yggdrasil.Data.JSON;
using Yggdrasil.Extensions;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// Static pet definition. Mirrors a row from eAthena's
	/// <c>db/pet_db.txt</c>; only the fields Sabine consumes today are
	/// modelled — taming-rate, performance, attack/retaliate hooks, and
	/// pet/loyal scripts will land alongside the companion-entity combat
	/// arm and the script bridge in later commits.
	/// </summary>
	public class PetData
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
	/// Pet template database. Indexed by mob id so the lookup paths
	/// (<c>GetByMobId</c> on the Zone-side facade) resolve in one
	/// hit. Egg-item-id and lure-item-id reverse-lookups iterate the
	/// table; the catalog is small enough that an inverted index isn't
	/// worth the maintenance burden.
	/// </summary>
	public class PetDb : DatabaseJsonIndexed<int, PetData>
	{
		/// <inheritdoc/>
		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("mobId", "name", "eggItemId", "foodItemId");

			var data = new PetData
			{
				MobId = entry.ReadInt("mobId"),
				Name = entry.ReadString("name"),
				LureItemId = entry.ReadInt("lureItemId", 0),
				EggItemId = entry.ReadInt("eggItemId"),
				EquipItemId = entry.ReadInt("equipItemId", 0),
				FoodItemId = entry.ReadInt("foodItemId"),
				FoodFullness = entry.ReadInt("foodFullness", 80),
				HungerIntervalSec = entry.ReadInt("hungerIntervalSec", 60),
				IntimacyHungryFeed = entry.ReadInt("intimacyHungryFeed", 50),
				IntimacyFullFeed = entry.ReadInt("intimacyFullFeed", -100),
				InitialIntimacy = entry.ReadInt("initialIntimacy", 250),
				DeathIntimacyLoss = entry.ReadInt("deathIntimacyLoss", 20),
				Speed = entry.ReadInt("speed", 150),
			};

			this.AddOrReplace(data.MobId, data);
		}
	}
}
