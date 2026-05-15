using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Shared.Util;
using Sabine.Zone.World.Actors.Components.Characters;
using Xunit;

namespace Tests.Sabine.Shared.Homunculus
{
	/// <summary>
	/// Covers the player-control surface added in the homunculus
	/// parity slice: rename round-trips through vars, feed clamps
	/// hunger and intimacy correctly, dismiss wipes the contract, and
	/// the EP9 wire-table still has the homunculus opcodes registered
	/// at their canonical kRO/eA positions.
	/// </summary>
	[Collection("PacketTable")]
	public class HomunculusComponentTests
	{
		private static HomunculusComponent NewLif(string name = "Liffy")
		{
			// The component never dereferences Character in the paths
			// exercised below; passing null avoids booting a server.
			var c = new HomunculusComponent(null);
			c.Create(HomunculusType.Lif, name);
			return c;
		}

		[Fact]
		public void Rename_persists_through_vars_round_trip()
		{
			var c = NewLif();
			c.Rename("Sparky");

			var vars = new Variables();
			c.SaveTo(vars);

			var reloaded = new HomunculusComponent(null);
			reloaded.LoadFrom(vars);

			Assert.Equal(HomunculusType.Lif, reloaded.Type);
			Assert.Equal("Sparky", reloaded.Name);
		}

		[Fact]
		public void Rename_rejects_whitespace_input()
		{
			var c = NewLif("Original");
			c.Rename("");
			c.Rename("   ");
			Assert.Equal("Original", c.Name);
		}

		[Fact]
		public void FeedItem_below_cap_raises_hunger_and_intimacy()
		{
			var c = NewLif();
			c.SetHunger(50);
			c.SetIntimacy(100);

			// 537 (PetFood) is on every type's food list; gives +11 hunger.
			var fed = c.FeedItem(537);

			Assert.True(fed);
			Assert.Equal(61, c.Hunger);
			Assert.True(c.Intimacy > 100);
		}

		[Fact]
		public void FeedItem_over_cap_clamps_hunger_and_drops_intimacy()
		{
			var c = NewLif();
			c.SetHunger(95);
			c.SetIntimacy(200);

			c.FeedItem(537); // would push hunger past 100

			Assert.Equal(100, c.Hunger);
			// Over-feed penalty: -5 intimacy per eAthena classic.
			Assert.Equal(195, c.Intimacy);
		}

		[Fact]
		public void FeedItem_rejects_food_not_on_type_list()
		{
			var c = NewLif();
			c.SetHunger(50);
			var before = c.Hunger;

			// Mushroom (521) is only on Vanilmirth's list; Lif refuses.
			var fed = c.FeedItem(521);

			Assert.False(fed);
			Assert.Equal(before, c.Hunger);
		}

		[Fact]
		public void Clear_wipes_contract_and_resets_state()
		{
			var c = NewLif();
			c.SetIntimacy(500);
			c.SetHunger(75);
			c.Clear();

			Assert.Equal(HomunculusType.None, c.Type);
			Assert.Equal(HomunculusState.Absent, c.State);
			Assert.False(c.HasContract);
			Assert.Equal(0, c.Intimacy);
			Assert.Equal(string.Empty, c.Name);
		}

		[Fact]
		public void Snapshot_round_trip_preserves_all_state()
		{
			var c = NewLif("Snappy");
			c.SetIntimacy(750);
			c.SetHunger(80);
			c.GainExp(50 * 4 * 4);
			c.TryLearn(SkillId.HLIF_HEAL);
			c.Rename("Renamed");

			var snapshot = c.ToSnapshot();
			Assert.NotNull(snapshot);
			Assert.Equal((int)HomunculusType.Lif, snapshot.Type);
			Assert.Equal("Renamed", snapshot.Name);
			Assert.Equal(750, snapshot.Intimacy);
			Assert.Equal(80, snapshot.Hunger);
			Assert.Contains($"{(int)SkillId.HLIF_HEAL}:1", snapshot.Skills);

			var reloaded = new HomunculusComponent(null);
			reloaded.LoadFromSnapshot(snapshot);

			Assert.Equal(HomunculusType.Lif, reloaded.Type);
			Assert.Equal("Renamed", reloaded.Name);
			Assert.Equal(c.Level, reloaded.Level);
			Assert.Equal(c.Hp, reloaded.Hp);
			Assert.Equal(c.HpMax, reloaded.HpMax);
			Assert.Equal(750, reloaded.Intimacy);
			Assert.Equal(80, reloaded.Hunger);
			Assert.Equal(c.Exp, reloaded.Exp);
			Assert.Equal(1, reloaded.GetSkillLevel(SkillId.HLIF_HEAL));
		}

		[Fact]
		public void Snapshot_returns_null_for_empty_contract()
		{
			var c = new HomunculusComponent(null);
			Assert.Null(c.ToSnapshot());

			c.Create(HomunculusType.Lif, "T");
			c.Clear();
			Assert.Null(c.ToSnapshot());
		}

		[Fact]
		public void Snapshot_active_downgrades_to_resting()
		{
			var c = NewLif();
			typeof(HomunculusComponent)
				.GetMethod("MarkActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.Invoke(c, null);

			var snapshot = c.ToSnapshot();
			Assert.Equal((int)HomunculusState.Resting, snapshot.State);
		}

		[Fact]
		public void Clear_then_save_persists_empty_row()
		{
			var c = NewLif();
			c.Clear();

			var vars = new Variables();
			c.SaveTo(vars);

			// SaveTo writes type=None and bails before the other keys;
			// LoadFrom on the same vars should leave a fresh component empty.
			var reloaded = new HomunculusComponent(null);
			reloaded.LoadFrom(vars);
			Assert.Equal(HomunculusType.None, reloaded.Type);
			Assert.False(reloaded.HasContract);
		}
	}

	/// <summary>
	/// Wire-table pins for the homunculus opcode block. The EP9
	/// register-call positions these at the kRO/eA canonical slots;
	/// regressions here break the open-midgard client (PV23 expects
	/// 0x022E for the property block and 0x0235 for the skill list).
	/// </summary>
	[Collection("PacketTable")]
	public class HomunculusPacketTableTests
	{
		[Fact]
		public void EP9_registers_homunculus_opcodes_at_canonical_slots()
		{
			using var _ = new PacketTableScope(Versions.S900);

			Assert.Equal(0x022D, PacketTable.ToNetwork(Op.CZ_COMMAND_MER));
			Assert.Equal(5, PacketTable.GetSize(0x022D));

			Assert.Equal(0x022E, PacketTable.ToNetwork(Op.ZC_PROPERTY_HOMUN));
			Assert.Equal(71, PacketTable.GetSize(0x022E));

			Assert.Equal(0x022F, PacketTable.ToNetwork(Op.ZC_FEED_MER));
			Assert.Equal(0x0231, PacketTable.ToNetwork(Op.CZ_RENAME_MER));
			Assert.Equal(0x0235, PacketTable.ToNetwork(Op.ZC_HOSKILLINFO_LIST));
			Assert.Equal(0x0239, PacketTable.ToNetwork(Op.ZC_HOSKILLINFO_UPDATE));
		}

		[Fact]
		public void S2500_keeps_homunculus_block_registered()
		{
			using var _ = new PacketTableScope(Versions.S2500);

			// S2500 inherits EP9 and later overlays — the homunc block
			// should still resolve, even if some opcodes get re-sized.
			Assert.Equal(0x022D, PacketTable.ToNetwork(Op.CZ_COMMAND_MER));
			Assert.Equal(0x022E, PacketTable.ToNetwork(Op.ZC_PROPERTY_HOMUN));
			Assert.Equal(0x0231, PacketTable.ToNetwork(Op.CZ_RENAME_MER));
			Assert.Equal(0x0235, PacketTable.ToNetwork(Op.ZC_HOSKILLINFO_LIST));
		}

		/// <summary>
		/// Switches Game.Version + reloads the static PacketTable for
		/// one test, then restores it on dispose so other tests see
		/// the prior table state. Local copy of the same pattern
		/// other packet-table tests use.
		/// </summary>
		private sealed class PacketTableScope : System.IDisposable
		{
			private readonly int _previousVersion;

			public PacketTableScope(int version)
			{
				_previousVersion = Game.Version;
				Game.Version = version;
				PacketTable.Load();
			}

			public void Dispose()
			{
				Game.Version = _previousVersion;
				PacketTable.Load();
			}
		}
	}
}
