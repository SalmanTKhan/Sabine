using System;
using System.Linq;
using Sabine.Shared.Util;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadServer()
		{
			this.Add("broadcast", "<message>", Localization.Get("Broadcasts message to everyone on the server."), this.Broadcast);
			this.Add("reloadscripts", "", Localization.Get("Reloads all scripts."), this.ReloadScripts);
			this.Add("reloadconf", "", Localization.Get("Reloads server configuration."), this.ReloadConf);
			this.Add("reloaddata", "", Localization.Get("Reloads data files."), this.ReloadData);

			this.AddAlias("broadcast", "bc");
			this.AddAlias("reloadscripts", "rs");

			this.Add("localbroadcast", "<message>", Localization.Get("Broadcasts a message to the local map."), this.LocalBroadcast);
			this.Add("kami", "<message>", Localization.Get("Broadcasts a yellow announcement message."), this.Kami);
			this.Add("kamib", "<message>", Localization.Get("Broadcasts a blue announcement message."), this.KamiB);
			this.Add("kamic", "<color> <message>", Localization.Get("Broadcasts a colored announcement."), this.KamiC);
			this.Add("lkami", "<message>", Localization.Get("Broadcasts a local announcement on this map."), this.LocalBroadcast);
			this.Add("gmotd", "", Localization.Get("Re-displays the GM message of the day."), this.Gmotd);
			this.Add("mapexit", "", Localization.Get("Disconnects all players from the server."), this.MapExit);

			this.Add("reloaditemdb", "", Localization.Get("Reloads the item database."), this.ReloadItemDb);
			this.Add("reloadmobdb", "", Localization.Get("Reloads the monster database."), this.ReloadMobDb);
			this.Add("reloadskilldb", "", Localization.Get("Reloads the skill database."), this.ReloadSkillDb);
			this.Add("reloadbattleconf", "", Localization.Get("Reloads the battle configuration."), this.ReloadBattleConf);
			this.Add("reloadstatusdb", "", Localization.Get("Reloads the status effect database."), this.ReloadStatusDb);
			this.Add("reloadpcdb", "", Localization.Get("Reloads the player database."), this.ReloadPcDb);
			this.Add("reloadmotd", "", Localization.Get("Reloads the message of the day."), this.ReloadMotd);
			this.Add("reloadatcommand", "", Localization.Get("Reloads chat command configuration."), this.ReloadAtcommand);
			this.Add("reloadnpcfile", "<path>", Localization.Get("Reloads a single NPC script file."), this.ReloadNpcFile);
			this.Add("reloadcashdb", "", Localization.Get("Reloads the cash shop database."), this.ReloadCashDb);
			this.Add("reloadquestdb", "", Localization.Get("Reloads the quest database."), this.ReloadQuestDb);
			this.Add("reloadmsgconf", "", Localization.Get("Reloads the message configuration."), this.ReloadMsgConf);
			this.Add("reloadinstancedb", "", Localization.Get("Reloads the instance database."), this.ReloadInstanceDb);
			this.Add("reloadachievementdb", "", Localization.Get("Reloads the achievement database."), this.ReloadAchievementDb);
			this.Add("reloadattendancedb", "", Localization.Get("Reloads the attendance database."), this.ReloadAttendanceDb);
			this.Add("reloadbarterdb", "", Localization.Get("Reloads the barter database."), this.ReloadBarterDb);
			this.Add("reloadlogconf", "", Localization.Get("Reloads the logging configuration."), this.ReloadLogConf);

			this.Add("send", "<op> [hex]", Localization.Get("Sends a raw packet to the target's client."), this.SendRaw);
			this.Add("setbattleflag", "<flag> <value>", Localization.Get("Sets a battle config flag at runtime."), this.SetBattleFlag);
			this.Add("set", "<var> <value>", Localization.Get("Sets a server variable."), this.Set);
			this.Add("addperm", "<perm>", Localization.Get("Adds a permission to the target."), this.AddPerm);
			this.Add("rmvperm", "<perm>", Localization.Get("Removes a permission from the target."), this.RmvPerm);
			this.Add("accinfo", "<account name>", Localization.Get("Displays account info."), this.AccInfo);
			this.Add("adjgmlvl", "<level>", Localization.Get("Adjusts the target's GM level."), this.AdjGmLvl);
			this.Add("adjcmdlvl", "<command> <level>", Localization.Get("Adjusts a command's authority level."), this.AdjCmdLvl);
		}

		private CommandResult LocalBroadcast(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var msg = args.Count == 1 ? args.Get(0) : string.Join(" ", args.GetAll());
			var text = string.Format("[Local] {0} : {1}", sender.Name, msg);
			using var players = Yggdrasil.Collections.PooledList<PlayerCharacter>.Rent();
			sender.Map.GetPlayers(players, 0, static (_, _) => true);
			foreach (var p in players)
				Send.ZC_NOTIFY_CHAT(p, 0, text);
			return CommandResult.Okay;
		}

		private CommandResult BroadcastWithPrefix(string prefix, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var msg = args.Count == 1 ? args.Get(0) : string.Join(" ", args.GetAll());
			Send.ZC_BROADCAST(string.Format("{0}{1}", prefix, msg));
			return CommandResult.Okay;
		}

		private CommandResult Kami(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.BroadcastWithPrefix("[Kami] ", args);
		private CommandResult KamiB(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.BroadcastWithPrefix("[Kami-Blue] ", args);

		private CommandResult KamiC(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 2) return CommandResult.InvalidArgument;
			var color = args.Get(0);
			var msg = string.Join(" ", args.GetAll().Skip(1));
			Send.ZC_BROADCAST(string.Format("[Kami #{0}] {1}", color, msg));
			return CommandResult.Okay;
		}

		private CommandResult Gmotd(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("MOTD: Welcome to Sabine."));
			return CommandResult.Okay;
		}

		private CommandResult MapExit(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			Send.ZC_BROADCAST("[Server] Server is shutting down. You will be disconnected.");
			using var players = Yggdrasil.Collections.PooledList<PlayerCharacter>.Rent();
			ZoneServer.Instance.World.Maps.Do(m => m.GetPlayers(players, 0, static (_, _) => true));
			foreach (var p in players)
			{
				try { p.Connection.Close(); } catch { }
			}
			return CommandResult.Okay;
		}

		private CommandResult Broadcast(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var msg = args.Count == 1 ? args.Get(0) : string.Join(" ", args.GetAll());
			Send.ZC_BROADCAST(string.Format("{0} : {1}", sender.Name, msg));
			sender.ServerMessage(Localization.Get("Message was broadcasted."));
			return CommandResult.Okay;
		}

		private CommandResult ReloadScripts(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Reloading scripts..."));
			ZoneServer.Instance.World.RemoveScriptedEntities();
			ZoneServer.Instance.ReloadScripts();
			ZoneServer.Instance.World.Spawners.InitialSpawn();
			sender.ServerMessage(Localization.Get("Done."));
			return CommandResult.Okay;
		}

		private CommandResult ReloadConf(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Reloading configuration..."));
			ZoneServer.Instance.LoadConf();
			sender.ServerMessage(Localization.Get("Done."));
			return CommandResult.Okay;
		}

		private CommandResult ReloadData(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Reloading data..."));
			ZoneServer.Instance.LoadData();
			sender.ServerMessage(Localization.Get("Done."));
			return CommandResult.Okay;
		}

		private CommandResult ReloadItemDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadMobDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadSkillDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadStatusDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadPcDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadCashDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadQuestDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadInstanceDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadAchievementDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadAttendanceDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadBarterDb(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadData(sender, target, message, commandName, args);
		private CommandResult ReloadBattleConf(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadConf(sender, target, message, commandName, args);
		private CommandResult ReloadAtcommand(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadConf(sender, target, message, commandName, args);
		private CommandResult ReloadMsgConf(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadConf(sender, target, message, commandName, args);
		private CommandResult ReloadLogConf(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadConf(sender, target, message, commandName, args);
		private CommandResult ReloadMotd(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("MOTD reloaded."));
			return CommandResult.Okay;
		}
		private CommandResult ReloadNpcFile(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ReloadScripts(sender, target, message, commandName, args);

		private CommandResult SendRaw(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Raw packet send is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult SetBattleFlag(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Runtime battle flag mutation is not implemented; edit conf and reloadconf."));
			return CommandResult.Okay;
		}

		private CommandResult Set(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 2) return CommandResult.InvalidArgument;
			target.Vars.Perm.SetString(args.Get(0), args.Get(1));
			sender.ServerMessage(Localization.Get("Set {0}={1} on {2}."), args.Get(0), args.Get(1), target.Name);
			return CommandResult.Okay;
		}

		private CommandResult AddPerm(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			target.Vars.Perm.SetBool("Sabine.Perm." + args.Get(0), true);
			sender.ServerMessage(Localization.Get("Permission '{0}' granted to {1}."), args.Get(0), target.Name);
			return CommandResult.Okay;
		}

		private CommandResult RmvPerm(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			target.Vars.Perm.SetBool("Sabine.Perm." + args.Get(0), false);
			sender.ServerMessage(Localization.Get("Permission '{0}' removed from {1}."), args.Get(0), target.Name);
			return CommandResult.Okay;
		}

		private CommandResult AccInfo(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var name = args.Count > 0 ? args.Get(0) : target.Name;
			if (!ZoneServer.Instance.World.Maps.TryGetPlayerByName(name, out var p))
			{
				sender.ServerMessage(Localization.Get("Player '{0}' not found."), name);
				return CommandResult.Okay;
			}

			sender.ServerMessage(Localization.Get("Account: {0} (id {1}) — Auth: {2}"), p.Username, p.Connection.Account.Id, p.Connection.Account.Authority);
			return CommandResult.Okay;
		}

		private CommandResult AdjGmLvl(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var lvl)) return CommandResult.InvalidArgument;
			target.Connection.Account.Authority = lvl;
			sender.ServerMessage(Localization.Get("{0}'s authority set to {1}."), target.Name, lvl);
			return CommandResult.Okay;
		}

		private CommandResult AdjCmdLvl(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Runtime command-level mutation not supported; edit commands.conf and reloadconf."));
			return CommandResult.Okay;
		}
	}
}
