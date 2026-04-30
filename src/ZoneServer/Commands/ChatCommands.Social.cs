using Sabine.Shared.Util;
using Sabine.Zone.World;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Chats;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadSocial()
		{
			this.Add("language", "<language>", Localization.Get("Changes the localization language."), this.Language);

			this.Add("marry", "<name>", Localization.Get("Marries the target to another character."), this.Marry);
			this.Add("divorce", "", Localization.Get("Divorces the target."), this.Divorce);
			this.Add("duel", "[name]", Localization.Get("Starts or invites to a duel."), this.Duel);

			this.Add("channel", "<name>", Localization.Get("Joins or speaks on a channel."), this.Channel);
			this.Add("join", "<name>", Localization.Get("Joins a channel."), this.Channel);
			this.Add("main", "<message>", Localization.Get("Sends a message on the main channel."), this.MainChat);
			this.Add("fontcolor", "<hex>", Localization.Get("Sets the chat font color."), this.FontColor);
			this.Add("font", "<id>", Localization.Get("Sets the chat font."), this.FontColor);
			this.Add("langtype", "<lang>", Localization.Get("Switches the language."), this.Language);

			this.Add("request", "<message>", Localization.Get("Sends a help request to GMs."), this.Request);
			this.Add("noask", "", Localization.Get("Toggles invitation refusal."), this.NoAsk);

			this.Add("autotrade", "", Localization.Get("Toggles autotrade."), this.AutoTrade);
			this.Add("at", "", Localization.Get("Toggles autotrade."), this.AutoTrade);
			this.Add("autoloot", "[rate]", Localization.Get("Toggles autoloot."), this.AutoLoot);
			this.Add("alootid", "<id>", Localization.Get("Auto-loots a specific item id."), this.ALootId);
			this.Add("showexp", "", Localization.Get("Toggles exp gain display."), this.ShowExp);
			this.Add("showzeny", "", Localization.Get("Toggles zeny gain display."), this.ShowZeny);
			this.Add("showdelay", "", Localization.Get("Toggles skill-delay display."), this.ShowDelay);
			this.Add("feelreset", "", Localization.Get("Resets the target's 'feel' bindings."), this.FeelReset);
			this.Add("reset", "", Localization.Get("Resets stats and skills."), this.Reset);

			this.Add("trade", "<name>", Localization.Get("Starts a trade with a player."), this.Trade);
			this.Add("email", "<address>", Localization.Get("Sets the account's email."), this.Email);
			this.Add("produce", "<id> [count]", Localization.Get("Produces items."), this.Produce);
			this.Add("cash", "<modifier>", Localization.Get("Adjusts cash points."), this.Cash);
			this.Add("points", "<modifier>", Localization.Get("Adjusts kafra points."), this.Points);
		}

		private CommandResult Marry(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!ZoneServer.Instance.World.Maps.TryGetPlayerByName(args.Get(0), out var spouse))
			{
				sender.ServerMessage(Localization.Get("Player '{0}' not found."), args.Get(0));
				return CommandResult.Okay;
			}
			target.MarriedToCharId = spouse.Id;
			spouse.MarriedToCharId = target.Id;
			sender.ServerMessage(Localization.Get("{0} and {1} are now married."), target.Name, spouse.Name);
			return CommandResult.Okay;
		}

		private CommandResult Divorce(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.MarriedToCharId is int spouseId
				&& ZoneServer.Instance.World.Maps.TryGetPlayerById(spouseId, out var spouse))
			{
				spouse.MarriedToCharId = null;
			}
			target.MarriedToCharId = null;
			sender.ServerMessage(Localization.Get("Divorced."));
			return CommandResult.Okay;
		}

		private CommandResult Duel(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var existing = DuelService.Instance.GetByOwner(sender.Id) ?? DuelService.Instance.GetByMember(sender.Id);
			if (existing == null)
			{
				var d = DuelService.Instance.Create(sender);
				sender.ServerMessage(Localization.Get("Duel {0} created."), d.Id);
			}
			else if (args.Count >= 1 && existing.OwnerCharId == sender.Id
				&& ZoneServer.Instance.World.Maps.TryGetPlayerByName(args.Get(0), out var invited))
			{
				existing.Invited.Add(invited.Id);
				invited.ServerMessage(Localization.Get("{0} has invited you to a duel ({1}). Use >accept."), sender.Name, existing.Id);
				sender.ServerMessage(Localization.Get("Invited {0} to duel {1}."), invited.Name, existing.Id);
			}
			else
			{
				sender.ServerMessage(Localization.Get("Duel {0}, {1} member(s)."), existing.Id, existing.Members.Count);
			}
			return CommandResult.Okay;
		}

		private CommandResult Channel(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var ch = ChatChannelService.Instance.GetOrCreate(args.Get(0));
			if (args.Count == 1)
			{
				if (ch.Contains(target)) { ch.Leave(target); sender.ServerMessage(Localization.Get("Left channel {0}."), ch.Name); }
				else { ch.Join(target); sender.ServerMessage(Localization.Get("Joined channel {0}."), ch.Name); }
				return CommandResult.Okay;
			}
			var msg = string.Join(" ", System.Linq.Enumerable.Skip(args.GetAll(), 1));
			ch.Broadcast(string.Format("[#{0}] {1} : {2}", ch.Name, sender.Name, msg));
			return CommandResult.Okay;
		}

		private CommandResult MainChat(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var msg = string.Join(" ", args.GetAll());
			ChatChannelService.Instance.GetOrCreate("main").Broadcast(string.Format("[#main] {0} : {1}", sender.Name, msg));
			return CommandResult.Okay;
		}

		private CommandResult FontColor(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			target.Vars.Perm.SetString("Sabine.FontColor", args.Get(0));
			sender.ServerMessage(Localization.Get("Font color set."));
			return CommandResult.Okay;
		}

		private CommandResult Request(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var msg = string.Join(" ", args.GetAll());
			using var players = Yggdrasil.Collections.PooledList<PlayerCharacter>.Rent();
			ZoneServer.Instance.World.Maps.Do(m => m.GetPlayers(players, 0, static (_, p) => p.Connection.Account.Authority > 0));
			foreach (var p in players)
				p.ServerMessage(Localization.Get("[Request from {0}] {1}"), sender.Name, msg);
			sender.ServerMessage(Localization.Get("Request sent to {0} GM(s)."), players.Count);
			return CommandResult.Okay;
		}

		private CommandResult NoAsk(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var v = target.Vars.Perm.ToggleBool("Sabine.NoAsk");
			sender.ServerMessage(Localization.Get("NoAsk: {0}"), v);
			return CommandResult.Okay;
		}

		private CommandResult AutoTrade(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var v = target.Vars.Perm.ToggleBool("Sabine.AutoTrade");
			sender.ServerMessage(Localization.Get("AutoTrade: {0}"), v);
			return CommandResult.Okay;
		}

		private CommandResult AutoLoot(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var rate = 100;
			if (args.Count >= 1) int.TryParse(args.Get(0), out rate);
			target.Vars.Perm.SetInt("Sabine.AutoLootRate", rate);
			sender.ServerMessage(Localization.Get("AutoLoot rate: {0}"), rate);
			return CommandResult.Okay;
		}

		private CommandResult ALootId(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("alootid is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult ShowExp(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
			{ sender.ServerMessage(Localization.Get("ShowExp: {0}"), target.Vars.Perm.ToggleBool("Sabine.ShowExp")); return CommandResult.Okay; }
		private CommandResult ShowZeny(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
			{ sender.ServerMessage(Localization.Get("ShowZeny: {0}"), target.Vars.Perm.ToggleBool("Sabine.ShowZeny")); return CommandResult.Okay; }
		private CommandResult ShowDelay(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
			{ sender.ServerMessage(Localization.Get("ShowDelay: {0}"), target.Vars.Perm.ToggleBool("Sabine.ShowDelay")); return CommandResult.Okay; }

		private CommandResult FeelReset(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Vars.Perm.SetString("Sabine.Feel", string.Empty);
			sender.ServerMessage(Localization.Get("Feel reset."));
			return CommandResult.Okay;
		}

		private CommandResult Reset(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Reset is not implemented (would zero stats and refund points)."));
			return CommandResult.Okay;
		}

		private CommandResult Trade(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Trade-via-command not implemented; use the trade UI."));
			return CommandResult.Okay;
		}

		private CommandResult Email(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			target.Vars.Perm.SetString("Sabine.Email", args.Get(0));
			sender.ServerMessage(Localization.Get("Email recorded."));
			return CommandResult.Okay;
		}

		private CommandResult Produce(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Produce is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult Cash(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var v)) return CommandResult.InvalidArgument;
			target.Vars.Perm.SetInt("Sabine.Cash", target.Vars.Perm.GetInt("Sabine.Cash", 0) + v);
			sender.ServerMessage(Localization.Get("Cash {0:+#;-#;0}."), v);
			return CommandResult.Okay;
		}

		private CommandResult Points(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var v)) return CommandResult.InvalidArgument;
			target.Vars.Perm.SetInt("Sabine.Points", target.Vars.Perm.GetInt("Sabine.Points", 0) + v);
			sender.ServerMessage(Localization.Get("Points {0:+#;-#;0}."), v);
			return CommandResult.Okay;
		}

		private CommandResult Language(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1)
			{
				var languages = ZoneServer.Instance.Localization.GetLanguages();
				sender.ServerMessage(Localization.Get("Available languages: {0}"), string.Join(", ", languages));
				return CommandResult.InvalidArgument;
			}

			var languageName = args.Get(0);
			if (!ZoneServer.Instance.Localization.Contains(languageName))
			{
				sender.ServerMessage(Localization.Get("Language '{0}' is not available."), languageName);
				return CommandResult.InvalidArgument;
			}

			target.SelectedLanguage = languageName;
			sender.ServerMessage(Localization.Get("Language changed to {0}."), languageName);
			return CommandResult.Okay;
		}
	}
}
