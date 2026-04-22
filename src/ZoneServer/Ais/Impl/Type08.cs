using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;


#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 08: Aggressive vs. Low Level
	/// </summary>
	/// <remarks>
	/// Aegis: 08
	/// Behavior: Attacks players on sight, but only if they are a certain number
	/// of levels below the monster (e.g., 5 levels).
	/// </remarks>
	[Ai("Type08")]
	public class Type08 : AggressiveAi
	{
		private const int LevelDifference = 5;

		protected override void CheckForTargets(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var chaseRange = (Character as Monster)?.Data.ChaseRange ?? 12;
			var myLevel = Character.Parameters.BaseLevel;

			using var players = PooledList<PlayerCharacter>.Rent();

			Character.Map.GetPlayers(
				players,
				(origin: Character.Position, range: chaseRange, level: myLevel, diff: LevelDifference),
				static (state, p) =>
				{
					if (p.IsDead)
						return false;

					if (p.Parameters.BaseLevel > state.level - state.diff)
						return false;

					if (!p.Position.InRange(state.origin, state.range))
						return false;

					return true;
				});

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
