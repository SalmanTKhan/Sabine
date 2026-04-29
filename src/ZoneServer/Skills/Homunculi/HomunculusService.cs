using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;

namespace Sabine.Zone.Skills.Homunculi
{
	/// <summary>
	/// Lifecycle operations for a player's homunculus: summon, rest,
	/// despawn-on-warp. Owns the rules around when an entity exists
	/// on a map vs. when only the persistent state lives on the
	/// player. Handlers call into here rather than mutating the
	/// component directly.
	/// </summary>
	public static class HomunculusService
	{
		// Placeholder mob id while system/data/monsters.txt has no
		// 6001-6004 entries. Spawning a Poring keeps the pipeline
		// observable; data entries for real homun mobs are a follow-up.
		private const int PlaceholderMobId = 1002;

		/// <summary>
		/// Summon the homunculus next to its owner. Returns true if a
		/// fresh entity was placed; false if the player has no
		/// contract, the homunculus is dead, or it was already active.
		/// </summary>
		public static bool Spawn(PlayerCharacter owner)
		{
			var state = owner.Homunculus;
			if (!state.HasContract) return false;
			if (state.IsActive && state.Entity != null) return false;
			if (state.Hp <= 0) return false;

			var mobId = ResolveMobId(state.Type);
			var entity = new Homunculus(owner, state, (IdentityId)mobId);
			entity.AttachAi("Homunculus");

			var pos = owner.Position;
			entity.Warp(owner.Map.Id, pos);

			state.Entity = entity;
			state.MarkActive();
			Sabine.Zone.Network.Send.ZC_PROPERTY_HOMUN(owner, entity);
			return true;
		}

		/// <summary>
		/// Despawn the homunculus and mark it as resting. HP carries
		/// over via the persistent component. Returns one Embryo to
		/// the owner — eAthena AM_REST behavior.
		/// </summary>
		public static void Rest(PlayerCharacter owner)
		{
			var state = owner.Homunculus;
			var entity = state.Entity;

			if (entity != null)
			{
				state.SyncHp(entity.Parameters.Hp);
				entity.Map?.RemoveNpc(entity);
				state.Entity = null;
			}

			if (state.Hp > 0)
			{
				state.MarkResting();
				owner.Inventory.AddItem(new Item(EmbryoItemId, 1));
			}
		}

		private const int EmbryoItemId = 7142;

		/// <summary>
		/// Called when the owner leaves the map (warp), logs out,
		/// or dies. Removes the entity but keeps the contract.
		/// </summary>
		public static void Detach(PlayerCharacter owner)
		{
			var state = owner.Homunculus;
			if (state.Entity == null) return;

			state.SyncHp(state.Entity.Parameters.Hp);
			state.Entity.Map?.RemoveNpc(state.Entity);
			state.Entity = null;
			if (state.Hp > 0) state.MarkResting();
		}

		private static int ResolveMobId(HomunculusType type)
		{
			// Real homunculus mob ids in eAthena classic. If
			// monsters.txt lacks an entry, Monster's constructor will
			// throw — fall through to the placeholder Poring id then.
			var ideal = type switch
			{
				HomunculusType.Lif => 6001,
				HomunculusType.Amistr => 6002,
				HomunculusType.Filir => 6003,
				HomunculusType.Vanilmirth => 6004,
				_ => PlaceholderMobId,
			};

			if (ZoneServer.Instance.Data.Monsters.Contains((IdentityId)ideal))
				return ideal;

			return PlaceholderMobId;
		}
	}
}
