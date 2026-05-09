using System.Collections.Generic;

namespace Sabine.Zone.World.Battlegrounds
{
	/// <summary>
	/// One side of an active Battleground (Flavius, KVM, Tierra). Holds
	/// the joined member list and the team-level events the script
	/// registered when the team was created. The opposing side lives
	/// in its own <see cref="Battleground"/> instance — eAthena's
	/// scripts model each "team" as its own bg_id rather than a
	/// two-team match object, so we mirror that shape.
	/// </summary>
	public class Battleground
	{
		private static int _nextId = 1;
		private static readonly object _idLock = new();

		public int Id { get; }
		public string MapStringId { get; set; }
		public int RespawnX { get; set; }
		public int RespawnY { get; set; }
		public string OnQuitEvent { get; set; }
		public string OnDeathEvent { get; set; }
		public List<int> CharacterIds { get; } = new();

		/// <summary>Kills accumulated by this team during the active match.</summary>
		public int Score { get; set; }

		/// <summary>Target score to win — first to reach this triggers the end-of-match flow.</summary>
		public int WinScore { get; set; } = 10;

		/// <summary>
		/// The opposing team's id in this match. When set, kill events
		/// where killer ∈ this.CharacterIds and victim ∈ opponent.CharacterIds
		/// bump <see cref="Score"/>. Null on solo / spectator-only BGs.
		/// </summary>
		public int? OpponentId { get; set; }

		/// <summary>True once the score reaches WinScore — the listener stops counting after this flips.</summary>
		public bool MatchEnded { get; set; }

		/// <summary>Per-team zeny prize awarded to every member on win.</summary>
		public int RewardZeny { get; set; } = 5000;

		public Battleground()
		{
			lock (_idLock)
				this.Id = _nextId++;
		}
	}
}
