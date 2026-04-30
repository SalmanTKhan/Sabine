using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Sabine.Zone.World.Groups
{
	/// <summary>
	/// Minimal clan registry. Mirrors PartyManager's shape; only the data
	/// needed by @clan/@clanspy.
	/// </summary>
	public class ClanManager
	{
		public static ClanManager Instance { get; } = new ClanManager();

		private readonly ConcurrentDictionary<int, Clan> _clans = new();
		private int _nextId;

		public Clan Create(string name, int leaderCharId)
		{
			var clan = new Clan
			{
				Id = Interlocked.Increment(ref _nextId),
				Name = name,
				LeaderCharId = leaderCharId,
			};
			clan.Members.Add(leaderCharId);
			_clans[clan.Id] = clan;
			return clan;
		}

		public bool Destroy(int id) => _clans.TryRemove(id, out _);

		public Clan GetByName(string name)
			=> _clans.Values.FirstOrDefault(c => string.Equals(c.Name, name, System.StringComparison.OrdinalIgnoreCase));

		public IEnumerable<Clan> All => _clans.Values;
	}

	public class Clan
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int LeaderCharId { get; set; }
		public HashSet<int> Members { get; } = new();
	}
}
