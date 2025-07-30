using System;
using System.Collections;
using System.Linq;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 21: Boss AI
	/// </summary>
	/// <remarks>
	/// Aegis: 21
	/// Behavior: A complex, aggressive AI. It's anti-magic and switches to casters (like Type 20),
	/// but also has opportunistic behavior: if it's chasing a target and another enemy
	/// comes into its immediate attack range, it will switch to the closer enemy.
	/// </remarks>
	[Ai("Type21")]
	public class Type21 : Type20 // Inherits from target-switching anti-magic AI
	{
		protected override void Init()
		{
			base.Init(); // Sets up Idle and Combat caster checks
			During("Combat", CheckForCloserTargets);
		}

		private void CheckForCloserTargets(CallbackState state)
		{
			if (state.Handled) return; // Caster switch has priority

			var attackRange = ((Character as Monster)?.Data.AttackRange ?? 1);

			// If current target exists and is outside our attack range (i.e., we are chasing it)
			if (TryGetEntity(_targetCharacterHandle, out var currentTarget) &&
				!currentTarget.Position.InRange(Character.Position, attackRange))
			{
				// Check for any other player inside attack range
				var closerTargets = Character.Map.GetPlayers(p =>
					!p.IsDead &&
					p.Handle != _targetCharacterHandle &&
					p.Position.InRange(Character.Position, attackRange));

				if (closerTargets.Any())
				{
					var newTarget = closerTargets.OrderBy(p => p.Position.GetDistance(Character.Position)).First();
					// Switch to the opportunistic target
					_targetCharacterHandle = newTarget.Handle;
					StartRoutine("Combat", Combat(newTarget.Handle));
					state.Handled = true;
				}
			}
		}
	}
}

#pragma warning restore IDE0009
