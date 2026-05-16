using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Sabine.Shared.Const;
using Yggdrasil.Data.Binary;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// Runtime overlay flags that override the static
	/// <see cref="MapCacheTile.Type"/>. Used to mutate cell behavior at
	/// runtime via the script-facing <c>SetCell</c> command — walls,
	/// skill-zone barriers, basilica sanctuaries, etc. The flags are
	/// stored sparsely (only for cells that actually have an override),
	/// so memory cost stays proportional to the mutation count rather
	/// than the map area.
	/// </summary>
	[Flags]
	public enum CellFlags : ushort
	{
		None = 0,

		/// <summary>Force the cell walkable, regardless of underlying tile.</summary>
		AllowWalk = 1 << 0,

		/// <summary>Block walking on the cell.</summary>
		BlockWalk = 1 << 1,

		/// <summary>Force the cell to allow projectiles through.</summary>
		AllowShoot = 1 << 2,

		/// <summary>Block projectiles from passing through.</summary>
		BlockShoot = 1 << 3,

		/// <summary>Cell is marked as water (set/clear toggle).</summary>
		Water = 1 << 4,

		/// <summary>Cell is NPC-only (blocks PCs but not projectiles).</summary>
		Npc = 1 << 5,

		/// <summary>Basilica sanctuary cell.</summary>
		Basilica = 1 << 6,

		/// <summary>Land Protector cell (skill-blocking).</summary>
		LandProtector = 1 << 7,

		/// <summary>No vending allowed on this cell.</summary>
		NoVending = 1 << 8,

		/// <summary>No chat rooms allowed on this cell.</summary>
		NoChat = 1 << 9,

		/// <summary>Ice Wall cell.</summary>
		IceWall = 1 << 10,

		/// <summary>No skills usable on this cell.</summary>
		NoSkill = 1 << 11,
	}

	/// <summary>
	/// Represents the cache data of a map.
	/// </summary>
	public class MapCacheData
	{
		public string StringId { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public MapCacheTile[,] Tiles { get; set; }

		// Sparse runtime cell-flag overlay. Only populated for cells
		// that were explicitly mutated via SetCell / SetWall.
		private readonly Dictionary<long, CellFlags> _overrides = new();
		private readonly object _overridesLock = new();

		private static long Key(int x, int y) => ((long)y << 32) | (uint)x;

		/// <summary>
		/// Returns the runtime override flags for the given cell.
		/// <see cref="CellFlags.None"/> means the cell has no overrides
		/// and the underlying <see cref="MapCacheTile.Type"/> governs
		/// behavior.
		/// </summary>
		public CellFlags GetOverrideFlags(int x, int y)
		{
			lock (_overridesLock)
				return _overrides.TryGetValue(Key(x, y), out var f) ? f : CellFlags.None;
		}

		/// <summary>
		/// Sets or clears a runtime cell override. Mirrors rAthena's
		/// <c>setcell</c> single-cell form. Walkable / Shootable use
		/// paired bits (Allow / Block) so explicit forced-state survives
		/// any later toggle of the opposite direction; single-bit flags
		/// (Water, Npc, Basilica, …) just set or clear one bit.
		/// </summary>
		public void SetCell(int x, int y, CellType type, bool value)
		{
			if (!this.IsPositionOnMap(x, y))
				return;

			var (set, clear) = TypeToFlags(type, value);
			lock (_overridesLock)
			{
				_overrides.TryGetValue(Key(x, y), out var current);
				current = (current | set) & ~clear;
				if (current == CellFlags.None)
					_overrides.Remove(Key(x, y));
				else
					_overrides[Key(x, y)] = current;
			}
		}

		/// <summary>
		/// Mutates every cell in the inclusive rectangle
		/// [x1..x2, y1..y2]. Mirrors rAthena's <c>setcell</c>
		/// rectangle form.
		/// </summary>
		public void SetCellRect(int x1, int y1, int x2, int y2, CellType type, bool value)
		{
			if (x1 > x2) (x1, x2) = (x2, x1);
			if (y1 > y2) (y1, y2) = (y2, y1);
			for (var x = x1; x <= x2; x++)
				for (var y = y1; y <= y2; y++)
					this.SetCell(x, y, type, value);
		}

		private static (CellFlags set, CellFlags clear) TypeToFlags(CellType type, bool value)
		{
			switch (type)
			{
				case CellType.Walkable:
					return value
						? (CellFlags.AllowWalk, CellFlags.BlockWalk)
						: (CellFlags.BlockWalk, CellFlags.AllowWalk);
				case CellType.Shootable:
					return value
						? (CellFlags.AllowShoot, CellFlags.BlockShoot)
						: (CellFlags.BlockShoot, CellFlags.AllowShoot);
				case CellType.Water:
					return value ? (CellFlags.Water, CellFlags.None) : (CellFlags.None, CellFlags.Water);
				case CellType.Npc:
					return value ? (CellFlags.Npc, CellFlags.None) : (CellFlags.None, CellFlags.Npc);
				case CellType.Basilica:
					return value ? (CellFlags.Basilica, CellFlags.None) : (CellFlags.None, CellFlags.Basilica);
				case CellType.LandProtector:
					return value ? (CellFlags.LandProtector, CellFlags.None) : (CellFlags.None, CellFlags.LandProtector);
				case CellType.NoVending:
					return value ? (CellFlags.NoVending, CellFlags.None) : (CellFlags.None, CellFlags.NoVending);
				case CellType.NoChat:
					return value ? (CellFlags.NoChat, CellFlags.None) : (CellFlags.None, CellFlags.NoChat);
				case CellType.IceWall:
					return value ? (CellFlags.IceWall, CellFlags.None) : (CellFlags.None, CellFlags.IceWall);
				case CellType.NoSkill:
					return value ? (CellFlags.NoSkill, CellFlags.None) : (CellFlags.None, CellFlags.NoSkill);
				default:
					return (CellFlags.None, CellFlags.None);
			}
		}

		/// <summary>
		/// Returns true if the given position is within the bounds of the
		/// map.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public bool IsPositionOnMap(int x, int y)
		{
			return x >= 0 && x < this.Width && y >= 0 && y < this.Height;
		}

		/// <summary>
		/// Returns true if the given tile can be walked on.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public bool IsPassable(int x, int y)
		{
			if (!this.IsPositionOnMap(x, y))
				return false;

			var ovr = this.GetOverrideFlags(x, y);
			if ((ovr & CellFlags.BlockWalk) != 0) return false;
			if ((ovr & CellFlags.AllowWalk) != 0) return true;
			return this.Tiles[x, y].IsWalkable;
		}

		/// <summary>
		/// Returns true if the given tile can be walked on.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public bool IsUnpassable(int x, int y)
		{
			if (!this.IsPositionOnMap(x, y))
				return false;

			return !this.IsPassable(x, y);
		}

		/// <summary>
		/// Returns true if projectiles can pass through the given cell.
		/// Defaults to <see cref="IsPassable"/> when no Shootable
		/// override is set.
		/// </summary>
		public bool IsShootable(int x, int y)
		{
			if (!this.IsPositionOnMap(x, y))
				return false;

			var ovr = this.GetOverrideFlags(x, y);
			if ((ovr & CellFlags.BlockShoot) != 0) return false;
			if ((ovr & CellFlags.AllowShoot) != 0) return true;
			return this.IsPassable(x, y);
		}

		/// <summary>
		/// Returns true if the given tile is a water tile (TileType.Water).
		/// Used by Aqua Benedicta and any future water-cell-dependent skill.
		/// </summary>
		public bool IsWater(int x, int y)
		{
			if (!this.IsPositionOnMap(x, y))
				return false;

			return this.Tiles[x, y].Type == TileType.Water;
		}
	}

	/// <summary>
	/// Represents one tile on a map.
	/// </summary>
	public class MapCacheTile
	{
		public int X { get; set; }
		public int Y { get; set; }
		public TileType Type { get; set; }

		public bool IsWalkable => this.Type != TileType.Unpassable;

		public MapCacheTile(int x, int y, TileType type)
		{
			this.X = x;
			this.Y = y;
			this.Type = type;
		}
	}

	/// <summary>
	/// Defines a tile's type, as stored in Sabine's map cache. Values
	/// match the raw GAT cell-type bytes used by Gravity's client. See
	/// docs/map-cache-tool.md and the openkore / rdw-archive field-file
	/// references for the full table.
	/// </summary>
	/// <remarks>
	/// Alpha and beta clients interpret type 5 as a walkable
	/// constructed floor (towns, buildings); modern (RE) clients
	/// repurpose it as a non-walkable snipable cliff. Sabine targets
	/// alpha/beta semantics, so <see cref="Passable"/> remains
	/// walkable here.
	/// </remarks>
	public enum TileType
	{
		/// <summary>
		/// Walkable natural ground (fields, dungeons). Distinct from
		/// <see cref="Passable"/>, which alpha/beta clients use for
		/// man-made surfaces like town floors.
		/// </summary>
		Walkable = 0,

		/// <summary>
		/// Characters can't walk on this tile (walls, cliffs).
		/// </summary>
		Unpassable = 1,

		/// <summary>
		/// Water cell. Walkable but slow; used by water-dependent
		/// skills like Aqua Benedicta.
		/// </summary>
		Water = 2,

		/// <summary>
		/// Walkable constructed floor (towns, buildings) in
		/// alpha/beta clients. Modern clients collapse this onto
		/// <see cref="Walkable"/>.
		/// </summary>
		Passable = 5,
	}

	/// <summary>
	/// A map cache database, holding detailed information about a map's
	/// terrain.
	/// </summary>
	public class MapCacheDb : DatabaseBinaryIndexed<string, MapCacheData>
	{
		/// <summary>
		/// Called to read the binary database from a file.
		/// </summary>
		/// <param name="fs"></param>
		protected override void Read(FileStream fs)
		{
			using (var brfs = new BinaryReader(fs))
			{
				var header = brfs.ReadBytes(4);
				var version = brfs.ReadInt32();
				var compressed = brfs.ReadBoolean();
				var count = brfs.ReadInt32();

				for (var i = 0; i < count; ++i)
				{
					var data = new MapCacheData();

					var length = brfs.ReadInt32();
					var bytes = brfs.ReadBytes(length);

					if (compressed)
					{
						using (var msCompressed = new MemoryStream(bytes))
						using (var msUncompressed = new MemoryStream())
						using (var ds = new DeflateStream(msCompressed, CompressionMode.Decompress, true))
						{
							ds.CopyTo(msUncompressed);
							bytes = msUncompressed.ToArray();
						}
					}

					using (var ms = new MemoryStream(bytes))
					using (var brms = new BinaryReader(ms))
					{
						data.StringId = brms.ReadString();
						data.Width = brms.ReadInt32();
						data.Height = brms.ReadInt32();

						data.Tiles = new MapCacheTile[data.Width, data.Height];
						for (var y = 0; y < data.Height; ++y)
						{
							for (var x = 0; x < data.Width; ++x)
							{
								var type = (TileType)brms.ReadByte();
								data.Tiles[x, y] = new MapCacheTile(x, y, type);
							}
						}

						this.AddOrReplace(data.StringId, data);
					}
				}
			}
		}
	}
}
