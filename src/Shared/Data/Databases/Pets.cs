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

		/// <summary>
		/// Per-10000 chance the pet attacks the owner's current target
		/// each AI tick. Mirrors eAthena's <c>attack_rate</c> column.
		/// Gated on intimacy ≥ <see cref="MinFriendlyIntimacy"/>.
		/// </summary>
		public int AttackRate { get; set; } = 300;

		/// <summary>
		/// Per-10000 chance the pet retaliates against whoever just hit
		/// the owner each AI tick. Mirrors eAthena's
		/// <c>defence_attack_rate</c> column. Same intimacy gate as
		/// <see cref="AttackRate"/>.
		/// </summary>
		public int DefenseAttackRate { get; set; } = 300;

		/// <summary>
		/// Minimum intimacy to enable the pet's combat arm. eAthena's
		/// <c>battle_config.pet_support_min_friendly</c> default is 900
		/// (Loyal). Sabine's catalog defaults to that value; per-pet
		/// overrides land via the data file.
		/// </summary>
		public int MinFriendlyIntimacy { get; set; } = 900;

		/// <summary>
		/// Per-10000 capture success rate (rAthena <c>CaptureRate</c>).
		/// 10000 = guaranteed, 1 = 0.01%. Default 1000 (10%) matches the
		/// rA pre-renewal pet_db floor for unconfigured rows.
		/// </summary>
		public int CaptureRate { get; set; } = 1000;
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
				AttackRate = entry.ReadInt("attackRate", 300),
				DefenseAttackRate = entry.ReadInt("defenseAttackRate", 300),
				MinFriendlyIntimacy = entry.ReadInt("minFriendlyIntimacy", 900),
				CaptureRate = entry.ReadInt("captureRate", 1000),
			};

			this.AddOrReplace(data.MobId, data);
		}
	}
}
