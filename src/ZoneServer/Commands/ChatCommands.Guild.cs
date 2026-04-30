using Sabine.Shared.Util;
using Sabine.Zone.Network;
using Sabine.Zone.World;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Groups;
using Sabine.Zone.World.Maps;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadGuild()
		{
			this.Add("guild", "<create|disband|info> [name]", Localization.Get("Guild administration."), this.Guild);
			this.Add("guildlevelup", "<modifier>", Localization.Get("Adjusts the target's guild level."), this.GuildLevelUp);
			this.Add("guildrecall", "", Localization.Get("Recalls the target's guild members."), this.GuildRecall);
			this.Add("guildspy", "", Localization.Get("Toggles spying on the target's guild chat."), this.GuildSpy);
			this.Add("changegm", "<name>", Localization.Get("Transfers guild leadership."), this.ChangeGm);
			this.Add("disguiseguild", "<sprite>", Localization.Get("Disguises the target's guild members."), this.DisguiseGuild);
			this.Add("undisguiseguild", "", Localization.Get("Removes guild disguise."), this.UndisguiseGuild);
			this.Add("sizeguild", "<size>", Localization.Get("Sets the size of all guild members."), this.SizeGuild);

			this.Add("agitstart", "", Localization.Get("Starts War of Emperium."), this.AgitStart);
			this.Add("agitend", "", Localization.Get("Ends War of Emperium."), this.AgitEnd);
			this.Add("agitstart2", "", Localization.Get("Starts War of Emperium (renewal)."), this.AgitStart);
			this.Add("agitend2", "", Localization.Get("Ends War of Emperium (renewal)."), this.AgitEnd);

			this.Add("pvpon", "", Localization.Get("Enables PvP on the target's map."), this.PvpOn);
			this.Add("pvpoff", "", Localization.Get("Disables PvP on the target's map."), this.PvpOff);
			this.Add("gvgon", "", Localization.Get("Enables GvG on the target's map."), this.GvgOn);
			this.Add("gvgoff", "", Localization.Get("Disables GvG on the target's map."), this.GvgOff);
			this.Add("gpvpon", "", Localization.Get("Enables GvG-PvP on the target's map."), this.GvgOn);
			this.Add("gpvpoff", "", Localization.Get("Disables GvG-PvP on the target's map."), this.GvgOff);

			this.AddAlias("guildlevelup", "glvl");
			this.AddAlias("guildlevelup", "glevel");
			this.AddAlias("guildlevelup", "guildlvl");
			this.AddAlias("guildlevelup", "guildlvup");
			this.AddAlias("guildlevelup", "guildlvlup");
		}

		private CommandResult Guild(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var sub = args.Get(0).ToLowerInvariant();
			switch (sub)
			{
				case "create":
					if (args.Count < 2) return CommandResult.InvalidArgument;
					var name = args.Get(1);
					var g = GuildManager.Instance.Create(target, name);
					if (g == null) sender.ServerMessage(Localization.Get("Could not create guild '{0}' (name in use or member already in a guild)."), name);
					else sender.ServerMessage(Localization.Get("Guild '{0}' created (id {1})."), g.Name, g.Id);
					return CommandResult.Okay;

				case "disband":
					if (target.Guild == null) { sender.ServerMessage(Localization.Get("Target is not in a guild.")); return CommandResult.Okay; }
					GuildManager.Instance.Disband(target.Guild.Id);
					sender.ServerMessage(Localization.Get("Guild disbanded."));
					return CommandResult.Okay;

				case "info":
					var gi = target.Guild;
					if (gi == null) { sender.ServerMessage(Localization.Get("Target is not in a guild.")); return CommandResult.Okay; }
					sender.ServerMessage("Guild '{0}' (id {1}) lv {2}, {3} member(s), master {4}", gi.Name, gi.Id, gi.Level, gi.Members.Count, gi.MasterName);
					return CommandResult.Okay;
			}
			return CommandResult.InvalidArgument;
		}

		private CommandResult GuildLevelUp(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.Guild == null) { sender.ServerMessage(Localization.Get("Target is not in a guild.")); return CommandResult.Okay; }
			var delta = 1;
			if (args.Count >= 1 && !int.TryParse(args.Get(0), out delta)) return CommandResult.InvalidArgument;
			target.Guild.Level += delta;
			sender.ServerMessage(Localization.Get("Guild '{0}' level: {1}"), target.Guild.Name, target.Guild.Level);
			return CommandResult.Okay;
		}

		private CommandResult GuildRecall(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.Guild == null) { sender.ServerMessage(Localization.Get("Target is not in a guild.")); return CommandResult.Okay; }
			var n = 0;
			foreach (var m in target.Guild.OnlineMembers)
			{
				if (m == sender) continue;
				m.Warp(sender.MapId, sender.Position);
				n++;
			}
			sender.ServerMessage(Localization.Get("Recalled {0} guild member(s)."), n);
			return CommandResult.Okay;
		}

		private CommandResult GuildSpy(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var v = sender.Vars.Temp.ToggleBool("Sabine.GuildSpy." + (target.Guild?.Id ?? 0));
			sender.ServerMessage(Localization.Get("Guild spy: {0}"), v);
			return CommandResult.Okay;
		}

		private CommandResult ChangeGm(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var g = target.Guild;
			if (g == null) { sender.ServerMessage(Localization.Get("Target is not in a guild.")); return CommandResult.Okay; }
			if (!ZoneServer.Instance.World.Maps.TryGetPlayerByName(args.Get(0), out var newMaster))
			{
				sender.ServerMessage(Localization.Get("Player '{0}' not found."), args.Get(0));
				return CommandResult.Okay;
			}
			g.MasterCharId = newMaster.Id;
			g.MasterName = newMaster.Name;
			sender.ServerMessage(Localization.Get("Guild master changed to {0}."), newMaster.Name);
			return CommandResult.Okay;
		}

		private CommandResult DisguiseGuild(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Guild disguise is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult UndisguiseGuild(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Guild disguise is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult SizeGuild(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Guild size change is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult AgitStart(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			WoeService.Instance.Start();
			Send.ZC_BROADCAST("[Server] War of Emperium has started.");
			return CommandResult.Okay;
		}

		private CommandResult AgitEnd(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			WoeService.Instance.End();
			Send.ZC_BROADCAST("[Server] War of Emperium has ended.");
			return CommandResult.Okay;
		}

		private CommandResult PvpOn(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Map.SetFlag(MapFlags.PvP);
			sender.ServerMessage(Localization.Get("PvP enabled on {0}."), target.Map.StringId);
			return CommandResult.Okay;
		}

		private CommandResult PvpOff(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Map.ClearFlag(MapFlags.PvP);
			sender.ServerMessage(Localization.Get("PvP disabled on {0}."), target.Map.StringId);
			return CommandResult.Okay;
		}

		private CommandResult GvgOn(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Map.SetFlag(MapFlags.Gvg);
			sender.ServerMessage(Localization.Get("GvG enabled on {0}."), target.Map.StringId);
			return CommandResult.Okay;
		}

		private CommandResult GvgOff(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Map.ClearFlag(MapFlags.Gvg);
			sender.ServerMessage(Localization.Get("GvG disabled on {0}."), target.Map.StringId);
			return CommandResult.Okay;
		}
	}
}
