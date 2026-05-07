using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 07: Social Looter
	/// </summary>
	/// <remarks>
	/// Aegis: 07
	/// Athena: 0x0183 (MD_CANMOVE|MD_CANATTACK|MD_LOOTER|MD_ASSIST)
	/// Behavior: combination of Type 02 (Looter) and Type 03 (Social).
	/// Picks up items, fights back when attacked, helps friends in need,
	/// and leashes back to spawn. Looter and leash behavior is inherited;
	/// the friend-assist callback is layered on top.
	/// </remarks>
	[Ai("Type07")]
	public class Type07 : LooterAi
	{
		protected override void Init()
		{
			base.Init(); // Hooks CheckAttacks, CheckNearbyItems, CheckTargetItem.
			During("Idle", CheckFriendAttacks);
		}

		private void CheckFriendAttacks(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0)
				return;

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
