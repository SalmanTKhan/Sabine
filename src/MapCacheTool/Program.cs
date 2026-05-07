using System;
using System.Collections.Generic;
using System.IO;
using Sabine.Shared.Data.Databases;
using Sabine.Tools.MapCache.Sources;

namespace Sabine.Tools.MapCache
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			if (args.Length == 0 || HasFlag(args, "--help") || HasFlag(args, "-h"))
			{
				PrintUsage();
				return 0;
			}

			try
			{
				var options = ParseOptions(args);
				return Run(options);
			}
			catch (NotImplementedException ex)
			{
				Console.Error.WriteLine($"error: {ex.Message}");
				return 2;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"error: {ex.Message}");
				return 1;
			}
		}

		private static int Run(Options options)
		{
			IMapSource source;
			if (options.GrfList != null || options.Grf != null)
				source = new GrfMapSource(options.GrfList ?? options.Grf);
			else
				source = new LooseFileMapSource(options.InputDir);

			var era = options.Era ?? EraTileMapping.InferFromFileName(Path.GetFileName(options.Output));
			var table = EraTileMapping.BuildTable(era);

			var maps = ReadMapList(options.MapList);
			Console.WriteLine($"Building {options.Output} (era={era}, {maps.Count} maps requested, compress={!options.NoCompress})");

			var outputDir = Path.GetDirectoryName(Path.GetFullPath(options.Output));
			if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			using var writer = new MapCacheWriter(options.Output, !options.NoCompress);

			var added = 0;
			var missing = 0;

			foreach (var name in maps)
			{
				var gatBytes = source.ReadGat(name);
				if (gatBytes == null)
				{
					Console.Error.WriteLine($"  miss   {name} (no .gat)");
					missing++;
					continue;
				}

				GatData gat;
				try
				{
					gat = GatReader.Read(gatBytes);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"  fail   {name}: {ex.Message}");
					missing++;
					continue;
				}

				var rswBytes = source.ReadRsw(name);
				var waterHeight = RswReader.NoWater;
				if (rswBytes != null && rswBytes.Length >= 170)
					waterHeight = BitConverter.ToSingle(rswBytes, 166);

				var tiles = BuildTiles(gat, table, waterHeight);
				writer.AddMap(name, gat.Width, gat.Height, tiles);
				added++;
				Console.WriteLine($"  ok     {name} {gat.Width}x{gat.Height}");
			}

			writer.Flush();
			Console.WriteLine($"Done. {added} cached, {missing} missing.");
			return missing == 0 ? 0 : 3;
		}

		private static TileType[,] BuildTiles(GatData gat, TileType[] table, float waterHeight)
		{
			var tiles = new TileType[gat.Width, gat.Height];
			var i = 0;
			for (var y = 0; y < gat.Height; y++)
			{
				for (var x = 0; x < gat.Width; x++, i++)
				{
					var raw = gat.Types[i];

					// Walkable ground that sits below the RSW water
					// surface gets remapped to walkable water (matches
					// rAthena's mapcache.cpp:107).
					if (waterHeight != RswReader.NoWater
						&& EraTileMapping.IsWalkableGround(raw)
						&& gat.Heights[i] > waterHeight)
					{
						tiles[x, y] = EraTileMapping.WalkableWater;
					}
					else
					{
						tiles[x, y] = table[raw];
					}
				}
			}

			return tiles;
		}

		private static List<string> ReadMapList(string path)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException($"Map list not found: {path}");

			var maps = new List<string>();
			foreach (var raw in File.ReadAllLines(path))
			{
				var line = raw.Trim();
				if (line.Length == 0 || line.StartsWith("//"))
					continue;

				var name = line;
				var dotGat = name.IndexOf(".gat", StringComparison.OrdinalIgnoreCase);
				if (dotGat >= 0)
					name = name.Substring(0, dotGat);

				maps.Add(name);
			}
			return maps;
		}

		private sealed class Options
		{
			public string InputDir;
			public string Grf;
			public string GrfList;
			public string MapList;
			public string Output;
			public Era? Era;
			public bool NoCompress;
		}

		private static Options ParseOptions(string[] args)
		{
			var o = new Options();
			for (var i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "--input-dir": o.InputDir = args[++i]; break;
					case "--grf": o.Grf = args[++i]; break;
					case "--grf-list": o.GrfList = args[++i]; break;
					case "--map-list": o.MapList = args[++i]; break;
					case "--output": o.Output = args[++i]; break;
					case "--era": o.Era = ParseEra(args[++i]); break;
					case "--no-compress": o.NoCompress = true; break;
					case "--rebuild": /* default behavior */ break;
					default:
						throw new ArgumentException($"unknown argument: {args[i]}");
				}
			}

			if (o.MapList == null)
				throw new ArgumentException("--map-list is required");
			if (o.Output == null)
				throw new ArgumentException("--output is required");
			if (o.InputDir == null && o.Grf == null && o.GrfList == null)
				throw new ArgumentException("one of --input-dir, --grf, --grf-list is required");
			if (o.InputDir != null && (o.Grf != null || o.GrfList != null))
				throw new ArgumentException("--input-dir is mutually exclusive with --grf / --grf-list");

			return o;
		}

		private static Era ParseEra(string value) => value.ToLowerInvariant() switch
		{
			"alpha" => Era.Alpha,
			"beta" => Era.Beta,
			"modern" or "renewal" or "re" => Era.Modern,
			_ => throw new ArgumentException($"unknown era: {value} (expected alpha|beta|modern)"),
		};

		private static bool HasFlag(string[] args, string name)
		{
			foreach (var a in args)
				if (a == name) return true;
			return false;
		}

		private static void PrintUsage()
		{
			Console.WriteLine(@"MapCacheTool — generates Sabine map_cache_*.dat files.

Usage:
  MapCacheTool --input-dir <path> --map-list <file> --output <file> [--era alpha|beta|modern] [--no-compress]
  MapCacheTool --grf <archive>   --map-list <file> --output <file> [--era ...]   (NOT YET IMPLEMENTED)

Required:
  --map-list <file>     text file with one map name per line; '//' = comment
  --output   <file>     destination, e.g. system/data/map_cache_200.dat

Source (one of):
  --input-dir <dir>     directory containing loose .gat and .rsw files
  --grf       <file>    .grf archive (reserved; not yet implemented)
  --grf-list  <file>    rAthena-style grf-files.txt (reserved; not yet implemented)

Options:
  --era       <name>    tile-type mapping (alpha|beta|modern). Default is
                        inferred from the output filename suffix
                        (_100 -> alpha, _200 -> beta, otherwise modern).
  --no-compress         write blobs uncompressed (useful for diffing).
  --rebuild             accepted for parity with rA's flag; tool always
                        rebuilds from scratch.

Tip: extract maps from a client GRF with GRFTool or GRFEditor into a
folder and point --input-dir at it.");
		}
	}
}
