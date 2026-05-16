using System;
using Sabine.Shared.Data.Databases;

namespace Sabine.Tools.MapCache
{
	/// <summary>
	/// Identifies which client era's tile semantics to emit. Different
	/// client generations interpreted the GAT type field differently
	/// (most visibly: alpha and beta clients distinguish type 5 "town
	/// floor" from type 0 "field ground", while later clients collapse
	/// both onto type 0).
	/// </summary>
	public enum Era
	{
		Alpha,
		Beta,
		Modern,
	}

	/// <summary>
	/// Maps a raw GAT cell type byte (as stored in client .gat files) to
	/// the <see cref="TileType"/> Sabine writes into the map cache. The
	/// table is era-specific because alpha/beta clients preserved the
	/// {0, 5} distinction while later clients did not.
	/// </summary>
	public static class EraTileMapping
	{
		// Walkable-water destination — used when the GAT marks a cell
		// walkable but its bottom-left corner is below the RSW water
		// level. We map all eras to Water because Sabine only models a
		// single water flag (TileType.Water / IsWater).
		public const TileType WalkableWater = TileType.Water;

		/// <summary>
		/// Returns a 256-entry lookup table mapping a raw GAT type byte
		/// to a Sabine <see cref="TileType"/>. Unknown types default to
		/// <see cref="TileType.Unpassable"/>.
		/// </summary>
		public static TileType[] BuildTable(Era era)
		{
			var table = new TileType[256];
			for (var i = 0; i < table.Length; i++)
				table[i] = TileType.Unpassable;

			switch (era)
			{
				case Era.Alpha:
				case Era.Beta:
					// Alpha and beta clients preserve the {0, 5}
					// distinction: 0 is field ground, 5 is town /
					// constructed-floor. The hand-authored map_cache_*
					// files keep this split.
					table[0] = TileType.Walkable;
					table[1] = TileType.Unpassable;
					table[2] = TileType.Water;
					table[3] = WalkableWater;
					table[5] = TileType.Passable;
					table[6] = TileType.Passable;
					break;

				case Era.Modern:
					// Modern (RE) clients collapse type 5 onto type 0.
					table[0] = TileType.Walkable;
					table[1] = TileType.Unpassable;
					table[2] = TileType.Water;
					table[3] = WalkableWater;
					table[5] = TileType.Walkable;
					table[6] = TileType.Walkable;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(era), era, null);
			}

			return table;
		}

		/// <summary>
		/// Returns true if the given GAT raw type is a walkable-ground
		/// cell that should be remapped to walkable-water when the cell
		/// height is below the RSW water level.
		/// </summary>
		public static bool IsWalkableGround(byte rawType)
		{
			return rawType == 0 || rawType == 5 || rawType == 6;
		}

		/// <summary>
		/// Picks an era from an output filename like "map_cache_200.dat".
		/// _100 → Alpha, _200 → Beta, anything else → Modern.
		/// </summary>
		public static Era InferFromFileName(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
				return Era.Modern;

			var lower = fileName.ToLowerInvariant();
			if (lower.Contains("_100"))
				return Era.Alpha;
			if (lower.Contains("_200"))
				return Era.Beta;
			return Era.Modern;
		}
	}
}
