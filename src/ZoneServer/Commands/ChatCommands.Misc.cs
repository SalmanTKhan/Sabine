using Sabine.Shared.Util;
using Sabine.Zone.World;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Groups;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadMisc()
		{
			this.Add("mail", "<to> <title> <body>", Localization.Get("Sends a mail message."), this.Mail);
			this.Add("auction", "<list|sell|bid|cancel|info> [args]", Localization.Get("Auction house (text-mode)."), this.Auction);
			this.Add("adoption", "<child>", Localization.Get("Initiates an adoption."), this.Adoption);
			this.Add("adopt", "<child>", Localization.Get("Completes an adoption."), this.Adoption);
			this.Add("clan", "<create|disband|info> [name]", Localization.Get("Clan administration."), this.Clan);
			this.Add("clanspy", "", Localization.Get("Toggles spying on a clan."), this.ClanSpy);
			this.Add("instance", "<create|destroy|info> [name]", Localization.Get("Instance management."), this.Instance);
		}

		private CommandResult Mail(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 3) return CommandResult.InvalidArgument;

			var result = ZoneServer.Instance.World.Mail.Send(
				target, args.Get(0), args.Get(1), args.Get(2),
				zeny: 0, itemClassId: 0, itemAmount: 0, itemRefine: 0,
				itemIdentified: true, itemDamaged: false, out _);

			if (result == World.Mail.MailManager.SendResult.ReceiverUnknown)
				sender.ServerMessage(Localization.Get("Player '{0}' not found."), args.Get(0));
			else
				sender.ServerMessage(Localization.Get("Mail sent."));

			return CommandResult.Okay;
		}

		private CommandResult Auction(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1)
				return CommandResult.InvalidArgument;

			var auctions = ZoneServer.Instance.World.Auctions;
			var sub = args.Get(0).ToLowerInvariant();

			switch (sub)
			{
				case "list":
				{
					var open = auctions.ListOpen();
					if (open.Count == 0)
					{
						sender.ServerMessage(Localization.Get("No open auctions."));
						return CommandResult.Okay;
					}
					foreach (var a in open)
					{
						var bid = a.HasBid ? a.CurrentBid.ToString() : Localization.Get("(no bid)");
						sender.ServerMessage("[{0}] {1} x{2} +{3} | bid {4} | buyout {5} | by {6} | ends {7:u}",
							a.Id, a.ItemClassId, a.ItemAmount, a.ItemRefine,
							bid, a.BuyoutPrice == 0 ? "-" : a.BuyoutPrice.ToString(),
							a.SellerName, a.Deadline);
					}
					return CommandResult.Okay;
				}

				case "info":
				{
					if (args.Count < 2 || !int.TryParse(args.Get(1), out var infoId))
						return CommandResult.InvalidArgument;
					var entry = auctions.Get(infoId);
					if (entry == null)
					{
						sender.ServerMessage(Localization.Get("Auction not found."));
						return CommandResult.Okay;
					}
					sender.ServerMessage("#{0} {1} x{2} +{3} | starting {4}, current {5}, buyout {6} | seller {7} | ends {8:u}",
						entry.Id, entry.ItemClassId, entry.ItemAmount, entry.ItemRefine,
						entry.StartingPrice, entry.CurrentBid,
						entry.BuyoutPrice == 0 ? "-" : entry.BuyoutPrice.ToString(),
						entry.SellerName, entry.Deadline);
					return CommandResult.Okay;
				}

				case "sell":
				{
					if (args.Count < 5)
					{
						sender.ServerMessage(Localization.Get("Usage: @auction sell <invSlot> <starting> <buyout> <hours>"));
						return CommandResult.InvalidArgument;
					}
					if (!int.TryParse(args.Get(1), out var invId) ||
						!int.TryParse(args.Get(2), out var starting) ||
						!int.TryParse(args.Get(3), out var buyout) ||
						!int.TryParse(args.Get(4), out var hours))
						return CommandResult.InvalidArgument;

					if (!target.Inventory.TryGetItem(invId, out var item))
					{
						sender.ServerMessage(Localization.Get("Item not found in slot {0}."), invId);
						return CommandResult.Okay;
					}

					var result = auctions.Register(target, item, starting, buyout, hours, out var entry);
					switch (result)
					{
						case World.Auctions.AuctionManager.RegisterResult.Success:
							sender.ServerMessage(Localization.Get("Listed as auction #{0} (deadline {1:u})."), entry.Id, entry.Deadline);
							break;
						case World.Auctions.AuctionManager.RegisterResult.InvalidPrice:
							sender.ServerMessage(Localization.Get("Invalid price."));
							break;
						case World.Auctions.AuctionManager.RegisterResult.InvalidDuration:
							sender.ServerMessage(Localization.Get("Duration must be 1-48 hours."));
							break;
					}
					return CommandResult.Okay;
				}

				case "bid":
				{
					if (args.Count < 3 ||
						!int.TryParse(args.Get(1), out var bidId) ||
						!int.TryParse(args.Get(2), out var bidAmount))
						return CommandResult.InvalidArgument;

					var result = auctions.Bid(target, bidId, bidAmount);
					switch (result)
					{
						case World.Auctions.AuctionManager.BidResult.Success:
							sender.ServerMessage(Localization.Get("Bid placed."));
							break;
						case World.Auctions.AuctionManager.BidResult.NotFound:
							sender.ServerMessage(Localization.Get("Auction not found."));
							break;
						case World.Auctions.AuctionManager.BidResult.TooLow:
							sender.ServerMessage(Localization.Get("Bid too low."));
							break;
						case World.Auctions.AuctionManager.BidResult.NotEnoughZeny:
							sender.ServerMessage(Localization.Get("Not enough zeny."));
							break;
						case World.Auctions.AuctionManager.BidResult.OwnAuction:
							sender.ServerMessage(Localization.Get("Cannot bid on your own auction."));
							break;
					}
					return CommandResult.Okay;
				}

				case "cancel":
				{
					if (args.Count < 2 || !int.TryParse(args.Get(1), out var cancelId))
						return CommandResult.InvalidArgument;

					var result = auctions.Cancel(target, cancelId);
					switch (result)
					{
						case World.Auctions.AuctionManager.CancelResult.Success:
							sender.ServerMessage(Localization.Get("Auction cancelled. Item returned via mail."));
							break;
						case World.Auctions.AuctionManager.CancelResult.NotFound:
							sender.ServerMessage(Localization.Get("Auction not found."));
							break;
						case World.Auctions.AuctionManager.CancelResult.NotSeller:
							sender.ServerMessage(Localization.Get("You are not the seller."));
							break;
						case World.Auctions.AuctionManager.CancelResult.HasBids:
							sender.ServerMessage(Localization.Get("Cannot cancel an auction with active bids."));
							break;
					}
					return CommandResult.Okay;
				}
			}

			return CommandResult.InvalidArgument;
		}

		private CommandResult Adoption(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!ZoneServer.Instance.World.Maps.TryGetPlayerByName(args.Get(0), out var child))
			{
				sender.ServerMessage(Localization.Get("Player '{0}' not found."), args.Get(0));
				return CommandResult.Okay;
			}
			child.Vars.Perm.SetInt("Sabine.AdoptedBy", target.Id);
			sender.ServerMessage(Localization.Get("{0} has adopted {1}."), target.Name, child.Name);
			return CommandResult.Okay;
		}

		private CommandResult Clan(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var sub = args.Get(0).ToLowerInvariant();
			switch (sub)
			{
				case "create":
					if (args.Count < 2) return CommandResult.InvalidArgument;
					var c = ClanManager.Instance.Create(args.Get(1), target.Id);
					sender.ServerMessage(Localization.Get("Clan '{0}' created (id {1})."), c.Name, c.Id);
					return CommandResult.Okay;
				case "disband":
					if (args.Count < 2) return CommandResult.InvalidArgument;
					var ex = ClanManager.Instance.GetByName(args.Get(1));
					if (ex == null) { sender.ServerMessage(Localization.Get("Clan not found.")); return CommandResult.Okay; }
					ClanManager.Instance.Destroy(ex.Id);
					sender.ServerMessage(Localization.Get("Clan disbanded."));
					return CommandResult.Okay;
				case "info":
					foreach (var x in ClanManager.Instance.All)
						sender.ServerMessage("[{0}] {1} ({2} members)", x.Id, x.Name, x.Members.Count);
					return CommandResult.Okay;
			}
			return CommandResult.InvalidArgument;
		}

		private CommandResult ClanSpy(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var v = sender.Vars.Temp.ToggleBool("Sabine.ClanSpy");
			sender.ServerMessage(Localization.Get("Clan spy: {0}"), v);
			return CommandResult.Okay;
		}

		private CommandResult Instance(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var sub = args.Get(0).ToLowerInvariant();
			switch (sub)
			{
				case "create":
					if (args.Count < 2) return CommandResult.InvalidArgument;
					var i = InstanceService.Instance.Create(args.Get(1), target.Id);
					sender.ServerMessage(Localization.Get("Instance {0} ('{1}') created."), i.Id, i.Name);
					return CommandResult.Okay;
				case "destroy":
					if (args.Count < 2 || !int.TryParse(args.Get(1), out var iid)) return CommandResult.InvalidArgument;
					sender.ServerMessage(InstanceService.Instance.Destroy(iid) ? Localization.Get("Instance destroyed.") : Localization.Get("Instance not found."));
					return CommandResult.Okay;
				case "info":
					foreach (var x in InstanceService.Instance.All)
						sender.ServerMessage("[{0}] {1}", x.Id, x.Name);
					return CommandResult.Okay;
			}
			return CommandResult.InvalidArgument;
		}
	}
}
