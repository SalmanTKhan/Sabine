using System.Collections.Generic;
using System.Linq;

namespace Sabine.Zone.World.Battlegrounds
{
	/// <summary>
	/// In-memory registry of active Battleground teams. Mirrors the
	/// runtime side of eAthena's bg_create / bg_destroy lifecycle —
	/// no persistence (matches end on shutdown).
	/// </summary>
	public class BattlegroundManager
	{
		private readonly object _syncLock = new();
		private readonly Dictionary<int, Battleground> _teams = new();

		public Battleground Create(string map, int x, int y, string onQuit, string onDeath)
		{
			var team = new Battleground
			{
				MapStringId = map,
				RespawnX = x,
				RespawnY = y,
				OnQuitEvent = onQuit,
				OnDeathEvent = onDeath,
			};
			lock (_syncLock)
				_teams[team.Id] = team;
			return team;
		}

		public Battleground Get(int id)
		{
			lock (_syncLock)
				return _teams.TryGetValue(id, out var t) ? t : null;
		}

		public bool Destroy(int id)
		{
			lock (_syncLock)
				return _teams.Remove(id);
		}

		public IReadOnlyList<Battleground> All
		{
			get { lock (_syncLock) return _teams.Values.ToArray(); }
		}
	}
}
