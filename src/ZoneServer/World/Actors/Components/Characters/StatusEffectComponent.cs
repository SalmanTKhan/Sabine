using System;
using System.Collections.Generic;
using Sabine.Shared.Const;
using Sabine.Zone.Skills.StatusEffects;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// A component that manages a character's active status effects.
	/// </summary>
	public class StatusEffectComponent : ICharacterComponent
	{
		private readonly Dictionary<StatusId, StatusEffect> _effects = new();
		private readonly object _syncLock = new();

		public Character Character { get; }

		public StatusEffectComponent(Character character)
		{
			this.Character = character;
		}

		/// <summary>
		/// Returns true if the character has the given status active.
		/// </summary>
		public bool Has(StatusId id)
		{
			lock (_syncLock)
				return _effects.ContainsKey(id);
		}

		/// <summary>
		/// Returns the active effect for the given id, or null.
		/// </summary>
		public StatusEffect Get(StatusId id)
		{
			lock (_syncLock)
				return _effects.TryGetValue(id, out var effect) ? effect : null;
		}

		/// <summary>
		/// Tries to get the active effect for the given id.
		/// </summary>
		public bool TryGet(StatusId id, out StatusEffect effect)
		{
			lock (_syncLock)
				return _effects.TryGetValue(id, out effect);
		}

		/// <summary>
		/// Applies a status effect to the character. If an effect with the
		/// same id is already active, the existing effect is ended (its
		/// stat changes are reverted) and the new one is started fresh.
		/// </summary>
		public void Start(StatusId id, int level, TimeSpan duration, Character caster, int val1 = 0, int val2 = 0, int val3 = 0, int val4 = 0)
		{
			var handler = StatusEffectHandlerManager.GetHandler(id);
			if (handler == null)
				return;

			StatusEffect previous;
			lock (_syncLock)
				_effects.TryGetValue(id, out previous);

			// End the existing instance outside the lock so that handler
			// callbacks (which may call Parameters.Modify and ultimately
			// send packets) don't run while we hold the components lock.
			if (previous != null)
				handler.OnEnd(this.Character, previous);

			var effect = new StatusEffect(id, level, duration, this.Character, caster, val1, val2, val3, val4);

			lock (_syncLock)
				_effects[id] = effect;

			handler.OnStart(this.Character, effect);

			BroadcastIcon(id, true);
		}

		/// <summary>
		/// Ends the given status effect on the character if active.
		/// </summary>
		public void Stop(StatusId id)
		{
			StatusEffect effect;
			lock (_syncLock)
			{
				if (!_effects.TryGetValue(id, out effect))
					return;
				_effects.Remove(id);
			}

			var handler = StatusEffectHandlerManager.GetHandler(id);
			handler?.OnEnd(this.Character, effect);

			BroadcastIcon(id, false);
		}

		/// <summary>
		/// Ends all active status effects.
		/// </summary>
		public void StopAll()
		{
			List<StatusEffect> snapshot;
			lock (_syncLock)
			{
				snapshot = new List<StatusEffect>(_effects.Values);
				_effects.Clear();
			}

			foreach (var effect in snapshot)
			{
				var handler = StatusEffectHandlerManager.GetHandler(effect.Id);
				handler?.OnEnd(this.Character, effect);
				BroadcastIcon(effect.Id, false);
			}
		}

		/// <summary>
		/// Ticks active effects and removes expired ones.
		/// </summary>
		public void Update(TimeSpan elapsed)
		{
			List<StatusEffect> snapshot;
			lock (_syncLock)
			{
				if (_effects.Count == 0)
					return;

				snapshot = new List<StatusEffect>(_effects.Values);
			}

			List<StatusEffect> expired = null;
			foreach (var effect in snapshot)
			{
				effect.Tick(elapsed);

				var handler = StatusEffectHandlerManager.GetHandler(effect.Id);
				handler?.OnTick(this.Character, effect, elapsed);

				if (effect.IsExpired)
				{
					expired ??= new List<StatusEffect>();
					expired.Add(effect);
				}
			}

			if (expired == null)
				return;

			foreach (var effect in expired)
			{
				lock (_syncLock)
					_effects.Remove(effect.Id);

				var handler = StatusEffectHandlerManager.GetHandler(effect.Id);
				handler?.OnEnd(this.Character, effect);

				BroadcastIcon(effect.Id, false);
			}
		}

		/// <summary>
		/// Sends the per-status icon update to the owning player. Skips
		/// non-player characters and statuses without a registered icon.
		/// </summary>
		private void BroadcastIcon(StatusId id, bool active)
		{
			if (this.Character is not PlayerCharacter pc)
				return;

			var icon = Sabine.Zone.Battle.StatusIcons.GetIcon(id);
			if (icon == Sabine.Zone.Battle.StatusIcons.None)
				return;

			Sabine.Zone.Network.Send.ZC_MSG_STATE_CHANGE(pc, icon, active);
		}
	}
}
