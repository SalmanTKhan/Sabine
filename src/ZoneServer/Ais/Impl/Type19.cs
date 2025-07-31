using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 19: Aggressive Anti-Magic
	/// </summary>
	/// <remarks>
	/// Aegis: 19
	/// Behavior: An aggressive monster that attacks on sight, but prioritizes
	/// players who are casting spells.
	/// </remarks>
	[Ai("Type19")]
	public class Type19 : AggressiveAi
	{
		protected override void CheckForTargets(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var chaseRange = (Character as Monster)?.Data.ChaseRange ?? 12;
			Character target = null;

			// Prioritize casters
			var casters = Character.Map.GetPlayers(p =>
				!p.IsDead && p.IsCasting && p.Position.InRange(Character.Position, chaseRange));

			if (casters.Any())
			{
				target = casters.OrderBy(p => p.Position.GetDistance(Character.Position)).First();
			}
			else
			{
				// If no casters, find any player
				var players = Character.Map.GetPlayers(p =>
					!p.IsDead && p.Position.InRange(Character.Position, chaseRange));
				if (players.Any())
					target = players.OrderBy(p => p.Position.GetDistance(Character.Position)).First();
			}

			if (target != null)
			{
				StartRoutine("Combat", Combat(target.Handle));
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
