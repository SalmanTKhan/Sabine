using System.Collections;
using Sabine.Shared.World;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Base
{
	/// <summary>
	/// Base class for AIs that pick up nearby dropped items while idle.
	/// Mirrors rAthena's MD_LOOTER mode: idle ticks scan visible-range
	/// drops, walk to the closest one, and feed it into the mob's loot
	/// list (so kills drop the picked-up item alongside normal drops).
	/// Looting is interrupted by the inherited CheckAttacks hook the
	/// moment something attacks the mob.
	/// </summary>
	public abstract class LooterAi : ReactiveAi
	{
		private int _targetItemHandle;

		protected override void Init()
		{
			base.Init(); // Hooks CheckAttacks for Idle + Leash.
			During("Idle", CheckNearbyItems);
			During("PickUpItem", CheckTargetItem);
			During("PickUpItem", CheckAttacks); // Looting can be interrupted.
		}

		/// <summary>
		/// Routine that walks to the targeted item and picks it up. The
		/// item handle stays cached so CheckTargetItem can detect another
		/// actor grabbing it first.
		/// </summary>
		protected virtual IEnumerable PickUpItem(int handle, Position pos)
		{
			_targetItemHandle = handle;

			foreach (var step in OnBeforePickUp(handle, pos))
				yield return step;

			yield return MoveTo(pos);
			yield return PickUp(handle);

			_targetItemHandle = 0;
			StartRoutine("Idle", Idle());
		}

		/// <summary>
		/// Hook that runs after a drop is targeted but before the mob
		/// starts walking toward it. Default is a no-op; subclasses can
		/// emit emotes or other reactions here.
		/// </summary>
		protected virtual IEnumerable OnBeforePickUp(int handle, Position pos)
		{
			yield break;
		}

		/// <summary>
		/// Idle hook that switches to PickUpItem when a drop is reachable.
		/// </summary>
		protected void CheckNearbyItems(CallbackState state)
		{
			if (state.Handled)
				return;

			if (TryFindNearbyItem(out var handle, out var pos))
			{
				StartRoutine("PickUpItem", PickUpItem(handle, pos));
				state.Handled = true;
			}
		}

		/// <summary>
		/// PickUpItem hook that aborts back to Idle if the targeted drop
		/// disappeared (someone else grabbed it, or it timed out).
		/// </summary>
		protected void CheckTargetItem(CallbackState state)
		{
			if (state.Handled)
				return;

			if (!EntityExists(_targetItemHandle))
			{
				_targetItemHandle = 0;
				StopMove();
				StartRoutine("Idle", Idle());
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
