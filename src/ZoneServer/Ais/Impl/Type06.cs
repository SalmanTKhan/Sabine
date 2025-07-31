using System.Collections;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 06: Immobile
	/// </summary>
	/// <remarks>
	/// Aegis: 06
	/// Athena: 0x00C1 (MD_STATUE|MD_CANATTACK)
	/// Behavior: Does not move. Will attack back if attacked, but will not chase.
	/// Plants are a common example.
	/// </remarks>
	[Ai("Type06")]
	public class Type06 : MonsterAi
	{
		protected override void Init()
		{
			During("Idle", CheckAttacks);
		}

		protected override void Start()
		{
			StartRoutine("Idle", Idle());
		}

		protected virtual IEnumerable Idle()
		{
			// Infinite wait, this monster is stationary.
			while (true)
				yield return true;
		}

		protected virtual IEnumerable Combat(int handle)
		{
			_targetCharacterHandle = handle;
			var attacker = Character;
			var attackDelay = attacker.Parameters.AttackDelay;
			var attackRange = ((attacker as Monster)?.Data.AttackRange ?? 1);

			while (true)
			{
				if (!attacker.Map.TryGetCharacter(handle, out var target) || target.IsDead)
					break; // Target is gone

				yield return Wait(attackDelay);

				if (attacker.Position.InRange(target.Position, attackRange))
				{
					attacker.StartAttacking(target, false);
				}
				else
				{
					// Target is out of range, stop combat as this AI cannot chase.
					break;
				}
			}

			_targetCharacterHandle = 0;
			Character.AttackerHandleTest = 0;
			StartRoutine("Idle", Idle());
		}

		protected void CheckAttacks(CallbackState state)
		{
			if (_targetCharacterHandle != 0) return;

			if (Character.AttackerHandleTest != 0)
			{
				StartRoutine("Combat", Combat(Character.AttackerHandleTest));
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
