using System.Collections.Generic;

namespace Sabine.Zone.World.Groups
{
	/// <summary>
	/// Plain record for a single `guilds` table row, plus its member
	/// rows. Returned by <c>ZoneDb.LoadAllGuilds</c> and consumed by
	/// <see cref="GuildManager.LoadAll"/> at startup; never used at
	/// runtime — operational state lives on <see cref="Guild"/>.
	/// </summary>
	public class GuildDbRow
	{
		public int GuildId { get; set; }
		public string Name { get; set; }
		public int MasterCharId { get; set; }
		public int Level { get; set; } = 1;
		public int ExpCurrent { get; set; }
		public int EmblemId { get; set; }
		public string Notice { get; set; } = string.Empty;
		public List<GuildMemberDbRow> Members { get; } = new();
	}

	/// <summary>
	/// One row of `guild_members`. The character's display name
	/// (`Guild.Members[i].Name`) is filled in lazily as players come
	/// online — the DB row only stores the foreign-key id and the
	/// position label.
	/// </summary>
	public class GuildMemberDbRow
	{
		public int CharacterId { get; set; }
		public bool IsMaster { get; set; }
		public string Position { get; set; } = string.Empty;
	}
}
