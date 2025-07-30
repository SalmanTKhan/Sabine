using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;
using Sabine.Zone.World.Entities;

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

			var friends = Character.Map.GetMonsters(m =>
				m.Handle != Character.Handle &&
				(m as Monster)?.Data.Id == (Character as Monster)?.Data.Id &&
				m.Position.InRange(Character.Position, Character.Map.VisibleRange / 2) &&
				m.AttackerHandleTest != 0);

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
