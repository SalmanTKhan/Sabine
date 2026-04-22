using System.Collections;
using System.Linq;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;


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
	public class Type10 : Type06
	{
		protected override void Init()
		{
			base.Init(); // Hooks CheckAttacks
			During("Idle", CheckForTargetsInRange);
		}

		protected override IEnumerable Idle()
		{
			// Check for targets periodically.
			while (true)
				yield return Wait(500);
		}

		protected virtual void CheckForTargetsInRange(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var attackRange = ((Character as Monster)?.Data.AttackRange ?? 1);

			using var players = PooledList<PlayerCharacter>.Rent();

			Character.Map.GetPlayers(
				players,
				(origin: Character.Position, range: attackRange),
				static (state, p) =>
				{
					if (p.IsDead)
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
