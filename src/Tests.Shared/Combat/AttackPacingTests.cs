using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;
using Xunit;

namespace Tests.Sabine.Shared.Combat
{
	/// <summary>
	/// Covers the server-side pacing gates that reject attack / skill /
	/// movement request packets arriving before the previous action's
	/// motion delay has elapsed. Mirrors rA ud.attackabletime /
	/// canact_tick / canmove_tick semantics.
	/// </summary>
	public class AttackPacingTests
	{
		private sealed class TestCharacter : Character
		{
			public override int Handle { get; protected set; }
			public override IdentityId IdentityId { get; protected set; }
			public override void Warp(Location location) { }
		}

		[Fact]
		public void Fresh_character_can_act_and_move()
		{
			var c = new TestCharacter();
			var now = DateTime.UtcNow;

			Assert.True(c.CanAttackNow(now));
			Assert.True(c.CanActNow(now));
			Assert.True(c.CanMoveNow(now));
		}

		[Fact]
		public void Attack_blocks_subsequent_attack_within_motion_delay()
		{
			var c = new TestCharacter();
			c.NoteAttacked(800);

			// Immediately after stamping, we're well inside the 800ms window
			// and outside the 50ms grace.
			Assert.False(c.CanAttackNow(DateTime.UtcNow));
		}

		[Fact]
		public void Attack_window_clears_after_motion_delay_elapses()
		{
			var c = new TestCharacter();
			c.NoteAttacked(200);

			// Future time well past the 200ms motion delay should be allowed.
			var future = DateTime.UtcNow.AddMilliseconds(500);
			Assert.True(c.CanAttackNow(future));
		}

		[Fact]
		public void Attack_grace_window_lets_slightly_early_packets_through()
		{
			var c = new TestCharacter();
			c.NoteAttacked(200);

			// 10ms before the gate end: inside the 50ms grace, allowed.
			var nearlyDone = DateTime.UtcNow.AddMilliseconds(190);
			Assert.True(c.CanAttackNow(nearlyDone));
		}

		[Fact]
		public void Skill_lockout_also_blocks_attack_via_unified_act_channel()
		{
			var c = new TestCharacter();
			c.NoteActed(800);

			// CanAttackNow consults NextActTime, so a skill's after-cast
			// lockout must block basic attacks too.
			Assert.False(c.CanAttackNow(DateTime.UtcNow));
			Assert.False(c.CanActNow(DateTime.UtcNow));

			// Movement is on its own channel — should still be allowed.
			Assert.True(c.CanMoveNow(DateTime.UtcNow));
		}

		[Fact]
		public void Note_helpers_only_extend_never_shrink_the_gate()
		{
			var c = new TestCharacter();
			c.NoteActed(1000);
			c.NoteActed(100); // shorter, must not shorten the gate

			// Still blocked well past the shorter delay.
			var afterShort = DateTime.UtcNow.AddMilliseconds(300);
			Assert.False(c.CanActNow(afterShort));
		}

		[Fact]
		public void Move_blocks_subsequent_move_within_step_delay()
		{
			var c = new TestCharacter();
			c.NoteMoveStarted(500);

			// Within the step delay, outside the 150ms move grace.
			Assert.False(c.CanMoveNow(DateTime.UtcNow));

			// Past the step delay, free again.
			var future = DateTime.UtcNow.AddMilliseconds(800);
			Assert.True(c.CanMoveNow(future));
		}

		[Fact]
		public void Move_grace_window_is_larger_than_attack_grace()
		{
			var c = new TestCharacter();
			c.NoteMoveStarted(200);

			// 120ms before the gate end — outside attack grace (50ms) but
			// inside move grace (150ms). Movement must be allowed.
			var t = DateTime.UtcNow.AddMilliseconds(80);
			Assert.True(c.CanMoveNow(t));
		}

		[Fact]
		public void Zero_or_negative_delays_are_ignored()
		{
			var c = new TestCharacter();
			c.NoteActed(0);
			c.NoteActed(-50);
			c.NoteMoveStarted(0);

			Assert.True(c.CanActNow(DateTime.UtcNow));
			Assert.True(c.CanMoveNow(DateTime.UtcNow));
		}
	}
}
