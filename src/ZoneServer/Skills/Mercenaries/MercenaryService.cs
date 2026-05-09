using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Mercenaries
{
	/// <summary>
	/// Lifecycle operations for a player's mercenary contract. Mirrors
	/// <see cref="Skills.Homunculi.HomunculusService"/>'s shape:
	/// Summon places the entity from a contract data row, Dismiss /
	/// Detach take it off the map, and OnContractExpired runs the
	/// scheduled time-up flow.
	///
	/// v1 stub — Spawn / Dismiss / Detach land in the entity commit;
	/// today the heartbeat tick on MercenaryComponent expires through
	/// the OnContractExpired hook below so the data path round-trips.
	/// </summary>
	public static class MercenaryService
	{
		/// <summary>
		/// Called by the per-second contract tick on
		/// <see cref="Sabine.Zone.World.Actors.Components.Characters.MercenaryComponent.Update"/>
		/// when the timer hits zero. Awards completion faith, drops the
		/// entity, and clears the contract.
		/// </summary>
		public static void OnContractExpired(PlayerCharacter owner)
		{
			if (owner?.Mercenary == null) return;

			var data = Sabine.Shared.Game.CurrentData?.Mercenaries?.Entries;
			if (data != null && data.TryGetValue(owner.Mercenary.MobId, out var entry))
				owner.Mercenary.AddFaith(entry.Faction, entry.FaithRewardOnComplete);

			Detach(owner);
			owner.Mercenary.EndContract();
			owner.ServerMessage(Sabine.Shared.Util.Localization.Get("Your mercenary contract has expired."));
		}

		/// <summary>
		/// Removes the live entity but keeps the contract intact. Used
		/// on warp / logout / death; the contract clock keeps running
		/// so the rental doesn't reset on every map change.
		/// </summary>
		public static void Detach(PlayerCharacter owner)
		{
			if (owner?.Mercenary?.Entity == null) return;
			owner.Mercenary.Entity.Map?.RemoveNpc(owner.Mercenary.Entity);
			owner.Mercenary.Entity = null;
			owner.Mercenary.MarkDespawned();
		}
	}
}
