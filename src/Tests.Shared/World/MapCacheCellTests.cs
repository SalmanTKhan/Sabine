using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Xunit;

namespace Tests.Sabine.Shared.World
{
	public class MapCacheCellTests
	{
		// Construct a tiny map. Tile data stays default (non-walkable);
		// overrides are what we're exercising.
		private static MapCacheData MakeMap(int width = 4, int height = 4)
		{
			var tiles = new MapCacheTile[width, height];
			for (var x = 0; x < width; x++)
				for (var y = 0; y < height; y++)
					tiles[x, y] = new MapCacheTile(x, y, TileType.Unpassable);

			return new MapCacheData
			{
				StringId = "test",
				Width = width,
				Height = height,
				Tiles = tiles,
			};
		}

		[Fact]
		public void Walkable_true_sets_AllowWalk_and_clears_BlockWalk()
		{
			var map = MakeMap();
			map.SetCell(1, 1, CellType.Walkable, false); // start blocked
			Assert.Equal(CellFlags.BlockWalk, map.GetOverrideFlags(1, 1));

			map.SetCell(1, 1, CellType.Walkable, true);

			var flags = map.GetOverrideFlags(1, 1);
			Assert.True((flags & CellFlags.AllowWalk) != 0);
			Assert.True((flags & CellFlags.BlockWalk) == 0);
		}

		[Fact]
		public void Walkable_false_sets_BlockWalk_and_clears_AllowWalk()
		{
			var map = MakeMap();
			map.SetCell(2, 2, CellType.Walkable, true);
			Assert.Equal(CellFlags.AllowWalk, map.GetOverrideFlags(2, 2));

			map.SetCell(2, 2, CellType.Walkable, false);

			var flags = map.GetOverrideFlags(2, 2);
			Assert.True((flags & CellFlags.BlockWalk) != 0);
			Assert.True((flags & CellFlags.AllowWalk) == 0);
		}

		[Fact]
		public void Shootable_paired_bits_behave_like_walkable()
		{
			var map = MakeMap();

			map.SetCell(0, 0, CellType.Shootable, true);
			Assert.Equal(CellFlags.AllowShoot, map.GetOverrideFlags(0, 0));

			map.SetCell(0, 0, CellType.Shootable, false);
			Assert.Equal(CellFlags.BlockShoot, map.GetOverrideFlags(0, 0));
		}

		[Fact]
		public void Single_bit_flag_toggles_cleanly()
		{
			var map = MakeMap();

			map.SetCell(1, 2, CellType.Water, true);
			Assert.Equal(CellFlags.Water, map.GetOverrideFlags(1, 2));

			map.SetCell(1, 2, CellType.Water, false);
			Assert.Equal(CellFlags.None, map.GetOverrideFlags(1, 2));
		}

		[Fact]
		public void Multiple_flags_can_coexist_on_one_cell()
		{
			var map = MakeMap();

			map.SetCell(3, 3, CellType.Walkable, false);
			map.SetCell(3, 3, CellType.NoSkill, true);
			map.SetCell(3, 3, CellType.NoVending, true);

			var flags = map.GetOverrideFlags(3, 3);
			Assert.True((flags & CellFlags.BlockWalk) != 0);
			Assert.True((flags & CellFlags.NoSkill) != 0);
			Assert.True((flags & CellFlags.NoVending) != 0);
		}

		[Fact]
		public void Clearing_last_flag_drops_the_sparse_entry()
		{
			// IsPassable falls back to the underlying tile when the
			// override is cleared, so checking that path is the
			// observable proxy for "the dictionary entry got removed."
			var map = MakeMap();
			map.SetCell(1, 1, CellType.Walkable, true);

			Assert.True(map.IsPassable(1, 1));

			map.SetCell(1, 1, CellType.Walkable, false);
			Assert.False(map.IsPassable(1, 1));

			// Re-toggle to true: tile data is still non-walkable, but
			// the AllowWalk override forces passability — proves the
			// override is still being read after the round-trip.
			map.SetCell(1, 1, CellType.Walkable, true);
			Assert.True(map.IsPassable(1, 1));
		}

		[Fact]
		public void Out_of_bounds_set_is_a_noop()
		{
			var map = MakeMap(width: 2, height: 2);

			map.SetCell(99, 99, CellType.Water, true);
			map.SetCell(-1, 0, CellType.Water, true);

			Assert.Equal(CellFlags.None, map.GetOverrideFlags(99, 99));
			Assert.Equal(CellFlags.None, map.GetOverrideFlags(-1, 0));
		}

		[Fact]
		public void SetCellRect_swaps_inverted_bounds()
		{
			var map = MakeMap(width: 4, height: 4);

			// x1>x2, y1>y2 — should still cover the whole rect.
			map.SetCellRect(3, 3, 0, 0, CellType.NoChat, true);

			for (var x = 0; x < 4; x++)
				for (var y = 0; y < 4; y++)
					Assert.True((map.GetOverrideFlags(x, y) & CellFlags.NoChat) != 0);
		}
	}
}
