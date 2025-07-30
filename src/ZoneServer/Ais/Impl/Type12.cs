using System;
using System.Collections;
using System.Linq;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 12: Mobile Guardian
	/// </summary>
	/// <remarks>
	/// Aegis: 12
	/// Behavior: Like Type 05 (mobile, aggressive), but only attacks
	/// players who are not in the same guild as the monster.
	/// Used for castle guardians.
	/// </remarks>
	[Ai("Type12")]
	public class Type12 : Type05 // Inherits mobile aggressive logic
	{
		private void CheckForTargets(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var myGuildId = (Character as Monster)?.GuildId ?? 0;
			if (myGuildId == 0) return;

			var chaseRange = (Character as Monster)?.Data.ChaseRange ?? 12;

			var players = Character.Map.GetPlayers(p =>
				!p.IsDead &&
				p.GuildId != myGuildId &&
				p.Position.InRange(Character.Position, chaseRange));

			if (players.Any())
			{
				var target = players.OrderBy(p => p.Position.GetDistance(Character.Position)).First();
				_targetCharacterHandle = target.Handle;
				StartRoutine("Combat", Combat(target.Handle));
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
