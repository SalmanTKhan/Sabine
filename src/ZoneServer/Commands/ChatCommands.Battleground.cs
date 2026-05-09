using System.Linq;
using Sabine.Shared.Util;
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
			sender.ServerMessage(Localization.Get("{0} joined BG #{1}."), target.Name, bg.Id);
			return CommandResult.Okay;
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
	}
}
