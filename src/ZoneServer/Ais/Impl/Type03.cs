using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 03: Social Reactive
	/// </summary>
	/// <remarks>
	/// Aegis: 03
	/// Athena: 0x0181 (MD_CANMOVE|MD_CANATTACK|MD_ASSIST)
	/// Behavior: Same as Type 01, but will also help friends of the same race
	/// that are being attacked nearby.
	/// </remarks>
	[Ai("Type03")]
	public class Type03 : ReactiveAi
	{
		protected override void Init()
		{
			base.Init();
			During("Idle", CheckFriendAttacks);
		}

		private void CheckFriendAttacks(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var character = (Monster)Character;
			using var friends = PooledList<Monster>.Rent();

			Character.Map.GetMonsters(
				friends,
				(origin: character.Position, visibleRange: character.Map.VisibleRange, handle: character.Handle, id: character.Data.Id),
				static (state, monster) =>
				{
					if (monster.Handle == state.handle)
						return false;

					if (monster.Data.Id != state.id)
						return false;

					if (!monster.Position.InRange(state.origin, state.visibleRange / 2f))
						return false;

					if (monster.AttackerHandleTest == 0)
						return false;

					return true;
				});

			if (friends.Any())
			{
				var friend = friends.First();
				StartRoutine("Combat", Combat(friend.AttackerHandleTest));
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
