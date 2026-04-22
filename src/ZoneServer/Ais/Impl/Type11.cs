using System.Linq;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;


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
	public class Type11 : Type10
	{
		protected override void CheckForTargetsInRange(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var myGuildId = Character.GuildId;
			if (myGuildId == 0) return;

			var attackRange = ((Character as Monster)?.Data.AttackRange ?? 1);

			using var players = PooledList<PlayerCharacter>.Rent();

			Character.Map.GetPlayers(
				players,
				(origin: Character.Position, range: attackRange, guildId: myGuildId),
				static (state, p) =>
				{
					if (p.IsDead)
						return false;

					if (p.GuildId == state.guildId)
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
