using System.Collections.Generic;
using System.Linq;
using Sabine.Shared;
using Sabine.Shared.Util;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Battlegrounds;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadBattleground()
		{
			this.Add("bglist", "", Localization.Get("Lists active Battleground teams."), this.BgList);
			this.Add("bgcreate", "<map> [respawnX] [respawnY]", Localization.Get("Creates a new Battleground team."), this.BgCreate);
			this.Add("bgjoin", "<bgId>", Localization.Get("Adds the target to a Battleground team."), this.BgJoin);
			this.Add("bgleave", "", Localization.Get("Removes the target from their Battleground team."), this.BgLeave);
			this.Add("bgdisband", "<bgId>", Localization.Get("Disbands a Battleground team."), this.BgDisband);
			this.Add("bgmatch", "<bgIdA> <bgIdB> [winScore]", Localization.Get("Pairs two BGs as opponents in a kill-count match."), this.BgMatch);
			this.Add("bgscore", "<bgId>", Localization.Get("Reports a BG's current score."), this.BgScore);
			this.Add("bgsetcrystal", "<bgId> <monsterHandle>", Localization.Get("Designates a monster as a BG's crystal (Flavius win condition)."), this.BgSetCrystal);
			this.Add("bgchat", "<message>", Localization.Get("Sends a team-only message to your Battleground."), this.BgChat);
		}

		/// <summary>
		/// /bgchat &lt;message&gt; — sends a team-only chat line to every
		/// online member of the sender's BG via
		/// <c>ZC_BATTLEFIELD_CHAT</c>. No-op (with a hint) when the sender
		/// isn't in a BG.
		/// </summary>
		private CommandResult BgChat(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (Game.Version < Versions.S2500)
			{
				sender.ServerMessage(Localization.Get("BG chat requires the S2500+ protocol."));
				return CommandResult.Okay;
			}

			var bg = ZoneServer.Instance.World.Battlegrounds.GetByMember(sender.Id);
			if (bg == null)
			{
				sender.ServerMessage(Localization.Get("You aren't in a Battleground."));
				return CommandResult.Okay;
			}

			// args.Get(0) is just the first token; reuse the raw message
			// suffix so multi-word lines arrive intact.
			var text = message ?? string.Empty;

			var recipients = new List<PlayerCharacter>();
			foreach (var charId in bg.CharacterIds)
			{
				var pc = ResolveOnline(charId);
				if (pc != null)
					recipients.Add(pc);
			}

			Send.ZC_BATTLEFIELD_CHAT(recipients, sender.Handle, sender.Name, text);
			return CommandResult.Okay;
		}

		private CommandResult BgList(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var all = ZoneServer.Instance.World.Battlegrounds.All;
			if (all.Count == 0)
			{
				sender.ServerMessage(Localization.Get("No active Battlegrounds."));
				return CommandResult.Okay;
			}

			foreach (var bg in all)
			{
				sender.ServerMessage(Localization.Get("BG #{0} on {1} — {2} member(s)"),
					bg.Id, bg.MapStringId ?? "(no map)", bg.CharacterIds.Count);
			}
			return CommandResult.Okay;
		}

		private CommandResult BgCreate(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;

			var map = args.Get(0);
			var x = args.Count > 1 && int.TryParse(args.Get(1), out var px) ? px : 0;
			var y = args.Count > 2 && int.TryParse(args.Get(2), out var py) ? py : 0;

			var bg = ZoneServer.Instance.World.Battlegrounds.Create(map, x, y, "", "");
			sender.ServerMessage(Localization.Get("Created Battleground #{0} on {1}."), bg.Id, map);
			return CommandResult.Okay;
		}

		/// <summary>
		/// /bgjoin &lt;bgId&gt; — adds the target to an existing
		/// Battleground. Refuses when the target is already in a BG to
		/// prevent silent team-stomping. eAthena's bg_join refuses the
		/// same way; the team add itself is the only state mutation.
		/// </summary>
		private CommandResult BgJoin(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var bgId)) return CommandResult.InvalidArgument;

			var bgs = ZoneServer.Instance.World.Battlegrounds;
			if (bgs.GetByMember(target.Id) != null)
			{
				sender.ServerMessage(Localization.Get("Target is already in a Battleground; /bgleave first."));
				return CommandResult.Okay;
			}

			var bg = bgs.Get(bgId);
			if (bg == null)
			{
				sender.ServerMessage(Localization.Get("No Battleground with id {0}."), bgId);
				return CommandResult.Okay;
			}

			bg.CharacterIds.Add(target.Id);
			BroadcastCampInfo(bg, target);
			sender.ServerMessage(Localization.Get("{0} joined BG #{1}."), target.Name, bg.Id);
			return CommandResult.Okay;
		}

		/// <summary>
		/// Sends <c>ZC_BATTLEFIELD_NOTIFY_CAMPINFO</c> for the joining
		/// player to every online member of the BG, including the joiner
		/// themself. PV23+ clients use this to populate the team panel
		/// with the new member's name + camp emblem.
		/// </summary>
		private static void BroadcastCampInfo(Battleground bg, PlayerCharacter joiner)
		{
			if (Game.Version < Versions.S2500)
				return;

			var camp = (short)bg.Id;
			foreach (var charId in bg.CharacterIds)
			{
				var pc = ResolveOnline(charId);
				if (pc != null)
					Send.ZC_BATTLEFIELD_NOTIFY_CAMPINFO(pc, joiner.Handle, joiner.Name, camp);
			}
		}

		private static PlayerCharacter ResolveOnline(int charId)
		{
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				if (map.TryGetPlayerById(charId, out var pc))
					return pc;
			}
			return null;
		}

		private CommandResult BgLeave(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var bg = ZoneServer.Instance.World.Battlegrounds.GetByMember(target.Id);
			if (bg == null)
			{
				sender.ServerMessage(Localization.Get("Target isn't in a Battleground."));
				return CommandResult.Okay;
			}

			bg.CharacterIds.Remove(target.Id);
			sender.ServerMessage(Localization.Get("{0} left BG #{1}."), target.Name, bg.Id);
			return CommandResult.Okay;
		}

		private CommandResult BgDisband(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var bgId)) return CommandResult.InvalidArgument;

			var ok = ZoneServer.Instance.World.Battlegrounds.Destroy(bgId);
			sender.ServerMessage(ok
				? Localization.Get("Disbanded BG #{0}.")
				: Localization.Get("No Battleground with id {0}."), bgId);
			return CommandResult.Okay;
		}

		/// <summary>
		/// /bgmatch &lt;bgIdA&gt; &lt;bgIdB&gt; [winScore] — pairs two
		/// Battlegrounds as opponents and resets their scores. The
		/// kill-count listener (BattlegroundScoreListener) reads
		/// OpponentId on every PvP kill and bumps the killer team's
		/// score; first to winScore (default 10) wins, payouts to
		/// every member. Use /bgmatch a b 0 to pause counting.
		/// </summary>
		private CommandResult BgMatch(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 2) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var idA)) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(1), out var idB)) return CommandResult.InvalidArgument;

			var bgs = ZoneServer.Instance.World.Battlegrounds;
			var a = bgs.Get(idA);
			var b = bgs.Get(idB);
			if (a == null || b == null)
			{
				sender.ServerMessage(Localization.Get("One or both BG ids don't exist."));
				return CommandResult.Okay;
			}

			var winScore = args.Count > 2 && int.TryParse(args.Get(2), out var ws) ? ws : 10;
			a.OpponentId = b.Id;
			b.OpponentId = a.Id;
			a.Score = 0; b.Score = 0;
			a.WinScore = winScore; b.WinScore = winScore;
			a.MatchEnded = false; b.MatchEnded = false;

			sender.ServerMessage(Localization.Get("BGs #{0} vs #{1} paired, first to {2}."), a.Id, b.Id, winScore);
			return CommandResult.Okay;
		}

		/// <summary>
		/// /bgsetcrystal &lt;bgId&gt; &lt;monsterHandle&gt; — designates an
		/// existing monster as the BG's crystal. When the monster dies,
		/// the opposing BG wins outright (Flavius-style destroy-the-
		/// crystal win condition), regardless of kill score. Pair this
		/// with /spawn to drop a tough monster on the BG map first,
		/// then call /bgsetcrystal with the spawned handle.
		/// </summary>
		private CommandResult BgSetCrystal(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 2) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var bgId)) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(1), out var handle)) return CommandResult.InvalidArgument;

			var bg = ZoneServer.Instance.World.Battlegrounds.Get(bgId);
			if (bg == null)
			{
				sender.ServerMessage(Localization.Get("No Battleground with id {0}."), bgId);
				return CommandResult.Okay;
			}

			bg.CrystalHandle = handle;
			sender.ServerMessage(Localization.Get("BG #{0} crystal set to handle {1}."), bgId, handle);
			return CommandResult.Okay;
		}

		private CommandResult BgScore(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var bgId)) return CommandResult.InvalidArgument;

			var bg = ZoneServer.Instance.World.Battlegrounds.Get(bgId);
			if (bg == null)
			{
				sender.ServerMessage(Localization.Get("No Battleground with id {0}."), bgId);
				return CommandResult.Okay;
			}

			sender.ServerMessage(Localization.Get("BG #{0}: {1} / {2} (opponent #{3}, ended={4})"),
				bg.Id, bg.Score, bg.WinScore, bg.OpponentId?.ToString() ?? "none", bg.MatchEnded);
			return CommandResult.Okay;
		}
	}
}
