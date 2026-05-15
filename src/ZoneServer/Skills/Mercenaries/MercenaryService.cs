using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;

namespace Sabine.Zone.Skills.Mercenaries
{
	/// <summary>
	/// Lifecycle operations for a player's mercenary contract. Mirrors
	/// <see cref="Skills.Homunculi.HomunculusService"/>'s shape: Hire
	/// validates the contract row and starts the clock; Spawn places
	/// the entity (used by Hire and re-summon after re-login); Detach
	/// takes the entity off the map but keeps the contract running;
	/// Dismiss / OnContractExpired / OnMercenaryDeath all end the
	/// contract with the appropriate faith adjustment.
	/// </summary>
	public static class MercenaryService
	{
		// Same Poring fallback as the Homun / Pet services so a missing
		// monsters.txt row doesn't throw at spawn while data is partial.
		private const int PlaceholderMobId = 1002;

		/// <summary>
		/// Hires the given mercenary type. Looks up the contract
		/// metadata in <c>Game.CurrentData.Mercenaries</c>, starts the
		/// contract clock, and spawns the live entity beside the owner.
		/// Refuses when a contract is already active or the mob id
		/// isn't registered.
		/// </summary>
		public static bool Hire(PlayerCharacter owner, int mobId)
		{
			if (owner?.Mercenary == null) return false;
			if (owner.Mercenary.HasContract) return false;
			if (owner.Map == null) return false;

			var data = Sabine.Shared.Game.CurrentData?.Mercenaries?.Entries;
			if (data == null || !data.TryGetValue(mobId, out var entry))
				return false;

			owner.Mercenary.StartContract(entry);
			Spawn(owner);
			return true;
		}

		/// <summary>
		/// Spawns the live entity for the current contract. Used by
		/// <see cref="Hire"/> on initial summon and on re-summon after
		/// a re-login (the contract loaded in Despawned state). Returns
		/// true if a fresh entity was placed.
		/// </summary>
		public static bool Spawn(PlayerCharacter owner)
		{
			if (owner?.Mercenary == null) return false;
			if (!owner.Mercenary.HasContract) return false;
			if (owner.Mercenary.Entity != null) return false;
			if (owner.Map == null) return false;

			var spawnId = ResolveMobId(owner.Mercenary.MobId);
			var entity = new Mercenary(owner, owner.Mercenary, (IdentityId)spawnId);
			entity.AttachAi("Mercenary");

			// Use the persisted player-set name if one exists; the
			// mob-template name (set in the Mercenary ctor) is the
			// fallback for fresh contracts.
			if (!string.IsNullOrEmpty(owner.Mercenary.Name))
				entity.Name = owner.Mercenary.Name;

			entity.Warp(owner.Map.Id, owner.Position);

			owner.Mercenary.Entity = entity;

			// Push the mercenary panel state to the client. Sabine v50000+
			// expects the init + skill list immediately after the entity
			// appears; older profiles don't ship these ops and skip.
			if (Sabine.Shared.Game.Version >= Sabine.Shared.Versions.S2500)
			{
				Sabine.Zone.Network.Send.ZC_MER_INIT(owner, entity);
				Sabine.Zone.Network.Send.ZC_MER_SKILLINFO_LIST(owner, entity);
			}

			return true;
		}

		/// <summary>
		/// Player-initiated dismiss. Awards the completion faith (the
		/// player's choice to release the merc faithfully), drops the
		/// entity, and clears the contract.
		/// </summary>
		public static void Dismiss(PlayerCharacter owner)
		{
			if (owner?.Mercenary == null) return;
			if (!owner.Mercenary.HasContract) return;

			AwardFaith(owner, completion: true);
			Detach(owner);
			owner.Mercenary.EndContract();
		}

		/// <summary>
		/// Time-up dispatcher called from
		/// <see cref="MercenaryComponent.Update"/> when
		/// ContractSecondsRemaining hits zero.
		/// </summary>
		public static void OnContractExpired(PlayerCharacter owner)
		{
			if (owner?.Mercenary == null) return;

			AwardFaith(owner, completion: true);
			Detach(owner);
			owner.Mercenary.EndContract();
			owner.ServerMessage(Sabine.Shared.Util.Localization.Get("Your mercenary contract has expired."));
		}

		/// <summary>
		/// Mercenary death — applies the faith penalty (negative delta)
		/// and clears the contract. Called from the on-death pipeline
		/// when the entity hits 0 HP.
		/// </summary>
		public static void OnMercenaryDeath(PlayerCharacter owner)
		{
			if (owner?.Mercenary == null) return;

			AwardFaith(owner, completion: false);
			Detach(owner);
			owner.Mercenary.EndContract();
			owner.ServerMessage(Sabine.Shared.Util.Localization.Get("Your mercenary has fallen."));
		}

		/// <summary>
		/// Removes the live entity but keeps the contract intact. Used
		/// on warp / logout / death of the owner; the contract clock
		/// keeps running so rentals don't reset on every map change.
		/// Re-summon via <see cref="Spawn"/>.
		/// </summary>
		public static void Detach(PlayerCharacter owner)
		{
			if (owner?.Mercenary?.Entity == null) return;
			owner.Mercenary.Entity.Map?.RemoveNpc(owner.Mercenary.Entity);
			owner.Mercenary.Entity = null;
			owner.Mercenary.MarkDespawned();
		}

		private static void AwardFaith(PlayerCharacter owner, bool completion)
		{
			var data = Sabine.Shared.Game.CurrentData?.Mercenaries?.Entries;
			if (data == null || !data.TryGetValue(owner.Mercenary.MobId, out var entry)) return;

			var delta = completion ? entry.FaithRewardOnComplete : entry.FaithPenaltyOnDeath;
			owner.Mercenary.AddFaith(entry.Faction, delta);
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
