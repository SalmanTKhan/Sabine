namespace Sabine.Zone.World.Woe
{
	/// <summary>
	/// DTO mapping one row of the <c>castle_state</c> table. Used
	/// at startup load and on every <c>SetCastleData</c> upsert.
	/// </summary>
	public class CastleStateRow
	{
		public string MapStringId { get; set; }
		public int OwnerGuildId { get; set; }
		public int Economy { get; set; }
		public int Defense { get; set; }
		public int TriggerE { get; set; }
		public int TriggerD { get; set; }
		public int TreasureFlags { get; set; }
	}
}
