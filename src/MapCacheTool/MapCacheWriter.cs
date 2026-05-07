using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Sabine.Shared.Data.Databases;

namespace Sabine.Tools.MapCache
{
	/// <summary>
	/// Writes a Sabine map cache .dat file in the binary format that
	/// <see cref="MapCacheDb"/> reads. Layout:
	///
	///   [4 bytes] magic ("MCAC")
	///   [int32  ] version
	///   [bool   ] compressed flag
	///   [int32  ] map count
	///   per map:
	///     [int32] blob length
	///     [bytes] blob (deflate-compressed when the flag is set)
	///       blob payload, after decompress:
	///         [string] stringId  (BinaryWriter 7-bit length-prefixed)
	///         [int32 ] width
	///         [int32 ] height
	///         [byte  ] type * width * height (row-major: outer y, inner x)
	/// </summary>
	public sealed class MapCacheWriter : IDisposable
	{
		private static readonly byte[] Magic = Encoding.ASCII.GetBytes("MCAC");
		private const int Version = 1;

		private readonly Stream _stream;
		private readonly BinaryWriter _writer;
		private readonly bool _compress;
		private readonly List<(byte[] Payload, int Length)> _entries = new();

		public MapCacheWriter(string path, bool compress)
		{
			_compress = compress;
			_stream = File.Create(path);
			_writer = new BinaryWriter(_stream);
		}

		public void AddMap(string stringId, int width, int height, TileType[,] tiles)
		{
			using var payloadStream = new MemoryStream();
			using (var bw = new BinaryWriter(payloadStream, Encoding.UTF8, leaveOpen: true))
			{
				bw.Write(stringId);
				bw.Write(width);
				bw.Write(height);
				for (var y = 0; y < height; y++)
				{
					for (var x = 0; x < width; x++)
						bw.Write((byte)tiles[x, y]);
				}
			}

			var raw = payloadStream.ToArray();

			if (_compress)
			{
				using var compressed = new MemoryStream();
				using (var ds = new DeflateStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
					ds.Write(raw, 0, raw.Length);
				raw = compressed.ToArray();
			}

			_entries.Add((raw, raw.Length));
		}

		public void Flush()
		{
			_writer.Write(Magic);
			_writer.Write(Version);
			_writer.Write(_compress);
			_writer.Write(_entries.Count);

			foreach (var (payload, length) in _entries)
			{
				_writer.Write(length);
				_writer.Write(payload);
			}

			_writer.Flush();
		}

		public void Dispose()
		{
			_writer.Dispose();
			_stream.Dispose();
		}
	}
}
