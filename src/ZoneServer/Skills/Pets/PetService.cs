using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;

namespace Sabine.Zone.Skills.Pets
{
	/// <summary>
	/// Lifecycle operations for a player's pet: summon, despawn-back-
	/// to-egg, despawn-on-warp. Mirrors <see cref="Skills.Homunculi.HomunculusService"/>'s
	/// shape so the call sites stay symmetric. Pet eggs round-trip
	/// through the inventory: hatching consumed an egg, despawning
	/// returns one.
	/// </summary>
	public static class PetService
	{
		// Placeholder used when monsters.txt doesn't have an entry for
		// the requested pet's mob id. Same Poring fallback as the homun
		// service so spawning never fails outright while data is partial.
		private const int PlaceholderMobId = 1002;

		/// <summary>
		/// Summons the pet next to its owner. Returns true if a fresh
		/// entity was placed; false when the player has no contract,
		/// the pet is already active, or the owner isn't on a map yet.
		/// </summary>
		public static bool Spawn(PlayerCharacter owner)
		{
			var state = owner.Pet;
			if (!state.HasContract) return false;
			if (state.IsActive && state.Entity != null) return false;
			if (owner.Map == null) return false;

			var mobId = ResolveMobId(state.MobId);
			var entity = new Pet(owner, state, (IdentityId)mobId);
			entity.AttachAi("Pet");

			entity.Warp(owner.Map.Id, owner.Position);

			state.Entity = entity;
			state.MarkActive();
			return true;
		}

		/// <summary>
		/// Despawn the pet back into its egg item. Mirrors eAthena
		/// <c>pet_return_egg</c>: the egg lands back in the owner's
		/// inventory and the pet entity is removed from the map.
		/// Contract persists; the player can re-summon later.
		/// </summary>
		public static void ReturnToEgg(PlayerCharacter owner)
		{
			var state = owner.Pet;
			var entity = state.Entity;

			if (entity != null)
			{
				entity.Map?.RemoveNpc(entity);
				state.Entity = null;
			}

			var entry = PetTypeData.GetByMobId(state.MobId);
			if (entry != null)
				owner.Inventory.AddItem(new Item(entry.EggItemId, 1));

			state.MarkDespawned();
		}

		/// <summary>
		/// Owner left the map / logged out / died. Removes the entity
		/// without returning an egg item so the round-trip stays clean
		/// on re-login (LoadFrom downgrades active state to despawned).
		/// </summary>
		public static void Detach(PlayerCharacter owner)
		{
			var state = owner.Pet;
			if (state.Entity == null) return;

			state.Entity.Map?.RemoveNpc(state.Entity);
			state.Entity = null;
			state.MarkDespawned();
		}

		private static int ResolveMobId(int requested)
		{
			if (requested <= 0) return PlaceholderMobId;
			if (ZoneServer.Instance.Data.Monsters.Contains((IdentityId)requested))
				return requested;
			return PlaceholderMobId;
		}
	}
}
