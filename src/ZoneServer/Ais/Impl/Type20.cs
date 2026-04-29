using System.Linq;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;


#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 20: Aggressive Anti-Magic with Target Switching
	/// </summary>
	/// <remarks>
	/// Aegis: 20
	/// Behavior: Same as Type 19, but will switch its current target to attack
	/// a player who starts casting a spell nearby.
	/// </remarks>
	[Ai("Type20")]
	public class Type20 : Type19
	{
		protected override void Init()
		{
			base.Init();
			During("Combat", CheckForCastersAndSwitch);
		}

		private void CheckForCastersAndSwitch(CallbackState state)
		{
			if (state.Handled) return;

			var chaseRange = (Character as Monster)?.Data.ChaseRange ?? 12;

			using var casters = PooledList<PlayerCharacter>.Rent();

			Character.Map.GetPlayers(
				casters,
				(targetHandle: _targetCharacterHandle, origin: Character.Position, range: chaseRange),
				static (state, p) =>
				{
					if (p.IsDead || p.IsHidden)
						return false;

					if (p.Handle == state.targetHandle) // Not the current target
						return false;

					if (!p.IsCasting)
						return false;

					if (!p.Position.InRange(state.origin, state.range))
						return false;

					return true;
				});

			if (casters.Any())
			{
				var newTarget = casters.OrderBy(p => p.Position.GetDistance(Character.Position)).First();
				// Switch target
				StartRoutine("Combat", Combat(newTarget.Handle)); // Restarts HuntDown on new target
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
