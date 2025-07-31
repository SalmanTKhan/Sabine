using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 17: Anti-Magic
	/// </summary>
	/// <remarks>
	/// Aegis: 17
	/// Behavior: A passive monster that becomes aggressive towards any player
	/// who starts casting a spell in its vicinity.
	/// </remarks>
	[Ai("Type17")]
	public class Type17 : ReactiveAi
	{
		protected override void Init()
		{
			base.Init(); // Hooks CheckAttacks
			During("Idle", CheckForCasters);
		}

		private void CheckForCasters(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var chaseRange = (Character as Monster)?.Data.ChaseRange ?? 12;

			var players = Character.Map.GetPlayers(p =>
				!p.IsDead && p.IsCasting && p.Position.InRange(Character.Position, chaseRange));

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
