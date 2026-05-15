using System.Reflection;
using Sabine.Shared.Const;
using Sabine.Shared.Util;
using Sabine.Zone.World.Actors.Components.Characters;
using Xunit;

namespace Tests.Sabine.Shared.Homunculus
{
	/// <summary>
	/// Covers the Evolve / Mutate / Shuffle state-machine transitions
	/// added in the evolution slice. Component-level tests with a
	/// null character — these paths don't touch the live entity
	/// outside of the optional <c>RefreshFromState</c> call, which
	/// short-circuits on a null entity.
	/// </summary>
	[Collection("PacketTable")]
	public class HomunculusEvolutionTests
	{
		private static HomunculusComponent NewLif()
		{
			var c = new HomunculusComponent(null);
			c.Create(HomunculusType.Lif, "Liffy");
			return c;
		}

		private static void Kill(HomunculusComponent c)
			=> typeof(HomunculusComponent)
				.GetMethod("SyncHp", BindingFlags.NonPublic | BindingFlags.Instance)
				.Invoke(c, new object[] { 0 });

		// --- Evolve ---------------------------------------------------------

		[Fact]
		public void Evolve_promotes_to_evolved_form_at_loyal_intimacy()
		{
			var c = NewLif();
			c.SetIntimacy(HomunculusComponent.EvolveIntimacyThreshold);

			var result = c.Evolve();

			Assert.Equal(HomunculusType.Lif2, result);
			Assert.Equal(HomunculusType.Lif2, c.Type);
			Assert.True(c.Type.IsEvolved());
		}

		[Fact]
		public void Evolve_rejects_low_intimacy()
		{
			var c = NewLif();
			c.SetIntimacy(HomunculusComponent.EvolveIntimacyThreshold - 1);

			Assert.Equal(HomunculusType.None, c.Evolve());
			Assert.Equal(HomunculusType.Lif, c.Type);
		}

		[Fact]
		public void Evolve_rejects_already_evolved()
		{
			var c = NewLif();
			c.SetIntimacy(1000);
			c.Evolve();
			Assert.Equal(HomunculusType.Lif2, c.Type);

			Assert.Equal(HomunculusType.None, c.Evolve());
			Assert.Equal(HomunculusType.Lif2, c.Type);
		}

		[Fact]
		public void Evolve_rejects_when_dead()
		{
			var c = NewLif();
			c.SetIntimacy(1000);
			Kill(c);

			Assert.Equal(HomunculusType.None, c.Evolve());
			Assert.Equal(HomunculusType.Lif, c.Type);
		}

		[Fact]
		public void Evolve_carries_level_exp_and_intimacy()
		{
			var c = NewLif();
			c.SetIntimacy(950);
			c.GainExp(50 * 4 * 4 + 50 * 9 * 9); // push past a couple levels
			var levelBefore = c.Level;

			c.Evolve();

			Assert.Equal(levelBefore, c.Level);
			Assert.Equal(950, c.Intimacy);
		}

		// --- Mutate ---------------------------------------------------------

		[Fact]
		public void Mutate_rejects_base_form()
		{
			var c = NewLif();
			c.SetIntimacy(1000);

			Assert.Equal(HomunculusType.None, c.Mutate());
			Assert.Equal(HomunculusType.Lif, c.Type);
		}

		[Fact]
		public void Mutate_rejects_evolved_below_level_threshold()
		{
			var c = NewLif();
			c.SetIntimacy(1000);
			c.Evolve();
			Assert.Equal(HomunculusType.Lif2, c.Type);

			// Level is still 1 — below the 99 threshold.
			Assert.Equal(HomunculusType.None, c.Mutate());
			Assert.Equal(HomunculusType.Lif2, c.Type);
		}

		[Fact]
		public void Mutate_promotes_evolved_at_threshold_to_sclass()
		{
			var c = NewLif();
			c.SetIntimacy(1000);
			c.Evolve();
			ForceLevel(c, HomunculusComponent.MutateLevelThreshold);

			var result = c.Mutate();

			Assert.Equal(HomunculusType.Eira, result);
			Assert.True(c.Type.IsSClass());
		}

		[Fact]
		public void Family_helpers_round_trip_through_all_three_tiers()
		{
			Assert.Equal(HomunculusType.Lif, HomunculusType.Lif2.GetBaseFamily());
			Assert.Equal(HomunculusType.Lif, HomunculusType.Eira.GetBaseFamily());
			Assert.Equal(HomunculusType.Amistr2, HomunculusType.Amistr.GetEvolvedForm());
			Assert.Equal(HomunculusType.Bayeri, HomunculusType.Amistr2.GetSClassForm());
		}

		// --- Foods + skills inherit through family --------------------------

		[Fact]
		public void Evolved_form_inherits_base_food_list()
		{
			var lif = HomunculusTypeData.GetFoods(HomunculusType.Lif);
			var lif2 = HomunculusTypeData.GetFoods(HomunculusType.Lif2);
			Assert.Equal(lif.Count, lif2.Count);
		}

		[Fact]
		public void Sclass_form_inherits_base_skill_tree()
		{
			var amistr = HomunculusTypeData.GetSkills(HomunculusType.Amistr);
			var bayeri = HomunculusTypeData.GetSkills(HomunculusType.Bayeri);
			Assert.Equal(amistr.Count, bayeri.Count);
		}

		[Fact]
		public void Evolved_form_grants_plus5_max_skill_level()
		{
			Assert.Equal(5, HomunculusTypeData.GetMaxLevelBonus(HomunculusType.Lif2));
			Assert.Equal(10, HomunculusTypeData.GetMaxLevelBonus(HomunculusType.Eira));
			Assert.Equal(0, HomunculusTypeData.GetMaxLevelBonus(HomunculusType.Lif));
		}

		[Fact]
		public void Evolution_persists_through_vars_round_trip()
		{
			var c = NewLif();
			c.SetIntimacy(1000);
			c.Evolve();
			Assert.Equal(HomunculusType.Lif2, c.Type);

			var vars = new Variables();
			c.SaveTo(vars);

			var reloaded = new HomunculusComponent(null);
			reloaded.LoadFrom(vars);

			Assert.Equal(HomunculusType.Lif2, reloaded.Type);
		}

		// Bump Level via reflection — there's no public setter; the
		// production path is GainExp loops, which is slow for 99
		// levels' worth of EXP.
		private static void ForceLevel(HomunculusComponent c, int level)
		{
			var prop = typeof(HomunculusComponent).GetProperty("Level");
			prop.SetValue(c, level);
		}
	}
}
