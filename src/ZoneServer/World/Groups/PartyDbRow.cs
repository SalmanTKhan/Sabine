using System.Collections.Generic;

namespace Sabine.Zone.World.Groups
{
	/// <summary>
	/// Plain record for a single `parties` table row, plus its member
	/// rows. Returned by <c>ZoneDb.LoadAllParties</c> and consumed by
	/// <see cref="PartyManager.LoadAll"/> at startup; never used at
	/// runtime — operational state lives on <see cref="Party"/>.
	/// </summary>
	public class PartyDbRow
	{
		public int PartyId { get; set; }
		public string Name { get; set; }
		public bool ExpShare { get; set; }
		public bool ItemShare { get; set; }
		public int LeaderCharId { get; set; }
		public List<PartyMemberDbRow> Members { get; } = new();
	}

	/// <summary>
	/// One row of `party_members`. The character's display name is
	/// joined in from `characters` at load time so the offline-
	/// placeholder roster has something usable to show until the
	/// player logs in.
	/// </summary>
	public class PartyMemberDbRow
	{
		public int CharacterId { get; set; }
		public string Name { get; set; } = string.Empty;
		public bool IsLeader { get; set; }
	}
}
