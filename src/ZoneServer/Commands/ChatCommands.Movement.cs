using Sabine.Shared.Util;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadMovement()
		{
			this.Add("warp", "<map> <x> <y>", Localization.Get("Warps player to destination."), this.Warp);
			this.Add("jump", "[[+-]x] [[+-]y]", Localization.Get("Warps player to another position on the same map."), this.Jump);
			this.Add("save", "[map] [x] [y]", Localization.Get("Sets the target's save point."), this.Save);
			this.Add("return", "", Localization.Get("Warps target to their save point."), this.Return);
			this.Add("load", "", Localization.Get("Warps target to their save point."), this.Return);
			this.Add("go", "<index>", Localization.Get("Warps to a known town by index/name."), this.Go);
			this.Add("follow", "", Localization.Get("Toggles following on the target."), this.Follow);
			this.Add("memo", "", Localization.Get("Saves a memo of the current location."), this.Memo);
			this.Add("refresh", "", Localization.Get("Refreshes the target's view."), this.Refresh);

			this.AddAlias("warp", "rura");
			this.AddAlias("warp", "mapmove");
			this.AddAlias("jump", "jumpto");
			this.AddAlias("jump", "warpto");
		}

		private CommandResult Warp(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1)
				return CommandResult.InvalidArgument;

			var mapIdent = args.Get(0).Trim(',', '"');

			if (!int.TryParse(mapIdent, out var mapId))
			{
				if (mapIdent.EndsWith(".gat"))
					mapIdent = mapIdent.Substring(0, mapIdent.Length - 4);

				if (!ZoneServer.Instance.Data.Maps.TryFind(mapIdent, out var mapData))
				{
					sender.ServerMessage(Localization.Get("Map '{0}' not found."), mapIdent);
					return CommandResult.Okay;
				}

				mapId = mapData.Id;
			}

			Position warpPos;

			if (args.Count >= 3)
			{
				if (!int.TryParse(args.Get(1).Trim(','), out var x))
					return CommandResult.InvalidArgument;
				if (!int.TryParse(args.Get(2).Trim(','), out var y))
					return CommandResult.InvalidArgument;
				warpPos = new Position(x, y);
			}
			else
			{
				warpPos = target.Map.GetRandomWalkablePosition();
			}

			if (!ZoneServer.Instance.World.Maps.TryGet(mapId, out var map))
			{
				sender.ServerMessage(Localization.Get("Map not found."));
				return CommandResult.Okay;
			}

			target.Warp(map.Id, warpPos);

			sender.ServerMessage(Localization.Get("Warped to {0}, {1}, {2}."), map.StringId, warpPos.X, warpPos.Y);
			if (sender != target)
				target.ServerMessage(Localization.Get("You were warped by {0}."), sender.Name);

			return CommandResult.Okay;
		}

		private CommandResult Jump(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			Position warpPos;

			if (args.Count >= 2)
			{
				var xStr = args.Get(0);
				var yStr = args.Get(1);

				if (!int.TryParse(xStr, out var x))
					return CommandResult.InvalidArgument;
				if (!int.TryParse(yStr, out var y))
					return CommandResult.InvalidArgument;

				if (xStr.StartsWith('+') || xStr.StartsWith('-') || yStr.StartsWith('+') || yStr.StartsWith('-'))
				{
					var pos = target.Position;
					x += pos.X;
					y += pos.Y;
				}

				warpPos = new Position(x, y);
			}
			else
			{
				warpPos = target.Map.GetRandomWalkablePosition();
			}

			target.Warp(target.MapId, warpPos);

			sender.ServerMessage(Localization.Get("Warped to {0}, {1}."), warpPos.X, warpPos.Y);
			if (sender != target)
				target.ServerMessage(Localization.Get("You were warped by {0}."), sender.Name);

			return CommandResult.Okay;
		}

		private CommandResult Save(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count >= 3)
			{
				var mapIdent = args.Get(0);
				if (!ZoneServer.Instance.Data.Maps.TryFind(mapIdent, out var mapData))
				{
					sender.ServerMessage(Localization.Get("Map '{0}' not found."), mapIdent);
					return CommandResult.Okay;
				}
				if (!int.TryParse(args.Get(1), out var x) || !int.TryParse(args.Get(2), out var y))
					return CommandResult.InvalidArgument;
				target.SaveLocation = new Location(mapData.Id, new Position(x, y));
			}
			else
			{
				target.SaveLocation = new Location(target.MapId, target.Position);
			}
			sender.ServerMessage(Localization.Get("Save point set: {0} ({1},{2})."), target.SaveLocation.MapId, target.SaveLocation.Position.X, target.SaveLocation.Position.Y);
			return CommandResult.Okay;
		}

		private CommandResult Return(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.SaveLocation.MapId == 0)
			{
				sender.ServerMessage(Localization.Get("No save point."));
				return CommandResult.Okay;
			}
			target.Warp(target.SaveLocation);
			return CommandResult.Okay;
		}

		private static readonly (string name, string map, int x, int y)[] GoDestinations = new[]
		{
			("prontera", "prontera", 156, 191),
			("morroc", "morocc", 156, 93),
			("geffen", "geffen", 119, 59),
			("payon", "payon", 162, 233),
			("alberta", "alberta", 28, 234),
			("izlude", "izlude", 128, 114),
			("aldebaran", "alde_alche", 168, 112),
			("comodo", "comodo", 209, 143),
			("yuno", "yuno", 157, 51),
			("amatsu", "amatsu", 198, 84),
			("gonryun", "gonryun", 160, 121),
			("umbala", "umbala", 89, 157),
			("niflheim", "niflheim", 21, 153),
			("louyang", "louyang", 217, 100),
			("jail", "prt_jail", 60, 60),
		};

		private CommandResult Go(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var key = args.Get(0).ToLowerInvariant();
			(string name, string map, int x, int y) dest = default;
			var found = false;
			if (int.TryParse(key, out var idx) && idx >= 0 && idx < GoDestinations.Length)
			{
				dest = GoDestinations[idx];
				found = true;
			}
			else
			{
				foreach (var d in GoDestinations)
				{
					if (d.name == key) { dest = d; found = true; break; }
				}
			}

			if (!found)
			{
				var i = 0;
				foreach (var d in GoDestinations)
					sender.ServerMessage("{0}: {1}", i++, d.name);
				return CommandResult.Okay;
			}

			if (!ZoneServer.Instance.Data.Maps.TryFind(dest.map, out var mapData))
			{
				sender.ServerMessage(Localization.Get("Destination map '{0}' not loaded."), dest.map);
				return CommandResult.Okay;
			}
			target.Warp(mapData.Id, new Position(dest.x, dest.y));
			return CommandResult.Okay;
		}

		private CommandResult Follow(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var v = sender.Vars.Temp.ToggleBool("Sabine.Following");
			sender.ServerMessage(Localization.Get("Follow: {0}"), v);
			return CommandResult.Okay;
		}

		private CommandResult Memo(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.Vars.Perm.SetString("Sabine.Memo", string.Format("{0},{1},{2}", target.MapId, target.Position.X, target.Position.Y));
			sender.ServerMessage(Localization.Get("Memo saved."));
			return CommandResult.Okay;
		}

		private CommandResult Refresh(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Warp(target.MapId, target.Position);
			return CommandResult.Okay;
		}
	}
}
