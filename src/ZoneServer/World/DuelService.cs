using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World
{
	/// <summary>
	/// Tracks active duels. A duel has an owner, a set of accepted
	/// members, and a set of pending invitations. PvP routing through
	/// duels is not wired up; the registry exists so commands have a
	/// target to mutate.
	/// </summary>
	public class DuelService
	{
		public static DuelService Instance { get; } = new DuelService();

		private readonly ConcurrentDictionary<int, Duel> _duels = new();
		private int _nextId = 1;

		public Duel Create(PlayerCharacter owner)
		{
			var duel = new Duel
			{
				Id = System.Threading.Interlocked.Increment(ref _nextId),
				OwnerCharId = owner.Id,
			};

			duel.Members.Add(owner.Id);
			_duels[duel.Id] = duel;
			return duel;
		}

		public Duel GetByOwner(int charId)
			=> _duels.Values.FirstOrDefault(d => d.OwnerCharId == charId);

		public Duel GetByMember(int charId)
			=> _duels.Values.FirstOrDefault(d => d.Members.Contains(charId));

		public bool Disband(int duelId)
			=> _duels.TryRemove(duelId, out _);
	}

	public class Duel
	{
		public int Id { get; set; }
		public int OwnerCharId { get; set; }
		public HashSet<int> Members { get; } = new();
		public HashSet<int> Invited { get; } = new();
	}
}
