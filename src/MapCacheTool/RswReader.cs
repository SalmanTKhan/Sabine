using System;
using System.IO;

namespace Sabine.Tools.MapCache
{
	/// <summary>
	/// Reads the water level out of a Gravity .rsw file. We don't need
	/// the rest of the file (lighting, model placements, terrain mesh)
	/// — only the float at offset 166, which is the absolute water
	/// height. Cells whose bottom-left corner sits below this height
	/// get remapped from walkable-ground to walkable-water in the cache.
	/// </summary>
	public static class RswReader
	{
		// rAthena uses 1000000 as the "no water" sentinel.
		public const float NoWater = 1000000f;

		// Offset of the water height float in the RSW header. Stable
		// across all client versions Sabine targets; rAthena uses the
		// same value (see grfio_read_rsw_water_level).
		private const int WaterHeightOffset = 166;

		public static float ReadWaterHeight(string path)
		{
			if (!File.Exists(path))
				return NoWater;

			var bytes = File.ReadAllBytes(path);
			if (bytes.Length < WaterHeightOffset + 4)
				return NoWater;

			return BitConverter.ToSingle(bytes, WaterHeightOffset);
		}
	}
}
