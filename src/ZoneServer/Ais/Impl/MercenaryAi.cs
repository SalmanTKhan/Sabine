#pragma warning disable IDE0009

using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// Mercenary AI: stays close to its owner, engages whatever the
	/// owner is currently fighting (or whoever just hit the owner).
	/// Mirrors <see cref="HomunculusAi"/>'s shape but without the
	/// auto-cast arm — mercenary skills (MS_*) are deferred to a
	/// follow-up commit; today the merc is a basic-attack assist.
	/// </summary>
	[Ai("Mercenary")]
	public class MercenaryAi : ReactiveAi
	{
		private const int FollowDistance = 3;
		private const int LeashRange = 14;
		private const int AssistScanRange = 9;

		protected override void Init()
		{
			base.Init();
			During("Idle", PickTarget);
			During("Idle", FollowOwner);
			During("Combat", LeashCheck);
		}

		private PlayerCharacter Owner => (this.Character as Mercenary)?.Owner;

		private void PickTarget(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var owner = this.Owner;
			if (owner == null || owner.IsDead) return;

			// Priority 1: owner's current target.
			if (owner.TargetHandle != 0
				&& this.Character.Map.TryGetCharacter(owner.TargetHandle, out var ownerTarget)
				&& !ownerTarget.IsDead
				&& this.Character.IsHostileTo(ownerTarget))
			{
				StartRoutine("Combat", Combat(ownerTarget.Handle));
				state.Handled = true;
				return;
			}

			// Priority 2: whoever just hit the owner.
			if (owner.AttackerHandleTest != 0
				&& this.Character.Map.TryGetCharacter(owner.AttackerHandleTest, out var hitter)
				&& !hitter.IsDead
				&& this.Character.IsHostileTo(hitter))
			{
				StartRoutine("Combat", Combat(hitter.Handle));
				state.Handled = true;
				return;
			}

			// Priority 3: scan radius for nearby aggressors.
			var nearby = this.Character.Map.GetCharactersInRange(this.Character.Position, AssistScanRange)
				.Where(c => this.Character.IsHostileTo(c) && !c.IsDead);
			if (nearby.Any())
			{
				var pick = nearby.OrderBy(c => c.Position.GetDistance(this.Character.Position)).First();
				StartRoutine("Combat", Combat(pick.Handle));
				state.Handled = true;
			}
		}

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

		private void LeashCheck(CallbackState state)
		{
			if (state.Handled) return;
			if (_targetCharacterHandle == 0) return;

			var owner = this.Owner;
			if (owner == null) return;

			if (!this.Character.Map.TryGetCharacter(_targetCharacterHandle, out var target)
				|| target.IsDead
				|| !owner.Position.InRange(target.Position, LeashRange))
			{
				_targetCharacterHandle = 0;
				StartRoutine("Idle", Idle());
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
