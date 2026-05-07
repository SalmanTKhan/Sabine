using System.IO;

namespace Sabine.Tools.MapCache.Sources
{
	/// <summary>
	/// <see cref="IMapSource"/> backed by a directory of loose .gat /
	/// .rsw files (the layout produced when extracting maps from a GRF
	/// with GRFTool or GRFEditor). Files are looked up by case-
	/// insensitive name + extension.
	/// </summary>
	public sealed class LooseFileMapSource : IMapSource
	{
		private readonly string _directory;

		public LooseFileMapSource(string directory)
		{
			if (!Directory.Exists(directory))
				throw new DirectoryNotFoundException($"Map input directory not found: {directory}");

			_directory = directory;
		}

		public byte[] ReadGat(string mapName) => ReadOptional(mapName + ".gat");
		public byte[] ReadRsw(string mapName) => ReadOptional(mapName + ".rsw");

		private byte[] ReadOptional(string fileName)
		{
			var path = Path.Combine(_directory, fileName);
			if (!File.Exists(path))
				return null;
			return File.ReadAllBytes(path);
		}
	}
}
