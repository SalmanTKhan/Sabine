using System.Collections;
using System.Linq;
using Sabine.Shared.World;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;
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
	/// Behavior: A combination of Type 02 (Looter) and Type 03 (Social).
	/// It picks up items, attacks back when hit, and helps friends in need.
	/// </remarks>
	[Ai("Type07")]
	public class Type07 : ReactiveAi
	{
		private int _targetItemHandle;

		protected override void Init()
		{
			base.Init(); // Hooks CheckAttacks
			During("Idle", CheckFriendAttacks);
			During("Idle", CheckNearbyItems);

			During("PickUpItem", CheckTargetItem);
			During("PickUpItem", base.CheckAttacks); // Can be attacked while looting
		}

		private IEnumerable PickUpItem(int handle, Position pos)
		{
			_targetItemHandle = handle;
			yield return MoveTo(pos);
			yield return PickUp(handle);

			StartRoutine("Idle", Idle());
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

		private void CheckNearbyItems(CallbackState state)
		{
			if (state.Handled) return;

			if (TryFindNearbyItem(out var handle, out var pos))
			{
				StartRoutine("PickUpItem", PickUpItem(handle, pos));
				state.Handled = true;
			}
		}

		private void CheckTargetItem(CallbackState state)
		{
			if (state.Handled) return;

			if (!EntityExists(_targetItemHandle))
			{
				_targetItemHandle = 0;
				StopMove();
				StartRoutine("Idle", Idle());
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009
