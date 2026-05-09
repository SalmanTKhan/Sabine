#pragma warning disable IDE0009

using System;
using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// Pet AI: stays close to its owner, optionally helps the owner in
	/// combat once intimacy crosses the pet definition's
	/// MinFriendlyIntimacy threshold. Mirrors the HomunculusAi shape but
	/// with the eAthena pet_db <c>attack_rate</c> /
	/// <c>defence_attack_rate</c> rolls gating combat: even when the pet
	/// is loyal, it engages probabilistically per AI tick rather than
	/// auto-locking on every nearby enemy.
	/// </summary>
	[Ai("Pet")]
	public class PetAi : ReactiveAi
	{
		private const int FollowDistance = 3;
		private const int AssistScanRange = 6;

		protected override void Init()
		{
			base.Init();
			During("Idle", FollowOwner);
			During("Idle", PickTarget);
			During("Combat", LeashCheck);
		}

		private PlayerCharacter Owner => (this.Character as Pet)?.Owner;

		private void FollowOwner(CallbackState state)
		{
			if (state.Handled) return;
			var owner = this.Owner;
			if (owner == null) return;
			if (owner.Map != this.Character.Map) return;
			if (this.Character.Position.InRange(owner.Position, FollowDistance)) return;

			this.Character.Controller.MoveTo(owner.Position);
			state.Handled = true;
		}

		private void PickTarget(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var pet = this.Character as Pet;
			if (pet == null || !pet.IsCombatReady) return;

			var owner = this.Owner;
			if (owner == null || owner.IsDead) return;

			var entry = Sabine.Shared.Game.CurrentData?.Pets?.Entries;
			if (entry == null || !entry.TryGetValue(pet.State.MobId, out var data)) return;

			var rnd = RandomProvider.Get();

			// Priority 1: defence_attack — retaliate against whoever
			// just hit the owner.
			if (owner.AttackerHandleTest != 0
				&& this.Character.Map.TryGetCharacter(owner.AttackerHandleTest, out var hitter)
				&& !hitter.IsDead
				&& this.Character.IsHostileTo(hitter)
				&& rnd.Next(10000) < data.DefenseAttackRate)
			{
				StartRoutine("Combat", Combat(hitter.Handle));
				state.Handled = true;
				return;
			}

			// Priority 2: attack_rate — assist on whatever the owner is
			// targeting. Same hostility filter as PickTarget on the
			// Homunculus AI — pet won't friendly-fire other players.
			if (owner.TargetHandle != 0
				&& this.Character.Map.TryGetCharacter(owner.TargetHandle, out var ownerTarget)
				&& !ownerTarget.IsDead
				&& this.Character.IsHostileTo(ownerTarget)
				&& rnd.Next(10000) < data.AttackRate)
			{
				StartRoutine("Combat", Combat(ownerTarget.Handle));
				state.Handled = true;
				return;
			}
		}

		private void LeashCheck(CallbackState state)
		{
			if (state.Handled) return;
			if (_targetCharacterHandle == 0) return;

			var owner = this.Owner;
			if (owner == null) return;

			// If the target wandered out of the assist scan radius from
			// the owner, drop combat and return to follow.
			if (!this.Character.Map.TryGetCharacter(_targetCharacterHandle, out var target)
				|| target.IsDead
				|| !owner.Position.InRange(target.Position, AssistScanRange))
			{
				_targetCharacterHandle = 0;
				StartRoutine("Idle", Idle());
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
