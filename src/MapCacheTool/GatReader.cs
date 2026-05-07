using System;
using System.IO;

namespace Sabine.Tools.MapCache
{
	/// <summary>
	/// Holds the raw cell grid extracted from a single .gat file before
	/// era remapping. <see cref="Heights"/> stores the bottom-left corner
	/// elevation per cell so callers can apply the RSW water-level
	/// remap themselves.
	/// </summary>
	public sealed class GatData
	{
		public int Width { get; init; }
		public int Height { get; init; }
		public byte[] Types { get; init; }
		public float[] Heights { get; init; }
	}

	/// <summary>
	/// Decodes Gravity .gat files. The format is well documented by
	/// rAthena; we mirror its layout: 14-byte header (6-byte magic,
	/// xs at offset 6, ys at offset 10) followed by 20-byte cell
	/// records (four float corner heights then a uint32 type).
	/// </summary>
	public static class GatReader
	{
		private const int HeaderSize = 14;
		private const int CellSize = 20;

		public static GatData Read(string path)
		{
			var bytes = File.ReadAllBytes(path);
			return Read(bytes);
		}

		public static GatData Read(byte[] bytes)
		{
			if (bytes.Length < HeaderSize)
				throw new InvalidDataException("GAT file is shorter than its header.");

			using var ms = new MemoryStream(bytes);
			using var br = new BinaryReader(ms);

			// Skip the 6-byte "GRAT" magic + version.
			br.ReadBytes(6);

			var xs = (int)br.ReadUInt32();
			var ys = (int)br.ReadUInt32();
			if (xs <= 0 || ys <= 0)
				throw new InvalidDataException($"GAT has non-positive dimensions ({xs}x{ys}).");

			var expected = HeaderSize + (long)xs * ys * CellSize;
			if (bytes.Length < expected)
				throw new InvalidDataException($"GAT cell payload truncated (expected {expected} bytes, got {bytes.Length}).");

			var count = xs * ys;
			var types = new byte[count];
			var heights = new float[count];

			for (var i = 0; i < count; i++)
			{
				heights[i] = br.ReadSingle();
				// Skip the other three corner heights.
				br.ReadSingle();
				br.ReadSingle();
				br.ReadSingle();
				var type = br.ReadUInt32();
				types[i] = (byte)type;
			}

			return new GatData
			{
				Width = xs,
				Height = ys,
				Types = types,
				Heights = heights,
			};
		}
	}
}
