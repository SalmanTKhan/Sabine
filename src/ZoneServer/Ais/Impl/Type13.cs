using System.Linq;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 13: Aggressive Social
	/// </summary>
	/// <remarks>
	/// Aegis: 13
	/// Behavior: Combination of Type 04 (Aggressive) and Type 03 (Social).
	/// It attacks players on sight and also helps friends in need.
	/// </remarks>
	[Ai("Type13")]
	public class Type13 : AggressiveAi
	{
		protected override void Init()
		{
			base.Init(); // Hooks CheckAttacks and CheckForTargets
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
