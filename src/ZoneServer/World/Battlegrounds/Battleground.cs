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

		public Battleground()
		{
			lock (_idLock)
				this.Id = _nextId++;
		}
	}
}
