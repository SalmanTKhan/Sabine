using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;


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

			using var players = PooledList<PlayerCharacter>.Rent();

			Character.Map.GetPlayers(
				players,
				(origin: Character.Position, range: chaseRange),
				static (state, p) =>
				{
					if (p.IsDead || p.IsHidden)
						return false;

					if (!p.Position.InRange(state.origin, state.range))
						return false;

					return true;
				});

			PlayerCharacter? bestCaster = null;
			var bestCasterDist = float.MaxValue;

			PlayerCharacter? bestPlayer = null;
			var bestPlayerDist = float.MaxValue;

			foreach (var p in players)
			{
				var dist = p.Position.GetDistance(Character.Position);

				if (dist < bestPlayerDist)
				{
					bestPlayerDist = dist;
					bestPlayer = p;
				}

				if (p.IsCasting && dist < bestCasterDist)
				{
					bestCasterDist = dist;
					bestCaster = p;
				}
			}

			target = bestCaster ?? bestPlayer;

			if (target != null)
			{
				StartRoutine("Combat", Combat(target.Handle));
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
