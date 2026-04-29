using System.Linq;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;


#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Base
{
	/// <summary>
	/// Base class for AIs that are aggressive and attack players on sight.
	/// </summary>
	public abstract class AggressiveAi : ReactiveAi
	{
		protected override void Init()
		{
			base.Init(); // Hooks CheckAttacks
			During("Idle", CheckForTargets);
		}

		/// <summary>
		/// Checks for nearby players and starts combat if any are found.
		/// </summary>
		protected virtual void CheckForTargets(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var chaseRange = (Character as Monster)?.Data.ChaseRange ?? 12;

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
