using Sabine.Shared.Data.Databases;
using Sabine.Zone.World.Woe;
using Xunit;

namespace Tests.Sabine.Shared.Woe
{
	/// <summary>
	/// Pure-logic tests for <see cref="Castle"/>'s eAthena-compatible
	/// numeric-slot accessors (GetData / SetData). <see cref="Castle"/>
	/// has no external dependencies, so these run without the WoE
	/// runtime singleton stack.
	/// </summary>
	public class CastleTests
	{
		private static Castle NewCastle(int id = 1, string mapId = "aldeg_cas01")
			=> new(new CastleData { Id = id, MapStringId = mapId, Name = "Test Castle" });

		[Fact]
		public void GetData_returns_template_id_for_slot_0()
		{
			var castle = NewCastle(id: 42);
			Assert.Equal(42, castle.GetData(0));
		}

		[Fact]
		public void GetData_returns_owner_economy_defense_triggers()
		{
			var castle = NewCastle();
			castle.OwnerGuildId = 7;
			castle.Economy = 30;
			castle.Defense = 12;
			castle.TriggerE = 1;
			castle.TriggerD = 0;

			Assert.Equal(7, castle.GetData(1));
			Assert.Equal(30, castle.GetData(2));
			Assert.Equal(12, castle.GetData(3));
			Assert.Equal(1, castle.GetData(4));
			Assert.Equal(0, castle.GetData(5));
		}

		[Fact]
		public void GetData_unpacks_treasure_bits_per_day_slot()
		{
			var castle = NewCastle();
			// Slots 10..17 mirror eAthena's per-day treasure flags;
			// bit N corresponds to slot 10+N.
			castle.TreasureFlags = 0b0000_0101; // bits 0 and 2 set
			Assert.Equal(1, castle.GetData(10));
			Assert.Equal(0, castle.GetData(11));
			Assert.Equal(1, castle.GetData(12));
			Assert.Equal(0, castle.GetData(13));
			Assert.Equal(0, castle.GetData(17));
		}

		[Fact]
		public void GetData_returns_zero_for_unknown_slot()
		{
			var castle = NewCastle();
			Assert.Equal(0, castle.GetData(999));
			Assert.Equal(0, castle.GetData(-1));
		}

		[Fact]
		public void SetData_clamps_economy_and_defense_to_max_investment()
		{
			var castle = NewCastle();
			Assert.True(castle.SetData(2, Castle.MaxInvestmentLevel + 50));
			Assert.True(castle.SetData(3, -10));

			Assert.Equal(Castle.MaxInvestmentLevel, castle.Economy);
			Assert.Equal(0, castle.Defense);
		}

		[Fact]
		public void SetData_normalises_trigger_flags_to_0_or_1()
		{
			var castle = NewCastle();
			Assert.True(castle.SetData(4, 99));
			Assert.True(castle.SetData(5, -3));
			Assert.Equal(1, castle.TriggerE);
			Assert.Equal(1, castle.TriggerD);

			Assert.True(castle.SetData(4, 0));
			Assert.Equal(0, castle.TriggerE);
		}

		[Fact]
		public void SetData_writes_treasure_bits_independently()
		{
			var castle = NewCastle();
			Assert.True(castle.SetData(10, 1));
			Assert.True(castle.SetData(12, 1));
			Assert.True(castle.SetData(15, 1));

			Assert.Equal(1, castle.GetData(10));
			Assert.Equal(0, castle.GetData(11));
			Assert.Equal(1, castle.GetData(12));
			Assert.Equal(1, castle.GetData(15));

			Assert.True(castle.SetData(12, 0));
			Assert.Equal(0, castle.GetData(12));
			// Other bits unchanged.
			Assert.Equal(1, castle.GetData(10));
			Assert.Equal(1, castle.GetData(15));
		}

		[Fact]
		public void SetData_returns_false_for_unknown_slot()
		{
			var castle = NewCastle();
			Assert.False(castle.SetData(999, 1));
			Assert.False(castle.SetData(6, 1));     // 6..9 are gaps in eA's slot table
		}

		[Fact]
		public void SetData_owner_is_unclamped()
		{
			var castle = NewCastle();
			Assert.True(castle.SetData(1, 12345));
			Assert.Equal(12345, castle.OwnerGuildId);
		}
	}
}
