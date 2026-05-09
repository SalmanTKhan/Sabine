using System;
using Sabine.Shared.Data.Databases;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	public enum MercenaryState
	{
		None = 0,
		Active = 1,
		Despawned = 2,
	}

	/// <summary>
	/// Per-player mercenary state. Mirrors <see cref="HomunculusComponent"/>
	/// shape: tracks bookkeeping (mob id, faction, contract-time-left,
	/// faith) and exposes a live entity reference while summoned. eAthena
	/// keeps four faith counters per character (one per faction); Sabine
	/// stores them as a small int dictionary on the component.
	///
	/// Per-faction faith counters survive across contract ends — they're
	/// the player's reputation with each mercenary guild and cap at
	/// ~10000 in eAthena. Sabine matches that cap; the lookup is
	/// keyed by <see cref="MercenaryFaction"/>.
	/// </summary>
	public class MercenaryComponent : ICharacterComponent
	{
		public Character Character { get; }

		public int MobId { get; private set; }
		public MercenaryFaction Faction { get; private set; }
		public MercenaryState State { get; private set; } = MercenaryState.None;

		/// <summary>Seconds remaining on the active contract. Decremented by the heartbeat tick once the entity is up.</summary>
		public int ContractSecondsRemaining { get; internal set; }

		/// <summary>Per-faction faith. Dictionary so we don't waste four ints when most players never hire.</summary>
		private readonly System.Collections.Generic.Dictionary<MercenaryFaction, int> _faith = new();

		public bool HasContract => this.State != MercenaryState.None;
		public bool IsActive => this.State == MercenaryState.Active;

		public MercenaryComponent(Character character)
		{
			this.Character = character;
		}

		/// <summary>Live entity while the merc is summoned; null otherwise.</summary>
		public Sabine.Zone.World.Actors.Mercenary Entity { get; internal set; }

		// ---------------------------------------------------------------
		// Faith API
		// ---------------------------------------------------------------

		private const int FaithCap = 10000;

		public int GetFaith(MercenaryFaction faction)
			=> _faith.TryGetValue(faction, out var v) ? v : 0;

		public void AddFaith(MercenaryFaction faction, int delta)
		{
			var current = this.GetFaith(faction);
			_faith[faction] = Math.Max(0, Math.Min(FaithCap, current + delta));
		}

		// ---------------------------------------------------------------
		// Contract lifecycle (called by MercenaryService)
		// ---------------------------------------------------------------

		internal void StartContract(MercenaryData data)
		{
			this.MobId = data.MobId;
			this.Faction = data.Faction;
			this.ContractSecondsRemaining = data.ContractSeconds;
			this.State = MercenaryState.Active;
		}

		internal void EndContract()
		{
			this.MobId = 0;
			this.Faction = MercenaryFaction.None;
			this.ContractSecondsRemaining = 0;
			this.State = MercenaryState.None;
		}

		internal void MarkDespawned() => this.State = MercenaryState.Despawned;

		// ---------------------------------------------------------------
		// Heartbeat
		// ---------------------------------------------------------------

		private TimeSpan _tickAccumulator;

		public void Update(TimeSpan elapsed)
		{
			if (this.State != MercenaryState.Active) return;

			_tickAccumulator += elapsed;
			while (_tickAccumulator >= TimeSpan.FromSeconds(1))
			{
				_tickAccumulator -= TimeSpan.FromSeconds(1);
				this.ContractSecondsRemaining = Math.Max(0, this.ContractSecondsRemaining - 1);

				// Expiry dispatch — the service drops the entity, awards
				// the completion faith, and runs any post-contract hooks.
				if (this.ContractSecondsRemaining == 0
					&& this.Character is PlayerCharacter owner)
				{
					Sabine.Zone.Skills.Mercenaries.MercenaryService.OnContractExpired(owner);
					return;
				}
			}
		}

		// ---------------------------------------------------------------
		// Persistence (vars-backed; matches the Pet / Homunculus shape)
		// ---------------------------------------------------------------

		private const string KeyMobId = "merc.mobId";
		private const string KeyState = "merc.state";
		private const string KeyFaction = "merc.faction";
		private const string KeySecondsRemaining = "merc.seconds";
		private const string KeyFaithPrefix = "merc.faith.";

		public void SaveTo(Sabine.Shared.Util.Variables vars)
		{
			vars.Set(KeyMobId, this.MobId);

			// Faith persists across contract ends; serialise per faction.
			foreach (var kvp in _faith)
				vars.Set(KeyFaithPrefix + (int)kvp.Key, kvp.Value);

			if (this.State == MercenaryState.None) return;

			// Active contracts downgrade to Despawned on save — the
			// merc has to be re-summoned after re-login. Contract time
			// is preserved so the rental clock keeps ticking.
			var stateToSave = this.State == MercenaryState.Active
				? MercenaryState.Despawned
				: this.State;
			vars.Set(KeyState, (int)stateToSave);
			vars.Set(KeyFaction, (int)this.Faction);
			vars.Set(KeySecondsRemaining, this.ContractSecondsRemaining);
		}

		public void LoadFrom(Sabine.Shared.Util.Variables vars)
		{
			// Faith counters first — they exist independent of an active contract.
			for (var i = 1; i <= 4; i++)
			{
				var key = KeyFaithPrefix + i;
				var v = vars.GetInt(key, 0);
				if (v > 0)
					_faith[(MercenaryFaction)i] = v;
			}

			var mobId = vars.GetInt(KeyMobId, 0);
			if (mobId == 0) return;

			this.MobId = mobId;
			this.State = (MercenaryState)vars.GetInt(KeyState, (int)MercenaryState.Despawned);
			this.Faction = (MercenaryFaction)vars.GetInt(KeyFaction, 0);
			this.ContractSecondsRemaining = vars.GetInt(KeySecondsRemaining, 0);
		}
	}
}
