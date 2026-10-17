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
		private readonly HashSet<string> _reservedMaps = new(System.StringComparer.OrdinalIgnoreCase);

		public bool Reserve(string mapStringId)
		{
			if (string.IsNullOrWhiteSpace(mapStringId)) return false;
			lock (_syncLock) return _reservedMaps.Add(mapStringId);
		}

		public bool Unbook(string mapStringId)
		{
			if (string.IsNullOrWhiteSpace(mapStringId)) return false;
			lock (_syncLock) return _reservedMaps.Remove(mapStringId);
		}

		public bool IsReserved(string mapStringId)
		{
			lock (_syncLock) return _reservedMaps.Contains(mapStringId);
		}

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

		/// <summary>
		/// Returns the Battleground a character is currently a member of,
		/// or null if they aren't in any BG. Walks the team list — fine
		/// for the alpha-era profile where active match counts are
		/// small. If BGs ever get hot, we'll graft an inverted index on
		/// CharacterId → BG.
		/// </summary>
		public Battleground GetByMember(int charId)
		{
			lock (_syncLock)
			{
				foreach (var t in _teams.Values)
					if (t.CharacterIds.Contains(charId))
						return t;
				return null;
			}
		}

		/// <summary>
		/// True when both characters are in different Battlegrounds —
		/// the canonical "PvP-eligible by team membership" rule.
		/// Self-pairings, same-team pairings, and unaffiliated players
		/// all return false. Mirrors eAthena's bg_check enemy / friend
		/// gating that <c>battle_check_target</c> threads through for
		/// every PvP attempt.
		/// </summary>
		public bool AreOpponents(int charIdA, int charIdB)
		{
			if (charIdA == charIdB) return false;

			var a = this.GetByMember(charIdA);
			var b = this.GetByMember(charIdB);
			if (a == null || b == null) return false;

			return a.Id != b.Id;
		}
	}
}
