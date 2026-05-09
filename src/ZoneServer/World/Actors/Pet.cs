using Sabine.Shared.Const;
using Sabine.Zone.World.Actors.Components.Characters;

namespace Sabine.Zone.World.Actors
{
	/// <summary>
	/// A pet companion. Lives on a map as a Monster-derived entity owned
	/// by a player. Wild monsters won't aggro on it (AggressiveAi scans
	/// for PlayerCharacter only); the pet itself is non-hostile to
	/// everyone — pets in classic eAthena have an attack rate but
	/// Sabine's pet AI is follow-only today, so the hostility check is
	/// just a hard "no" until pet combat lands.
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

		internal override bool IsHostileTo(Character target)
		{
			// Pets never initiate combat. The eAthena pet attack/retaliate
			// rates (pet_db columns 18/19) are deferred — when they land
			// they'll gate on intimacy ≥ pet_support_min_friendly and
			// re-enable hostility against the owner's combat target.
			return false;
		}
	}
}
