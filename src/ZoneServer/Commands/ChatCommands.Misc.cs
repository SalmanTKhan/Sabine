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
			this.Add("auction", "", Localization.Get("Opens the auction interface."), this.Auction);
			this.Add("adoption", "<child>", Localization.Get("Initiates an adoption."), this.Adoption);
			this.Add("adopt", "<child>", Localization.Get("Completes an adoption."), this.Adoption);
			this.Add("clan", "<create|disband|info> [name]", Localization.Get("Clan administration."), this.Clan);
			this.Add("clanspy", "", Localization.Get("Toggles spying on a clan."), this.ClanSpy);
			this.Add("instance", "<create|destroy|info> [name]", Localization.Get("Instance management."), this.Instance);
		}

		private CommandResult Mail(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 3) return CommandResult.InvalidArgument;
			MailService.Instance.Send(sender.Name, args.Get(0), args.Get(1), args.Get(2));
			sender.ServerMessage(Localization.Get("Mail queued."));
			return CommandResult.Okay;
		}

		private CommandResult Auction(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Auction subsystem is not yet implemented."));
			return CommandResult.Okay;
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
