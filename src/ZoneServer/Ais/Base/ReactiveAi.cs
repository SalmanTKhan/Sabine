using System.Collections;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Base
{
	/// <summary>
	/// Base class for AIs that are passive but fight back when attacked.
	/// </summary>
	public abstract class ReactiveAi : MonsterAi
	{
		protected override void Init()
		{
			base.Init();
			During("Idle", CheckAttacks);
		}

		protected override void Start()
		{
			StartRoutine("Idle", Idle());
		}

		/// <summary>
		/// The main combat routine. The monster will hunt down its target.
		/// </summary>
		/// <param name="handle">The handle of the target character.</param>
		protected virtual IEnumerable Combat(int handle)
		{
			_targetCharacterHandle = handle;
			yield return HuntDown(handle);
			_targetCharacterHandle = 0;
			Character.AttackerHandleTest = 0;

			StartRoutine("Idle", Idle());
		}

		/// <summary>
		/// The main idle routine. The monster will wait and then wander.
		/// </summary>
		protected virtual IEnumerable Idle()
		{
			while (true)
			{
				yield return Wait(3000, 10000);
				yield return Wander(5);
			}
		}

		/// <summary>
		/// Checks if the monster has been attacked and starts combat if so.
		/// </summary>
		protected virtual void CheckAttacks(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			if (Character.AttackerHandleTest != 0)
			{
				StartRoutine("Combat", Combat(Character.AttackerHandleTest));
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
