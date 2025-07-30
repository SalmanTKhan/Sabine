using System.Collections;
using System.Linq;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 04: Aggressive
	/// </summary>
	/// <remarks>
	/// Aegis: 04
	/// Athena: 0x0281 (MD_CANMOVE|MD_CANATTACK|MD_AGGRESSIVE)
	/// Behavior: Attacks players on sight. Will also fight back if attacked.
	/// Typically has a shorter chase range.
	/// </remarks>
	[Ai("Type04")]
	public class Type04 : MonsterAi
	{
		private int _targetCharacterHandle;

		protected override void Init()
		{
			During("Idle", CheckAttacks);
			During("Idle", CheckForTargets);
		}

		protected override void Start()
		{
			StartRoutine("Idle", Idle());
		}

		private IEnumerable Idle()
		{
			while (true)
			{
				yield return Wait(3000, 10000);
				yield return Wander(5);
			}
		}

		private IEnumerable Combat(int handle)
		{
			_targetCharacterHandle = handle;
			yield return HuntDown(handle);
			_targetCharacterHandle = 0;
			Character.AttackerHandleTest = 0;

			StartRoutine("Idle", Idle());
		}

		private void CheckAttacks(CallbackState state)
		{
			if (_targetCharacterHandle != 0) return;

			if (Character.AttackerHandleTest != 0)
			{
				_targetCharacterHandle = Character.AttackerHandleTest;
				StartRoutine("Combat", Combat(_targetCharacterHandle));
				state.Handled = true;
			}
		}

		private void CheckForTargets(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var chaseRange = (Character as Monster)?.Data.ChaseRange ?? 12;

			var players = Character.Map.GetPlayers(p =>
				!p.IsDead &&
				p.Position.InRange(Character.Position, chaseRange));

			if (players.Any())
			{
				var target = players.OrderBy(p => p.Position.GetDistance(Character.Position)).First();
				_targetCharacterHandle = target.Handle;
				StartRoutine("Combat", Combat(_targetCharacterHandle));
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
