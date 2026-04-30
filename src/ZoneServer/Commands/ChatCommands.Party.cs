using Sabine.Shared.Util;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Groups;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadParty()
		{
			this.Add("party", "<create|disband|info> [name]", Localization.Get("Party administration."), this.Party);
			this.Add("partyrecall", "<name>", Localization.Get("Recalls a member of the target's party."), this.PartyRecall);
			this.Add("partyspy", "", Localization.Get("Toggles party-chat spying on the target."), this.PartySpy);
			this.Add("partyoption", "<exp> <item>", Localization.Get("Adjusts the party's loot options."), this.PartyOption);
			this.Add("partysharelvl", "<level>", Localization.Get("Sets party share level range."), this.PartyShareLvl);
			this.Add("invite", "<name>", Localization.Get("Invites a player to the sender's party."), this.Invite);
			this.Add("leave", "", Localization.Get("Leaves the current party."), this.Leave);
			this.Add("accept", "", Localization.Get("Accepts a pending invite."), this.Accept);
			this.Add("reject", "", Localization.Get("Rejects a pending invite."), this.Reject);
			this.Add("changeleader", "<name>", Localization.Get("Transfers party leadership."), this.ChangeLeader);
		}

		private CommandResult Party(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var sub = args.Get(0).ToLowerInvariant();
			switch (sub)
			{
				case "create":
					if (args.Count < 2) return CommandResult.InvalidArgument;
					var p = PartyManager.Instance.CreateParty(target, args.Get(1));
					sender.ServerMessage(p == null ? Localization.Get("Could not create party.") : Localization.Get("Party '{0}' created."), p?.Name);
					return CommandResult.Okay;
				case "disband":
					if (target.Party == null) { sender.ServerMessage(Localization.Get("Not in a party.")); return CommandResult.Okay; }
					target.Party.Disband();
					sender.ServerMessage(Localization.Get("Party disbanded."));
					return CommandResult.Okay;
				case "info":
					if (target.Party == null) { sender.ServerMessage(Localization.Get("Not in a party.")); return CommandResult.Okay; }
					sender.ServerMessage("Party '{0}' (id {1}, {2} member(s))", target.Party.Name, target.Party.Id, target.Party.Members.Count);
					return CommandResult.Okay;
			}
			return CommandResult.InvalidArgument;
		}

		private CommandResult PartyRecall(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.Party == null) { sender.ServerMessage(Localization.Get("Not in a party.")); return CommandResult.Okay; }
			var n = 0;
			foreach (var m in target.Party.Members)
			{
				if (m.Player != null && m.Player != sender)
				{
					m.Player.Warp(sender.MapId, sender.Position);
					n++;
				}
			}
			sender.ServerMessage(Localization.Get("Recalled {0} party member(s)."), n);
			return CommandResult.Okay;
		}

		private CommandResult PartySpy(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var v = sender.Vars.Temp.ToggleBool("Sabine.PartySpy." + (target.Party?.Id ?? 0));
			sender.ServerMessage(Localization.Get("Party spy: {0}"), v);
			return CommandResult.Okay;
		}

		private CommandResult PartyOption(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Party option editing not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult PartyShareLvl(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Party share level not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult Invite(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Invite via command is not implemented; use the party UI."));
			return CommandResult.Okay;
		}

		private CommandResult Leave(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.Party == null) { sender.ServerMessage(Localization.Get("Not in a party.")); return CommandResult.Okay; }
			target.Party.RemoveMember(target.Id);
			sender.ServerMessage(Localization.Get("Left party."));
			return CommandResult.Okay;
		}

		private CommandResult Accept(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Accept via command is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult Reject(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Reject via command is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult ChangeLeader(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Leader change via command is not implemented."));
			return CommandResult.Okay;
		}
	}
}
