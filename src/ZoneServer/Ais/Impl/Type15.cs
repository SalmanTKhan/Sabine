using System.Collections;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 15: Alchemist Summon
	/// </summary>
	/// <remarks>
	/// Aegis: 15
	/// Behavior: A summoned creature that follows its owner. It will attack any
	/// target that its owner attacks or is attacked by.
	/// </remarks>
	[Ai("Type15")]
	public class Type15 : MonsterAi
	{
		protected int _targetCharacterHandle;

		// Assuming Monster has an OwnerHandle property.
		protected int OwnerHandle => (Character as Monster)?.OwnerHandle ?? 0;

		protected override void Init()
		{
			During("Idle", CheckOwnerStatus);
			During("FollowOwner", CheckOwnerStatus);
		}

		protected override void Start()
		{
			StartRoutine("Idle", Idle());
		}

		private IEnumerable Idle()
		{
			while (true)
				yield return Wait(250);
		}

		private IEnumerable FollowOwner()
		{
			if (OwnerHandle == 0 || !TryGetEntity(OwnerHandle, out var owner))
			{
				StartRoutine("Idle", Idle());
				yield break;
			}

			yield return MoveTo(owner.Position);
			StartRoutine("Idle", Idle());
		}

		private IEnumerable Combat(int handle)
		{
			_targetCharacterHandle = handle;
			yield return HuntDown(handle);
			_targetCharacterHandle = 0;

			StartRoutine("Idle", Idle());
		}

		protected virtual void CheckOwnerStatus(CallbackState state)
		{
			if (OwnerHandle == 0 || !Character.Map.TryGetCharacter(OwnerHandle, out var owner))
			{
				// Owner is gone, what to do? Maybe disappear. For now, just idles.
				return;
			}

			// If in combat, check if target is still valid
			if (_targetCharacterHandle != 0)
			{
				if (!EntityExists(_targetCharacterHandle))
					_targetCharacterHandle = 0;
				else
					return; // Continue combat
			}

			// Check if owner is attacked
			if (owner.AttackerHandleTest != 0)
			{
				StartRoutine("Combat", Combat(owner.AttackerHandleTest));
				state.Handled = true;
				return;
			}

			// Check if owner is attacking something (assuming TargetHandle property)
			if (owner.TargetHandle != 0 && EntityExists(owner.TargetHandle))
			{
				StartRoutine("Combat", Combat(owner.TargetHandle));
				state.Handled = true;
				return;
			}

			// Check distance to owner and follow if too far
			var followDistance = 5;
			if (!owner.Position.InRange(Character.Position, followDistance))
			{
				StartRoutine("FollowOwner", FollowOwner());
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
