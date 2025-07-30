using System.Collections;
using System.Linq;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 10: Immobile Aggressive
	/// </summary>
	/// <remarks>
	/// Aegis: 10
	/// Behavior: Does not move. Attacks any player that comes within its attack range.
	/// </remarks>
	[Ai("Type10")]
	public class Type10 : MonsterAi
	{
		protected int _targetCharacterHandle;

		protected override void Init()
		{
			During("Idle", CheckAttacks);
			During("Idle", CheckForTargetsInRange);
		}

		protected override void Start()
		{
			StartRoutine("Idle", Idle());
		}

		private IEnumerable Idle()
		{
			// Infinite wait, this monster is stationary.
			while (true)
				yield return Wait(500); // Check for targets every 500ms
		}

		protected IEnumerable Combat(int handle)
		{
			_targetCharacterHandle = handle;
			var attacker = Character;
			var attackDelay = attacker.Parameters.AttackDelay;
			var attackRange = ((attacker as Monster)?.Data.AttackRange ?? 1);

			while (true)
			{
				if (!attacker.Map.TryGetCharacter(handle, out var target) || target.IsDead)
					break;

				yield return Wait(attackDelay);

				if (attacker.Position.InRange(target.Position, attackRange))
				{
					attacker.StartAttacking(target, false);
				}
				else
				{
					break;
				}
			}

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

		private void CheckForTargetsInRange(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var attackRange = ((Character as Monster)?.Data.AttackRange ?? 1);

			var players = Character.Map.GetPlayers(p =>
				!p.IsDead &&
				p.Position.InRange(Character.Position, attackRange));

			if (players.Any())
			{
				var target = players.OrderBy(p => p.Position.GetDistance(Character.Position)).First();
				StartRoutine("Combat", Combat(target.Handle));
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
