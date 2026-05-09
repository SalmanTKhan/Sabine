using Sabine.Shared.Const;
using Sabine.Shared.Util;
using Sabine.Zone.Network;
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
			if (!Scripting.Shortcuts.Marry(target, spouse))
			{
				sender.ServerMessage(Localization.Get("Marriage refused (already married, or invalid pairing)."));
				return CommandResult.Okay;
			}
			sender.ServerMessage(Localization.Get("{0} and {1} are now married."), target.Name, spouse.Name);
			return CommandResult.Okay;
		}

		private CommandResult Divorce(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (!Scripting.Shortcuts.Divorce(target))
			{
				sender.ServerMessage(Localization.Get("Not married."));
				return CommandResult.Okay;
			}
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

		// eAthena atcommand_autolootitem: a per-character whitelist of up
		// to 10 item ids that survive the auto-loot filter regardless of
		// the global rate. No-arg lists / clears the current set; an int
		// argument toggles membership; "reset" / "clear" empties the list.
		// NOTE: Sabine's current pickup pipeline doesn't yet consult this
		// list — the same gap exists on /autoloot, which only stores
		// Sabine.AutoLootRate without a read-side consumer. Persisting the
		// set still means the toggle survives a relog so the filter goes
		// live the moment the pickup hook lands.
		private CommandResult ALootId(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			const string varName = "Sabine.AutoLootIds";
			const int maxIds = 10;

			var existing = target.Vars.Perm.GetIntArray(varName);
			var ids = existing != null ? new System.Collections.Generic.List<int>(existing) : new System.Collections.Generic.List<int>();

			if (args.Count == 0)
			{
				if (ids.Count == 0)
					sender.ServerMessage(Localization.Get("AutoLootIds: (empty)"));
				else
					sender.ServerMessage("AutoLootIds: {0}", string.Join(", ", ids));
				return CommandResult.Okay;
			}

			var arg = args.Get(0);
			if (arg.Equals("reset", System.StringComparison.OrdinalIgnoreCase) || arg.Equals("clear", System.StringComparison.OrdinalIgnoreCase))
			{
				target.Vars.Perm.SetIntArray(varName, System.Array.Empty<int>());
				sender.ServerMessage(Localization.Get("AutoLootIds cleared."));
				return CommandResult.Okay;
			}

			if (!int.TryParse(arg, out var itemId) || itemId <= 0)
				return CommandResult.InvalidArgument;

			if (ids.Remove(itemId))
			{
				target.Vars.Perm.SetIntArray(varName, ids);
				sender.ServerMessage(Localization.Get("Removed {0} from AutoLootIds."), itemId);
				return CommandResult.Okay;
			}

			if (ids.Count >= maxIds)
			{
				sender.ServerMessage(Localization.Get("AutoLootIds full ({0} max). Drop one with /alootid <existing id> first."), maxIds);
				return CommandResult.Okay;
			}

			ids.Add(itemId);
			target.Vars.Perm.SetIntArray(varName, ids);
			sender.ServerMessage(Localization.Get("Added {0} to AutoLootIds."), itemId);
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
			var refundedStats = ResetStats(target);
			var refundedSkills = ResetSkills(target);

			target.Parameters.RecalculateAll();
			target.Skills.RefreshClient();

			sender.ServerMessage(Localization.Get("Reset: refunded {0} stat point(s) and {1} skill point(s)."), refundedStats, refundedSkills);
			if (sender != target)
				target.ServerMessage(Localization.Get("Your stats and skills have been reset by {0}."), sender.Name);

			return CommandResult.Okay;
		}

		// eAthena pc_resetstate: stat points spent on a stat from base 1
		// to N equal sum_{i=1..N-1} (1 + (i+9)/10), so we walk that here
		// to refund accurately. Stats settle back to 1 after the refund.
		private static int ResetStats(PlayerCharacter target)
		{
			var p = target.Parameters;
			var refund = StatRefund(p.Str) + StatRefund(p.Agi) + StatRefund(p.Vit)
				+ StatRefund(p.Int) + StatRefund(p.Dex) + StatRefund(p.Luk);

			p.Set(ParameterType.Str, 1); Send.ZC_PAR_CHANGE(target, ParameterType.Str);
			p.Set(ParameterType.Agi, 1); Send.ZC_PAR_CHANGE(target, ParameterType.Agi);
			p.Set(ParameterType.Vit, 1); Send.ZC_PAR_CHANGE(target, ParameterType.Vit);
			p.Set(ParameterType.Int, 1); Send.ZC_PAR_CHANGE(target, ParameterType.Int);
			p.Set(ParameterType.Dex, 1); Send.ZC_PAR_CHANGE(target, ParameterType.Dex);
			p.Set(ParameterType.Luk, 1); Send.ZC_PAR_CHANGE(target, ParameterType.Luk);

			p.Modify(ParameterType.StatPoints, refund);
			return refund;
		}

		private static int StatRefund(int currentValue)
		{
			var spent = 0;
			for (var i = 1; i < currentValue; i++)
				spent += 1 + (i + 9) / 10;
			return spent;
		}

		// eAthena pc_resetskill: zero every tree skill the player has
		// learned and refund one skill point per consumed level. Quest
		// skills outside the class tree are left alone.
		private static int ResetSkills(PlayerCharacter target)
		{
			if (!ZoneServer.Instance.Data.SkillTrees.TryFind(target.JobId, out var treeData))
				return 0;

			var refund = 0;
			foreach (var entry in treeData.Skills)
			{
				if (!target.Skills.TryGet(entry.SkillId, out var skill) || skill.Level <= 0)
					continue;

				refund += skill.Level;
				skill.Level = 0;
			}

			if (refund > 0)
				target.Parameters.Modify(ParameterType.SkillPoints, refund);

			return refund;
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
