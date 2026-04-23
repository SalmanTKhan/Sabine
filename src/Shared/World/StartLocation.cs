using System.Collections.Generic;
using Sabine.Shared.Data.Databases;

namespace Sabine.Shared.World
{
	/// <summary>
	/// Resolves the default starting/save location for the currently running
	/// packet version.
	/// </summary>
	public static class StartLocation
	{
		private static readonly List<(string StringId, Position Position)> Fallbacks = new()
		{
			("prontera", new Position(156, 191)),
			("prt_vilg02", new Position(99, 81)),
		};

		/// <summary>
		/// Returns a default save/start location that is valid for the given
		/// maps database (i.e. loaded under the current packet version).
		/// Prefers the configured start location, then the version-appropriate
		/// hard-coded fallback. Returns false if nothing usable was found.
		/// </summary>
		public static bool TryGetDefault(MapsDb maps, string preferredStringId, Position preferredPosition, out Location location)
		{
			if (!string.IsNullOrWhiteSpace(preferredStringId) && maps.TryFind(preferredStringId, out var mapData))
			{
				location = new Location(mapData.Id, preferredPosition.X, preferredPosition.Y);
				return true;
			}

			foreach (var fallback in Fallbacks)
			{
				if (maps.TryFind(fallback.StringId, out var data))
				{
					location = new Location(data.Id, fallback.Position.X, fallback.Position.Y);
					return true;
				}
			}

			location = default;
			return false;
		}
	}
}
