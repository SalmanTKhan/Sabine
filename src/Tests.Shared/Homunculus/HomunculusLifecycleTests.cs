using Sabine.Shared.Util;
using Sabine.Zone.World.Actors.Components.Characters;
using Xunit;

namespace Tests.Sabine.Shared.Homunculus
{
	/// <summary>
	/// Covers the state-machine transitions used by AM_CALLHOMUN,
	/// AM_REST, and AM_RESURRECTHOMUN. Exercises the component
	/// directly with a null character — the methods under test don't
	/// dereference Character outside of NotifyOwner, which short-
	/// circuits when Entity is null.
	/// </summary>
	[Collection("PacketTable")]
	public class HomunculusLifecycleTests
	{
		private static HomunculusComponent NewLif(string name = "Liffy")
		{
			var c = new HomunculusComponent(null);
			c.Create(HomunculusType.Lif, name);
			return c;
		}

		[Fact]
		public void Fresh_create_starts_resting_with_full_hp()
		{
			var c = NewLif();
			Assert.Equal(HomunculusState.Resting, c.State);
			Assert.Equal(c.HpMax, c.Hp);
			Assert.True(c.HasContract);
		}

		[Fact]
		public void Sync_hp_to_zero_marks_state_dead()
		{
			var c = NewLif();
			Assert.Equal(HomunculusState.Resting, c.State);

			// SyncHp is the path Homunculus.TakeDamage uses to mirror
			// the live entity's HP back into persistent state.
			typeof(HomunculusComponent)
				.GetMethod("SyncHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.Invoke(c, new object[] { 0 });

			Assert.Equal(0, c.Hp);
			Assert.Equal(HomunculusState.Dead, c.State);
		}

		[Fact]
		public void Resurrect_from_dead_restores_half_hp_and_returns_to_resting()
		{
			var c = NewLif();
			typeof(HomunculusComponent)
				.GetMethod("SyncHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.Invoke(c, new object[] { 0 });
			Assert.Equal(HomunculusState.Dead, c.State);

			var ok = c.Resurrect();

			Assert.True(ok);
			Assert.Equal(HomunculusState.Resting, c.State);
			Assert.Equal(c.HpMax / 2, c.Hp);
		}

		[Fact]
		public void Resurrect_no_op_when_not_dead()
		{
			var c = NewLif();
			Assert.Equal(HomunculusState.Resting, c.State);

			var ok = c.Resurrect();

			Assert.False(ok);
			Assert.Equal(HomunculusState.Resting, c.State);
		}

		[Fact]
		public void Dead_state_persists_through_vars_round_trip()
		{
			var c = NewLif();
			typeof(HomunculusComponent)
				.GetMethod("SyncHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.Invoke(c, new object[] { 0 });

			var vars = new Variables();
			c.SaveTo(vars);

			var reloaded = new HomunculusComponent(null);
			reloaded.LoadFrom(vars);

			// SaveTo coerces Active → Resting on persist, but Dead
			// stays Dead so the player has to resurrect after re-login
			// instead of silently un-dying.
			Assert.Equal(HomunculusState.Dead, reloaded.State);
			Assert.True(reloaded.HasContract);
		}

		[Fact]
		public void Active_state_downgrades_to_resting_on_persist()
		{
			var c = NewLif();

			typeof(HomunculusComponent)
				.GetMethod("MarkActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.Invoke(c, null);
			Assert.Equal(HomunculusState.Active, c.State);

			var vars = new Variables();
			c.SaveTo(vars);

			var reloaded = new HomunculusComponent(null);
			reloaded.LoadFrom(vars);

			// The persistence layer doesn't try to revive the live
			// entity on re-login — the player calls again.
			Assert.Equal(HomunculusState.Resting, reloaded.State);
		}
	}
}
