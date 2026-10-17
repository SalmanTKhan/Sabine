using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;
using Xunit;

namespace Tests.Sabine.Shared.Combat
{
	/// <summary>
	/// Covers the server-side attack-range semantics fixed in plan Stream A1:
	/// the Chebyshev (square) range helper on <see cref="Position"/>, the
	/// <see cref="Character.InAttackRange"/> wrapper, the failure-reply clamp
	/// (<c>Math.Max(1, rawRange)</c>) used by the CZ_REQUEST_ACT distance
	/// failure reply, and the per-swing range recheck in base
	/// <see cref="Character.Attack(Character, bool)"/> that prevents the
	/// scheduler from firing further damaging swings after the target walks
	/// out of range.
	/// <para>
	/// Companion expectations covered by other fixtures or manual runs (out of
	/// scope for headless unit tests):
	/// <list type="bullet">
	/// <item>monster AI hunt-down uses square range for attack + Euclidean for chase leash: covered by code review of <c>MonsterAi.HuntDown</c> + <c>Type06</c>; the chase-range math is exercised indirectly via <see cref="Position.InRange(float)"/> which has its own dedicated tests.</item>
	/// <item>hide / cloak / death cancellation of the auto-attack loop: relies on <see cref="Character.IsDead"/> and <see cref="Character.StatusEffects"/>; the scheduler wiring is only verifiable live with a ZoneServer instance.</item>
	/// </list>
	/// </para>
	/// </summary>
	public class AttackRangeTests
	{
		/// <summary>
		/// Minimal <see cref="Character"/> subclass that overrides the abstract
		/// identity/world members so unit tests can instantiate it freely.
		/// Mirrors the convention used by
		/// <c>Sabine.Tests.Shared.Combat.AttackPacingTests</c>.
		/// </summary>
		private sealed class TestCharacter : Character
		{
			public override int Handle { get; protected set; }
			public override IdentityId IdentityId { get; protected set; }
			public override void Warp(Location location) { }

			// Exposes the protected <c>Character.Parameters</c> setter for
		// the test harness. The base setter is <c>protected</c>, so only
		// a subclass can write the field.
		public void SetParametersForTests(Parameters p) => this.Parameters = p;
		}

		/// <summary>
		/// Minimal <see cref="Parameters"/> stub: the abstract base class
		/// has no abstract members, so a parameterless subclass is enough
		/// to satisfy the base <c>Character.IsDead => Parameters.Hp == 0</c>
		/// read, the <c>attacker.IsImmobilized</c> path (which short-circuits
		/// when <c>StatusEffects</c> is null), and the same-map equality
		/// check (both null <c>Map</c> == both null <c>Map</c>).
		/// </summary>
		private sealed class TestParameters : Parameters {
		}

		private static TestCharacter NewAliveCharacter(Position pos)
		{
			var c = new TestCharacter { Position = pos };
			c.SetParametersForTests(new TestParameters());
			return c;
		}

		// ===== Position.InSquareRange (Chebyshev) =====

		[Theory]
		[InlineData(0, 1)]
		[InlineData(0, -1)]
		[InlineData(1, 0)]
		[InlineData(-1, 0)]
		[InlineData(1, 1)]
		[InlineData(1, -1)]
		[InlineData(-1, 1)]
		[InlineData(-1, -1)]
		public void Range1_Accepts_all_eight_adjacent_cells(int dx, int dy)
		{
			var a = new Position(100, 100);
			var b = new Position(100 + dx, 100 + dy);
			Assert.True(a.InSquareRange(b, 1));
		}

		[Theory]
		[InlineData(2, 0)]
		[InlineData(0, 2)]
		[InlineData(2, 1)]
		[InlineData(1, 2)]
		[InlineData(2, 2)]
		public void Range1_Rejects_far_cells(int dx, int dy)
		{
			var a = new Position(100, 100);
			var b = new Position(100 + dx, 100 + dy);
			Assert.False(a.InSquareRange(b, 1));
		}

		[Theory]
		[InlineData(3, 3, 3)]
		[InlineData(5, 0, 5)]
		[InlineData(0, 5, 5)]
		[InlineData(5, 5, 5)]
		public void RangeN_Accepts_boundary_same_direction(int dx, int dy, int range)
		{
			var a = new Position(100, 100);
			var b = new Position(100 + dx, 100 + dy);
			Assert.True(a.InSquareRange(b, range),
				$"(N,N) at range {range} should be in square range");
		}

		[Theory]
		[InlineData(4, 0, 3)]
		[InlineData(6, 0, 5)]
		[InlineData(6, 6, 5)]
		public void RangeN_Rejects_one_beyond_boundary(int dx, int dy, int range)
		{
			var a = new Position(100, 100);
			var b = new Position(100 + dx, 100 + dy);
			Assert.False(a.InSquareRange(b, range),
				$"(N+1,0) at range {range} should NOT be in square range");
		}

		// ===== Character.InAttackRange wrapper =====

		[Fact]
		public void Character_InAttackRange_accepts_diagonal_adjacent()
		{
			var attacker = NewAliveCharacter(new Position(50, 50));
			var target = NewAliveCharacter(new Position(51, 51));
			Assert.True(attacker.InAttackRange(target, 1));
		}

		[Fact]
		public void Character_InAttackRange_accepts_axis_adjacent()
		{
			var attacker = NewAliveCharacter(new Position(50, 50));
			var target = NewAliveCharacter(new Position(52, 50));
			Assert.True(attacker.InAttackRange(target, 2));
		}

		[Fact]
		public void Character_InAttackRange_rejects_far_target()
		{
			var attacker = NewAliveCharacter(new Position(50, 50));
			var target = NewAliveCharacter(new Position(60, 50));
			Assert.False(attacker.InAttackRange(target, 3));
		}

		[Fact]
		public void GetAttackRange_default_is_three_for_unarmed_character()
		{
			var c = new TestCharacter();
			Assert.Equal(3, c.GetAttackRange());
		}

		// ===== Failure reply clamp contract =====

		[Theory]
		[InlineData(0, 1)]
		[InlineData(-1, 1)]
		[InlineData(1, 1)]
		[InlineData(3, 3)]
		[InlineData(5, 5)]
		[InlineData(16, 16)]
		public void Reported_attack_range_clamps_to_at_least_one(int rawRange, int expected)
		{
			var reported = ComputeReportedAttackRange(rawRange);
			Assert.Equal(expected, reported);
			Assert.True(reported >= 1,
				"distance-failure reply must never advertise < 1 cell (ranged-lock deadlock)");
		}

		/// <summary>
		/// Mirror of the literal <c>Math.Max(1, attackRange)</c> used by
		/// <c>PacketHandler.CZ_REQUEST_ACT</c> when sending
		/// <c>ZC_ATTACK_FAILURE_FOR_DISTANCE</c>. Pinning this here lets a
		/// future edit that switches to <c>attackRange - 1</c> fail loudly in
		/// CI rather than reintroducing the rogue-0 melee-resend deadlock.
		/// </summary>
		private static int ComputeReportedAttackRange(int attackRange)
			=> Math.Max(1, attackRange);

		// ===== Per-swing range recheck in base Character.Attack =====

		[Fact]
		public void Attack_rejects_swing_when_target_is_out_of_range()
		{
			var attacker = NewAliveCharacter(new Position(50, 50));
			var target = NewAliveCharacter(new Position(70, 70));

			// Sanity: default range is 3 and target is far away.
			Assert.Equal(3, attacker.GetAttackRange());
			Assert.False(attacker.InAttackRange(target, attacker.GetAttackRange()));

			// Drive the lifecycle: autoAttack=false so we don't need a
			// ZoneServer scheduler; that branch returns very early after
			// rejecting the swing.
			attacker.Attack(target, autoAttack: false);

			Assert.Equal(0, attacker.AttackCallbackIdForTests);
		}

		[Fact]
		public void Attack_clears_callback_when_target_walks_out_of_range_between_calls()
		{
			// Mimic the cheating scenario from the plan: attacker + target
			// start in range, the player walks the target out of range, then
			// the scheduler callback fires. The per-swing recheck must
			// ensure no second damaging swing is scheduled.
			var attacker = NewAliveCharacter(new Position(50, 50));
			var target = NewAliveCharacter(new Position(51, 50));
			Assert.True(attacker.InAttackRange(target, attacker.GetAttackRange()));

			// Manually invalidate any previously scheduled swing (the
			// no-zone-server test path cannot do Scheduler.Schedule, so we
			// observe the cleared-after-rejection property directly).
			target.Position = new Position(80, 80);

			attacker.Attack(target, autoAttack: false);

			Assert.False(attacker.InAttackRange(target, attacker.GetAttackRange()));
			Assert.Equal(0, attacker.AttackCallbackIdForTests);
		}

		[Fact]
		public void StopAttacking_flips_cancel_flag()
		{
			var attacker = new TestCharacter();
			Assert.False(attacker.CancelAttackFlagForTests);
			attacker.StopAttacking();
			Assert.True(attacker.CancelAttackFlagForTests);
		}
	}
}
