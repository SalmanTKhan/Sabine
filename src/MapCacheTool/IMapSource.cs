namespace Sabine.Tools.MapCache
{
	/// <summary>
	/// Reads the raw .gat / .rsw bytes for a named map. Loose-file and
	/// GRF-archive implementations both satisfy this interface so the
	/// rest of the tool doesn't care where the data came from.
	/// </summary>
	public interface IMapSource
	{
		/// <summary>
		/// Returns the .gat file bytes for the given map name (no
		/// extension), or null if the map isn't present.
		/// </summary>
		byte[] ReadGat(string mapName);

		/// <summary>
		/// Returns the .rsw file bytes for the given map name, or null
		/// if the map isn't present. Callers that only need the water
		/// level can pass the bytes through <see cref="RswReader"/>.
		/// </summary>
		byte[] ReadRsw(string mapName);
	}
}
