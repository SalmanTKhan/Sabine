using Sabine.Shared.Const;
using Sabine.Zone.World.Actors.Components.Characters;

namespace Sabine.Zone.World.Actors
{
	/// <summary>
	/// A pet companion. Lives on a map as a Monster-derived entity owned
	/// by a player. Wild monsters won't aggro on it (AggressiveAi scans
	/// for PlayerCharacter only). Combat arm is intimacy-gated by
	/// <see cref="Sabine.Shared.Data.Databases.PetData.MinFriendlyIntimacy"/>;
	/// while below the threshold the pet just follows, once loyal it
	/// engages the owner's combat target.
	/// </summary>
	public class Pet : Monster
	{
		public PlayerCharacter Owner { get; }
		public PetComponent State { get; }

		public Pet(PlayerCharacter owner, PetComponent state, IdentityId identityId)
			: base(identityId)
		{
			this.Owner = owner;
			this.State = state;
			this.Master = owner;
			this.Name = state.Name;
		}

		/// <summary>
		/// True when intimacy meets the pet definition's threshold for
		/// the combat arm. eAthena gates this through
		/// <c>battle_config.pet_support_min_friendly</c>; per-pet
		/// overrides land via the data file.
		/// </summary>
		public bool IsCombatReady
		{
			get
			{
				var entry = Sabine.Shared.Game.CurrentData?.Pets?.Entries;
				if (entry == null) return false;
				if (!entry.TryGetValue(this.State.MobId, out var data)) return false;
				return this.State.Intimacy >= data.MinFriendlyIntimacy;
			}
		}

		internal override bool IsHostileTo(Character target)
		{
			if (target == null || target == this) return false;
			if (target == this.Owner) return false;
			if (target is PlayerCharacter) return false;
			if (target is Pet) return false;

			if (!this.IsCombatReady) return false;

			return target is Monster;
		}
	}
}
