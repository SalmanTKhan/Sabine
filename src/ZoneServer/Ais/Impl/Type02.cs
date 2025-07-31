using System.Collections;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 02: Passive Looter
	/// </summary>
	/// <remarks>
	/// Aegis: 02
	/// Athena: 0x0083 (MD_CANMOVE|MD_LOOTER|MD_CANATTACK)
	/// </remarks>
	[Ai("Type02")]
	public class Type02 : ReactiveAi
	{
		private int _targetItemHandle;

		protected override void Init()
		{
			base.Init(); // Hooks CheckAttacks
			During("Idle", CheckNearbyItems);
			During("PickUpItem", CheckTargetItem);
			During("PickUpItem", base.CheckAttacks); // Can be attacked while looting
		}

		private IEnumerable PickUpItem(int handle, Position pos)
		{
			_targetItemHandle = handle;

			if (Chance(15))
				yield return Emotion(EmotionId.MusicNote);

			yield return MoveTo(pos);
			yield return PickUp(handle);

			StartRoutine("Idle", Idle());
		}

		private void CheckNearbyItems(CallbackState state)
		{
			if (state.Handled) return;

			if (TryFindNearbyItem(out var handle, out var pos))
			{
				StartRoutine("PickUpItem", PickUpItem(handle, pos));
				state.Handled = true;
			}
		}

		private void CheckTargetItem(CallbackState state)
		{
			if (state.Handled) return;

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
