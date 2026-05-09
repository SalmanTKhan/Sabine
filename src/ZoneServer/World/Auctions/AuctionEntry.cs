using System;

namespace Sabine.Zone.World.Auctions
{
	/// <summary>
	/// Lifecycle state of an auction row. Mirrors the `status` column
	/// in `sql/updates/2026-05-09_001.sql`.
	/// </summary>
	public enum AuctionStatus : byte
	{
		Open = 0,
		Sold = 1,
		Expired = 2,
		Cancelled = 3,
	}

	/// <summary>
	/// One persisted auction. The item is snapshot at sell time using
	/// the same fields as <see cref="Mail.MailMessage"/> so payouts
	/// can hand them straight to <c>MailManager.Send</c>.
	/// </summary>
	public class AuctionEntry
	{
		public int Id { get; set; }
		public int SellerId { get; set; }
		public string SellerName { get; set; } = string.Empty;

		public int ItemClassId { get; set; }
		public short ItemAmount { get; set; } = 1;
		public byte ItemRefine { get; set; }
		public bool ItemIdentified { get; set; } = true;
		public bool ItemDamaged { get; set; }

		public int StartingPrice { get; set; }
		public int BuyoutPrice { get; set; }
		public int CurrentBid { get; set; }
		public int BidderId { get; set; }
		public string BidderName { get; set; } = string.Empty;

		public AuctionStatus Status { get; set; } = AuctionStatus.Open;
		public DateTime CreatedAt { get; set; }
		public DateTime Deadline { get; set; }

		public bool HasBid => this.BidderId > 0 && this.CurrentBid > 0;
	}
}
