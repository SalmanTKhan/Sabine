using Sabine.Shared.Util;
using Sabine.Zone.Network;
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
			if (target.Party == null) { sender.ServerMessage(Localization.Get("Not in a party.")); return CommandResult.Okay; }
			if (!target.Party.IsLeader(target)) { sender.ServerMessage(Localization.Get("Only the leader may change party options.")); return CommandResult.Okay; }
			if (args.Count < 1) return CommandResult.InvalidArgument;

			if (int.TryParse(args.Get(0), out var exp))
			{
				target.Party.ExpShareOption = exp == 0
					? PartyExpShareOption.Individual
					: PartyExpShareOption.EvenShare;
			}
			if (args.Count >= 2 && int.TryParse(args.Get(1), out var item))
			{
				target.Party.ItemShareOption = item == 0
					? PartyItemShareOption.Individual
					: PartyItemShareOption.EvenShare;
			}

			ZoneServer.Instance.Database.UpdatePartyOptions(target.Party.Id,
				target.Party.ExpShareOption == PartyExpShareOption.EvenShare,
				target.Party.ItemShareOption == PartyItemShareOption.EvenShare);

			target.Party.UpdateAllClients();
			sender.ServerMessage(Localization.Get("Party options updated."));
			return CommandResult.Okay;
		}

		// eAthena atcommand_partysharelvl: caps the base-level gap a
		// member can have from the sharer and still get a slice of the
		// EvenShare pool. The level cap is plumbed onto Party today;
		// EXP / loot distribution doesn't yet consult it (the
		// distribution code itself isn't wired yet — same gap as
		// /alootid). No-arg prints the current cap.
		private CommandResult PartyShareLvl(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.Party == null)
			{
				sender.ServerMessage(Localization.Get("Not in a party."));
				return CommandResult.Okay;
			}

			if (args.Count == 0)
			{
				sender.ServerMessage(Localization.Get("Party share level cap: {0}"), target.Party.ShareLevel);
				return CommandResult.Okay;
			}

			if (!target.Party.IsLeader(target))
			{
				sender.ServerMessage(Localization.Get("Only the leader may change party options."));
				return CommandResult.Okay;
			}

			if (!int.TryParse(args.Get(0), out var level) || level < 0)
				return CommandResult.InvalidArgument;

			target.Party.ShareLevel = level;
			sender.ServerMessage(Localization.Get("Party share level cap set to {0}."), level);
			return CommandResult.Okay;
		}

		private CommandResult Invite(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (target.Party == null) { sender.ServerMessage(Localization.Get("Not in a party.")); return CommandResult.Okay; }
			if (!target.Party.IsLeader(target)) { sender.ServerMessage(Localization.Get("Only the leader may invite.")); return CommandResult.Okay; }

			var name = args.Get(0);
			if (!ZoneServer.Instance.World.Maps.TryGetPlayerByName(name, out var invitee))
			{
				sender.ServerMessage(Localization.Get("Player '{0}' is not online."), name);
				return CommandResult.Okay;
			}

			if (invitee.Party != null)
			{
				sender.ServerMessage(Localization.Get("'{0}' is already in a party."), name);
				return CommandResult.Okay;
			}

			if (target.Party.IsFull())
			{
				sender.ServerMessage(Localization.Get("Party is full."));
				return CommandResult.Okay;
			}

			PartyManager.Instance.RegisterInvite(target, invitee);
			Send.ZC_REQ_JOIN_GROUP(invitee, target.Connection.Account.Id, target.Party.Name);
			sender.ServerMessage(Localization.Get("Invited '{0}'."), invitee.Name);
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
			if (!PartyManager.Instance.ConsumePendingInvite(target.Id, out var invite))
			{
				sender.ServerMessage(Localization.Get("No pending invite."));
				return CommandResult.Okay;
			}

			var party = PartyManager.Instance.GetParty(invite.PartyId);
			if (party == null || target.Party != null || party.IsFull())
			{
				sender.ServerMessage(Localization.Get("Could not join party."));
				return CommandResult.Okay;
			}

			if (!party.AddMember(target))
			{
				sender.ServerMessage(Localization.Get("Could not join party."));
				return CommandResult.Okay;
			}

			if (ZoneServer.Instance.World.Maps.TryGetPlayerById(invite.InviterCharacterId, out var inviter))
				Send.ZC_ACK_REQ_JOIN_GROUP(inviter, target.Name, 2);

			sender.ServerMessage(Localization.Get("Joined party '{0}'."), party.Name);
			return CommandResult.Okay;
		}

		private CommandResult Reject(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (!PartyManager.Instance.ConsumePendingInvite(target.Id, out var invite))
			{
				sender.ServerMessage(Localization.Get("No pending invite."));
				return CommandResult.Okay;
			}

			if (ZoneServer.Instance.World.Maps.TryGetPlayerById(invite.InviterCharacterId, out var inviter))
				Send.ZC_ACK_REQ_JOIN_GROUP(inviter, target.Name, 1);

			sender.ServerMessage(Localization.Get("Invite rejected."));
			return CommandResult.Okay;
		}

		private CommandResult ChangeLeader(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (target.Party == null) { sender.ServerMessage(Localization.Get("Not in a party.")); return CommandResult.Okay; }
			if (!target.Party.IsLeader(target)) { sender.ServerMessage(Localization.Get("Only the leader may transfer leadership.")); return CommandResult.Okay; }

			var name = args.Get(0);
			var newLeader = target.Party.GetMemberByName(name);
			if (newLeader == null)
			{
				sender.ServerMessage(Localization.Get("'{0}' is not in the party."), name);
				return CommandResult.Okay;
			}

			target.Party.ChangeLeader(newLeader.CharacterId);
			sender.ServerMessage(Localization.Get("Leadership transferred to '{0}'."), name);
			return CommandResult.Okay;
		}
	}
}
