using System.Collections;
using System.Linq;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 13: Aggressive Social
	/// </summary>
	/// <remarks>
	/// Aegis: 13
	/// Behavior: Combination of Type 04 (Aggressive) and Type 03 (Social).
	/// It attacks players on sight and also helps friends in need.
	/// </remarks>
	[Ai("Type13")]
	public class Type13 : MonsterAi
	{
		private int _targetCharacterHandle;

		protected override void Init()
		{
			During("Idle", CheckAttacks);
			During("Idle", CheckFriendAttacks);
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
			if (state.Handled || _targetCharacterHandle != 0) return;

			if (Character.AttackerHandleTest != 0)
			{
				_targetCharacterHandle = Character.AttackerHandleTest;
				StartRoutine("Combat", Combat(_targetCharacterHandle));
				state.Handled = true;
			}
		}

		private void CheckFriendAttacks(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var friends = Character.Map.GetMonsters(m =>
				m.Handle != Character.Handle &&
				(m as Monster)?.Data.Id == (Character as Monster)?.Data.Id &&
				m.Position.InRange(Character.Position, Character.Map.VisibleRange / 2) &&
				m.AttackerHandleTest != 0);

			if (friends.Any())
			{
				var friend = friends.First();
				_targetCharacterHandle = friend.AttackerHandleTest;
				StartRoutine("Combat", Combat(_targetCharacterHandle));
				state.Handled = true;
			}
		}

		private void CheckForTargets(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var chaseRange = (Character as Monster)?.Data.ChaseRange ?? 12;
			var players = Character.Map.GetPlayers(p =>
				!p.IsDead && p.Position.InRange(Character.Position, chaseRange));

			if (players.Any())
			{
				var target = players.OrderBy(p => p.Position.GetDistance(Character.Position)).First();
				_targetCharacterHandle = target.Handle;
				StartRoutine("Combat", Combat(target.Handle));
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
