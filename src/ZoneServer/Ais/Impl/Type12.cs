using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;

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
	public class Type12 : AggressiveAi
	{
		protected override void CheckForTargets(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var myGuildId = Character.GuildId;
			if (myGuildId == 0) return;

			var chaseRange = (Character as Monster)?.Data.ChaseRange ?? 12;

			using var players = PooledList<PlayerCharacter>.Rent();

			Character.Map.GetPlayers(
				players,
				(origin: Character.Position, range: chaseRange, guildId: myGuildId),
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
