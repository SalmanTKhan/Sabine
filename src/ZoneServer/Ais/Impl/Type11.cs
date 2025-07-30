using System;
using System.Collections;
using System.Linq;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 11: Immobile Guardian
	/// </summary>
	/// <remarks>
	/// Aegis: 11
	/// Behavior: Like Type 10 (immobile, attacks in range), but only attacks
	/// players who are not in the same guild as the monster.
	/// Used for castle guardians.
	/// </remarks>
	[Ai("Type11")]
	public class Type11 : Type10 // Inherits immobile combat logic
	{
		private void CheckForTargetsInRange(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			// Guardians are associated with a guild.
			var myGuildId = (Character as Monster)?.GuildId ?? 0;
			if (myGuildId == 0) return; // Not a guardian if no guild is set.

			var attackRange = ((Character as Monster)?.Data.AttackRange ?? 1);

			// Assuming PlayerCharacter has a GuildId property.
			var players = Character.Map.GetPlayers(p =>
				!p.IsDead &&
				p.GuildId != myGuildId &&
				p.Position.InRange(Character.Position, attackRange));

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
