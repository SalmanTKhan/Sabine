using System;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	public enum PetState
	{
		/// <summary>No pet contract on this character.</summary>
		None = 0,
		/// <summary>Pet exists in egg form (item in inventory).</summary>
		Egg = 1,
		/// <summary>Pet is summoned and following the owner.</summary>
		Active = 2,
		/// <summary>Pet was unsummoned back into its egg item.</summary>
		Despawned = 3,
	}

	/// <summary>
	/// Per-player pet state. Mirrors the shape of
	/// <see cref="HomunculusComponent"/>: tracks bookkeeping (mob id,
	/// state, intimacy, hunger, name, equipped accessory) without
	/// spawning a tangible map entity in this commit. Hatch / summon /
	/// feed handlers in follow-up commits mutate this state; the live
	/// companion entity slots into <see cref="Entity"/> when it lands.
	///
	/// Persistence rides on character vars (same shape as Homun) so no
	/// schema migration is needed for v1.
	/// </summary>
	public class PetComponent : ICharacterComponent
	{
		public Character Character { get; }

		public int MobId { get; private set; }
		public PetState State { get; private set; } = PetState.None;
		public string Name { get; private set; } = "";
		public int Intimacy { get; private set; }
		public int Hunger { get; private set; } = 50;
		public int EquippedAccessoryItemId { get; private set; }

		/// <summary>True when a contract exists at all (Egg, Active, or Despawned).</summary>
		public bool HasContract => this.State != PetState.None;
		public bool IsActive => this.State == PetState.Active;

		public PetComponent(Character character)
		{
			this.Character = character;
		}

		/// <summary>Live entity reference while the pet is summoned; null otherwise.</summary>
		public Sabine.Zone.World.Actors.Monster Entity { get; internal set; }

		// eAthena pet hunger: drops 1 every HungryDelay seconds
		// (pet_db column 9, typically 60s). Intimacy bleeds when the
		// pet is starving and recovers slowly when it's well fed.
		private TimeSpan _hungerAccumulator;

		public void Update(TimeSpan elapsed)
		{
			if (this.State != PetState.Active) return;

			var entry = PetTypeData.GetByMobId(this.MobId);
			if (entry == null) return;

			_hungerAccumulator += elapsed;
			var interval = TimeSpan.FromSeconds(entry.HungerIntervalSec);
			while (_hungerAccumulator >= interval)
			{
				_hungerAccumulator -= interval;
				this.TickHunger(entry);
				if (this.State == PetState.None) return;
			}
		}

		private void TickHunger(Sabine.Shared.Data.Databases.PetData entry)
		{
			this.Hunger = Math.Max(0, this.Hunger - 1);

			if (this.Hunger == 0)
			{
				this.Intimacy = Math.Max(0, this.Intimacy - entry.DeathIntimacyLoss);
				if (this.Intimacy == 0)
				{
					// Bond broken — pet runs away. Despawn the entity if
					// present and clear the contract; the egg is NOT
					// refunded (eAthena pet_run behaviour).
					this.Entity?.Map?.RemoveNpc(this.Entity);
					this.Entity = null;
					this.BreakContract();
				}
			}
		}

		/// <summary>
		/// Feeds the pet with one unit of its food item. Returns true on
		/// success; false when there's no contract or the pet is already
		/// at full hunger. The intimacy delta depends on current hunger
		/// at the time of feeding — the eAthena bands collapse to four
		/// zones here:
		/// <list type="bullet">
		/// <item>≤ 25 (starving): +R_Hungry intimacy</item>
		/// <item>26–74 (comfortable): half of R_Hungry</item>
		/// <item>75–89 (stuffed): R_Full (negative for overfeed-punished pets)</item>
		/// <item>≥ 90 (overfed): 2× R_Full</item>
		/// </list>
		/// </summary>
		public bool Feed()
		{
			if (this.State != PetState.Active) return false;

			var entry = PetTypeData.GetByMobId(this.MobId);
			if (entry == null) return false;

			int delta;
			if (this.Hunger <= 25)        delta = entry.IntimacyHungryFeed;
			else if (this.Hunger <= 74)   delta = entry.IntimacyHungryFeed / 2;
			else if (this.Hunger <= 89)   delta = entry.IntimacyFullFeed;
			else                          delta = entry.IntimacyFullFeed * 2;

			this.Hunger = Math.Min(100, this.Hunger + entry.FoodFullness);
			this.Intimacy = Math.Max(0, Math.Min(1000, this.Intimacy + delta));
			return true;
		}

		/// <summary>
		/// Initializes a fresh contract from a pet definition. Called
		/// by the hatch handler after the egg item is consumed.
		/// </summary>
		public void Adopt(Sabine.Shared.Data.Databases.PetData entry, string name)
		{
			this.MobId = entry.MobId;
			this.Name = string.IsNullOrEmpty(name) ? entry.Name : name;
			this.Intimacy = entry.InitialIntimacy;
			this.Hunger = 50;
			this.EquippedAccessoryItemId = 0;
			this.State = PetState.Egg;
		}

		internal void MarkActive() => this.State = PetState.Active;
		public void MarkDespawned() => this.State = PetState.Despawned;

		/// <summary>GM / debug setter; the feed flow uses this through the
		/// scoring path in the hunger-tick commit.</summary>
		public void SetIntimacy(int value) => this.Intimacy = Math.Max(0, Math.Min(1000, value));

		/// <summary>GM / debug setter; the feed flow uses this through
		/// the food-item path in the hunger-tick commit.</summary>
		public void SetHunger(int value) => this.Hunger = Math.Max(0, Math.Min(100, value));

		/// <summary>
		/// Drops the contract entirely (pet ran away from low intimacy
		/// or the player abandoned it). Mirrors Homunculus's bond-broken
		/// path; called from the hunger tick once it lands.
		/// </summary>
		internal void BreakContract()
		{
			this.MobId = 0;
			this.Name = "";
			this.Intimacy = 0;
			this.Hunger = 0;
			this.EquippedAccessoryItemId = 0;
			this.State = PetState.None;
		}

		// --- Persistence ---------------------------------------------------
		// Same Vars-keyed approach as Homun. Active state is downgraded to
		// Despawned on save — the player has to re-summon on re-login.

		private const string KeyMobId = "pet.mobId";
		private const string KeyState = "pet.state";
		private const string KeyName = "pet.name";
		private const string KeyIntimacy = "pet.intimacy";
		private const string KeyHunger = "pet.hunger";
		private const string KeyAccessory = "pet.accessory";

		public void SaveTo(Sabine.Shared.Util.Variables vars)
		{
			vars.Set(KeyMobId, this.MobId);

			if (this.State == PetState.None)
				return;

			var stateToSave = this.State == PetState.Active
				? PetState.Despawned
				: this.State;
			vars.Set(KeyState, (int)stateToSave);
			vars.Set(KeyName, this.Name);
			vars.Set(KeyIntimacy, this.Intimacy);
			vars.Set(KeyHunger, this.Hunger);
			vars.Set(KeyAccessory, this.EquippedAccessoryItemId);
		}

		public void LoadFrom(Sabine.Shared.Util.Variables vars)
		{
			var mobId = vars.GetInt(KeyMobId, 0);
			if (mobId == 0) return;

			var entry = PetTypeData.GetByMobId(mobId);
			if (entry == null) return;

			this.MobId = mobId;
			this.State = (PetState)vars.GetInt(KeyState, (int)PetState.Despawned);
			this.Name = vars.GetString(KeyName, entry.Name);
			this.Intimacy = vars.GetInt(KeyIntimacy, entry.InitialIntimacy);
			this.Hunger = vars.GetInt(KeyHunger, 50);
			this.EquippedAccessoryItemId = vars.GetInt(KeyAccessory, 0);
		}
	}
}
