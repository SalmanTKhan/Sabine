using System;

namespace Sabine.Tools.MapCache.Sources
{
	/// <summary>
	/// Placeholder for reading maps directly out of a Gravity .grf
	/// archive. The CLI surface (<c>--grf</c> / <c>--grf-list</c>) is
	/// reserved so callers don't have to migrate later, but the
	/// implementation isn't here yet — extract maps with GRFTool or
	/// GRFEditor and use <c>--input-dir</c> for now.
	/// </summary>
	public sealed class GrfMapSource : IMapSource
	{
		public GrfMapSource(string grfListPath)
		{
			throw new NotImplementedException(
				"GRF input is not yet implemented; extract maps with GRFTool/GRFEditor and use --input-dir for now.");
		}

		public byte[] ReadGat(string mapName) => throw new NotImplementedException();
		public byte[] ReadRsw(string mapName) => throw new NotImplementedException();
	}
}
