using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Mail;
using Yggdrasil.Logging;

namespace Sabine.Zone.World.Auctions
{
	/// <summary>
	/// Active auction registry (Slice G v1, text-mode). State is
	/// DB-backed (see <c>sql/updates/2026-05-09_001.sql</c>). Mutations
	/// (register / bid / cancel / resolve) write through to disk.
	/// A 30s poll resolves any auction whose <see cref="AuctionEntry.Deadline"/>
	/// has passed.
	/// </summary>
	/// <remarks>
	/// Payouts (item to winner, zeny to seller, refunds to outbid
	/// bidders) are delivered via <see cref="MailManager"/> so the
	/// recipient picks them up at their leisure — no online check
	/// required. Mirrors eAthena's <c>auction.c</c> data model.
	/// </remarks>
	public class AuctionManager
	{
		public static AuctionManager Instance { get; } = new AuctionManager();

		private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

		private readonly object _syncRoot = new();
		private readonly Dictionary<int, AuctionEntry> _open = new();
		private Timer _timer;
		private bool _loaded;

		/// <summary>
		/// Register result for <see cref="Register"/>.
		/// </summary>
		public enum RegisterResult : byte
		{
			Success = 0,
			InvalidPrice = 1,
			InvalidDuration = 2,
		}

		/// <summary>
		/// Bid result for <see cref="Bid"/>.
		/// </summary>
		public enum BidResult : byte
		{
			Success = 0,
			NotFound = 1,
			TooLow = 2,
			NotEnoughZeny = 3,
			OwnAuction = 4,
		}

		/// <summary>
		/// Cancel result for <see cref="Cancel"/>.
		/// </summary>
		public enum CancelResult : byte
		{
			Success = 0,
			NotFound = 1,
			NotSeller = 2,
			HasBids = 3,
		}

		/// <summary>
		/// Hydrates the in-memory cache from the `auctions` table.
		/// Called once at world boot.
		/// </summary>
		public void LoadAll()
		{
			lock (_syncRoot)
			{
				if (_loaded)
					return;
				_loaded = true;

				List<AuctionEntry> rows;
				try
				{
					rows = ZoneServer.Instance.Database.LoadOpenAuctions();
				}
				catch (Exception ex)
				{
					Log.Error("AuctionManager.LoadAll: {0}", ex);
					return;
				}

				foreach (var row in rows)
					_open[row.Id] = row;
			}
		}

		/// <summary>Starts the resolution timer. Idempotent.</summary>
		public void StartPolling()
		{
			lock (_syncRoot)
			{
				if (_timer != null)
					return;
				_timer = new Timer(_ => this.Tick(),
					null, PollInterval, PollInterval);
			}
		}

		/// <summary>Returns a snapshot of open auctions (newest first).</summary>
		public IReadOnlyList<AuctionEntry> ListOpen()
		{
			lock (_syncRoot)
				return _open.Values.OrderByDescending(a => a.CreatedAt).ToList();
		}

		/// <summary>Resolves an auction id to its row, or null.</summary>
		public AuctionEntry Get(int auctionId)
		{
			lock (_syncRoot)
				return _open.TryGetValue(auctionId, out var e) ? e : null;
		}

		/// <summary>
		/// Lists an inventory item for sale. The item is removed from
		/// the seller's inventory immediately and held in the auction
		/// row's snapshot until resolution. <paramref name="hours"/>
		/// is clamped to [1, 48].
		/// </summary>
		public RegisterResult Register(PlayerCharacter seller, Item item,
			int startingPrice, int buyoutPrice, int hours, out AuctionEntry entry)
		{
			entry = null;

			if (startingPrice <= 0 || (buyoutPrice != 0 && buyoutPrice < startingPrice))
				return RegisterResult.InvalidPrice;
			if (hours < 1 || hours > 48)
				return RegisterResult.InvalidDuration;

			entry = new AuctionEntry
			{
				SellerId = seller.Id,
				SellerName = seller.Name,
				ItemClassId = item.ClassId,
				ItemAmount = (short)item.Amount,
				ItemRefine = item.RefineLevel,
				ItemIdentified = item.IsIdentified,
				ItemDamaged = item.IsDamaged,
				StartingPrice = startingPrice,
				BuyoutPrice = buyoutPrice,
				CurrentBid = 0,
				BidderId = 0,
				BidderName = string.Empty,
				Status = AuctionStatus.Open,
				CreatedAt = DateTime.UtcNow,
				Deadline = DateTime.UtcNow.AddHours(hours),
			};

			seller.Inventory.RemoveItem(item);

			ZoneServer.Instance.Database.InsertAuction(entry);
			lock (_syncRoot)
				_open[entry.Id] = entry;

			return RegisterResult.Success;
		}

		/// <summary>
		/// Places a bid on an open auction. Outbids refund the prior
		/// bidder via mail. A bid &gt;= buyoutPrice resolves the
		/// auction immediately.
		/// </summary>
		public BidResult Bid(PlayerCharacter bidder, int auctionId, int amount)
		{
			AuctionEntry entry;
			AuctionEntry resolved = null;
			int priorBid = 0;
			int priorBidderId = 0;
			string priorBidderName = null;

			lock (_syncRoot)
			{
				if (!_open.TryGetValue(auctionId, out entry))
					return BidResult.NotFound;
				if (entry.SellerId == bidder.Id)
					return BidResult.OwnAuction;
				if (amount < entry.StartingPrice || amount <= entry.CurrentBid)
					return BidResult.TooLow;
				if (bidder.Parameters.Zeny < amount)
					return BidResult.NotEnoughZeny;

				priorBid = entry.CurrentBid;
				priorBidderId = entry.BidderId;
				priorBidderName = entry.BidderName;

				bidder.Parameters.Zeny -= amount;
				entry.CurrentBid = amount;
				entry.BidderId = bidder.Id;
				entry.BidderName = bidder.Name;

				if (entry.BuyoutPrice > 0 && amount >= entry.BuyoutPrice)
				{
					_open.Remove(auctionId);
					resolved = entry;
				}
			}

			ZoneServer.Instance.Database.UpdateAuctionBid(
				entry.Id, entry.CurrentBid, entry.BidderId, entry.BidderName);

			if (priorBid > 0 && priorBidderId > 0)
				this.RefundOutbid(bidder, priorBidderId, priorBidderName, entry.Id, priorBid);

			if (resolved != null)
				this.PayoutSold(resolved);

			return BidResult.Success;
		}

		/// <summary>
		/// Seller cancels an auction that has no bids. Item is mailed
		/// back to the seller.
		/// </summary>
		public CancelResult Cancel(PlayerCharacter seller, int auctionId)
		{
			AuctionEntry entry;
			lock (_syncRoot)
			{
				if (!_open.TryGetValue(auctionId, out entry))
					return CancelResult.NotFound;
				if (entry.SellerId != seller.Id)
					return CancelResult.NotSeller;
				if (entry.HasBid)
					return CancelResult.HasBids;
				_open.Remove(auctionId);
			}

			entry.Status = AuctionStatus.Cancelled;
			ZoneServer.Instance.Database.UpdateAuctionStatus(entry.Id, AuctionStatus.Cancelled);
			this.MailItemBackToSeller(entry, "Auction cancelled",
				$"Your auction #{entry.Id} was cancelled. Item returned.");

			return CancelResult.Success;
		}

		// ----- Resolution loop ------------------------------------------

		private void Tick()
		{
			List<AuctionEntry> due;
			lock (_syncRoot)
			{
				var now = DateTime.UtcNow;
				due = _open.Values.Where(a => a.Deadline <= now).ToList();
				foreach (var a in due)
					_open.Remove(a.Id);
			}

			foreach (var entry in due)
			{
				try
				{
					if (entry.HasBid)
						this.PayoutSold(entry);
					else
						this.PayoutExpired(entry);
				}
				catch (Exception ex)
				{
					Log.Error("AuctionManager.Tick: payout failed for #{0}: {1}", entry.Id, ex);
				}
			}
		}

		private void PayoutSold(AuctionEntry entry)
		{
			entry.Status = AuctionStatus.Sold;
			ZoneServer.Instance.Database.UpdateAuctionStatus(entry.Id, AuctionStatus.Sold);

			// Item to winner.
			this.SendMailFromSystem(entry.BidderName, "Auction won",
				$"You won auction #{entry.Id}. Item enclosed.",
				zeny: 0,
				itemClassId: entry.ItemClassId,
				itemAmount: entry.ItemAmount,
				itemRefine: entry.ItemRefine,
				itemIdentified: entry.ItemIdentified,
				itemDamaged: entry.ItemDamaged);

			// Zeny to seller.
			this.SendMailFromSystem(entry.SellerName, "Auction sold",
				$"Your auction #{entry.Id} sold for {entry.CurrentBid}z.",
				zeny: entry.CurrentBid,
				itemClassId: 0, itemAmount: 0, itemRefine: 0,
				itemIdentified: true, itemDamaged: false);
		}

		private void PayoutExpired(AuctionEntry entry)
		{
			entry.Status = AuctionStatus.Expired;
			ZoneServer.Instance.Database.UpdateAuctionStatus(entry.Id, AuctionStatus.Expired);
			this.MailItemBackToSeller(entry, "Auction expired",
				$"Your auction #{entry.Id} expired with no bids. Item returned.");
		}

		private void RefundOutbid(PlayerCharacter newBidder, int priorBidderId,
			string priorBidderName, int auctionId, int priorBid)
		{
			// The new bidder is online and was charged just now; we
			// only need to push zeny back to the prior bidder. They
			// may be offline, so route through mail.
			this.SendMailFromSystem(priorBidderName, "Outbid",
				$"You were outbid on auction #{auctionId}. Refund enclosed.",
				zeny: priorBid,
				itemClassId: 0, itemAmount: 0, itemRefine: 0,
				itemIdentified: true, itemDamaged: false);
			_ = newBidder; // online sender; nothing to do here
		}

		private void MailItemBackToSeller(AuctionEntry entry, string title, string body)
		{
			this.SendMailFromSystem(entry.SellerName, title, body,
				zeny: 0,
				itemClassId: entry.ItemClassId,
				itemAmount: entry.ItemAmount,
				itemRefine: entry.ItemRefine,
				itemIdentified: entry.ItemIdentified,
				itemDamaged: entry.ItemDamaged);
		}

		/// <summary>
		/// Synthesises a mail from "Auctioneer". MailManager's <c>Send</c>
		/// requires a real <see cref="PlayerCharacter"/> sender; we
		/// look up an online player as the carrier (any online char),
		/// and if no one is online we write the row directly. The
		/// recipient sees senderName="Auctioneer" either way.
		/// </summary>
		private void SendMailFromSystem(string receiverName, string title, string body,
			int zeny, int itemClassId, short itemAmount, byte itemRefine,
			bool itemIdentified, bool itemDamaged)
		{
			var receiverId = ZoneServer.Instance.Database.GetCharacterIdByName(receiverName ?? string.Empty);
			if (receiverId <= 0)
			{
				Log.Warning("AuctionManager: cannot deliver '{0}' — receiver '{1}' not found.",
					title, receiverName);
				return;
			}

			var msg = new MailMessage
			{
				SenderId = 0,
				SenderName = "Auctioneer",
				ReceiverId = receiverId,
				Title = title,
				Body = body,
				SentAt = DateTime.UtcNow,
				Zeny = zeny,
				AttachedItemClassId = itemClassId,
				AttachedItemAmount = itemAmount,
				AttachedItemRefine = itemRefine,
				AttachedItemIdentified = itemIdentified,
				AttachedItemDamaged = itemDamaged,
			};

			ZoneServer.Instance.Database.InsertMail(msg);

			if (ZoneServer.Instance.World.Maps.TryGetPlayerById(receiverId, out var receiver))
			{
				Network.Send.ZC_MAIL_RECEIVE(receiver, msg);
				Network.Send.ZC_MAIL_WINDOWS(receiver, 0);
			}
		}
	}
}
