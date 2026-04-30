using System;
using System.Linq;
using System.Text;
using Sabine.Shared.Util;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private static readonly DateTime ServerStartTime = DateTime.UtcNow;

		private void LoadInfo()
		{
			this.Add("where", "", Localization.Get("Displays current location."), this.Where);

			this.Add("who", "[name]", Localization.Get("Lists online players."), this.Who);
			this.Add("whomap", "", Localization.Get("Lists players on the target's map."), this.WhoMap);
			this.Add("whogm", "", Localization.Get("Lists online GM characters."), this.WhoGm);
			this.Add("mapinfo", "", Localization.Get("Shows info about the target's map."), this.MapInfo);
			this.Add("mobinfo", "<id|name>", Localization.Get("Displays info about a monster."), this.MobInfo);
			this.Add("iteminfo", "<id|name>", Localization.Get("Displays info about an item."), this.ItemInfo);
			this.Add("whodrops", "<item>", Localization.Get("Lists monsters that drop an item."), this.WhoDrops);
			this.Add("whereis", "<monster>", Localization.Get("Lists maps that spawn a monster."), this.WhereIs);
			this.Add("version", "", Localization.Get("Displays the server version."), this.Version);
			this.Add("servertime", "", Localization.Get("Displays the current server time."), this.ServerTime);
			this.Add("uptime", "", Localization.Get("Displays the server uptime."), this.Uptime);
			this.Add("commands", "", Localization.Get("Lists available commands."), this.Help);
			this.Add("charcommands", "", Localization.Get("Lists available character-target commands."), this.Help);
			this.Add("rates", "", Localization.Get("Displays server rates."), this.Rates);
			this.Add("skilltree", "<job>", Localization.Get("Displays a job's skill tree."), this.SkillTree);
			this.Add("stats", "", Localization.Get("Displays the target's stats."), this.Stats);
			this.Add("idsearch", "<text>", Localization.Get("Searches item names."), this.IdSearch);
			this.Add("gat", "", Localization.Get("Displays the gat-cell type at the current position."), this.Gat);
			this.Add("itemlist", "", Localization.Get("Lists items in the target's inventory."), this.ItemList);
			this.Add("storagelist", "", Localization.Get("Lists items in the target's storage."), this.StorageList);
			this.Add("cartlist", "", Localization.Get("Lists items in the target's cart."), this.CartList);

			this.AddAlias("mobinfo", "monsterinfo");
			this.AddAlias("mobinfo", "mi");
			this.AddAlias("iteminfo", "ii");
			this.AddAlias("who", "whois");
			this.AddAlias("who", "who2");
			this.AddAlias("who", "who3");
			this.AddAlias("servertime", "time");
			this.AddAlias("servertime", "date");
			this.AddAlias("help", "h");
		}

		private CommandResult Who(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			using var players = PooledList<PlayerCharacter>.Rent();
			ZoneServer.Instance.World.Maps.Do(m => m.GetPlayers(players, 0, static (_, _) => true));
			var filter = args.Count > 0 ? args.Get(0).ToLowerInvariant() : null;
			var n = 0;
			foreach (var p in players)
			{
				if (filter != null && !p.Name.ToLowerInvariant().Contains(filter)) continue;
				sender.ServerMessage("{0} (job {1}, lv {2}) — {3}", p.Name, p.JobId, p.Parameters.BaseLevel, p.Map.StringId);
				if (++n >= 50) { sender.ServerMessage("..."); break; }
			}
			sender.ServerMessage(Localization.Get("Total: {0}"), players.Count);
			return CommandResult.Okay;
		}

		private CommandResult WhoMap(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			using var players = PooledList<PlayerCharacter>.Rent();
			target.Map.GetPlayers(players, 0, static (_, _) => true);
			foreach (var p in players)
				sender.ServerMessage("{0} (lv {1})", p.Name, p.Parameters.BaseLevel);
			sender.ServerMessage(Localization.Get("Players on map: {0}"), players.Count);
			return CommandResult.Okay;
		}

		private CommandResult WhoGm(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			using var players = PooledList<PlayerCharacter>.Rent();
			ZoneServer.Instance.World.Maps.Do(m => m.GetPlayers(players, 0, static (_, p) => p.Connection.Account.Authority > 0));
			foreach (var p in players)
				sender.ServerMessage("{0} [auth {1}]", p.Name, p.Connection.Account.Authority);
			sender.ServerMessage(Localization.Get("Online GMs: {0}"), players.Count);
			return CommandResult.Okay;
		}

		private CommandResult MapInfo(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var m = target.Map;
			sender.ServerMessage("Map: {0} ({1})", m.StringId, m.Id);
			sender.ServerMessage("Players: {0}", m.PlayerCount);
			sender.ServerMessage("Flags: {0}", m.Flags);
			return CommandResult.Okay;
		}

		private CommandResult MobInfo(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var ident = args.Get(0);
			var data = int.TryParse(ident, out var id)
				? ZoneServer.Instance.Data.Monsters.Find(a => (int)a.Id == id)
				: ZoneServer.Instance.Data.Monsters.Find(a => string.Equals(a.Name, ident, StringComparison.OrdinalIgnoreCase));
			if (data == null) { sender.ServerMessage(Localization.Get("Monster not found.")); return CommandResult.Okay; }

			sender.ServerMessage("[{0}] {1} (id {2})", data.Id, data.Name, (int)data.Id);
			return CommandResult.Okay;
		}

		private CommandResult ItemInfo(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var ident = args.Get(0);
			var data = int.TryParse(ident, out var id)
				? ZoneServer.Instance.Data.Items.Find(a => a.ClassId == id)
				: ZoneServer.Instance.Data.Items.Find(a => string.Equals(a.Name, ident, StringComparison.OrdinalIgnoreCase));
			if (data == null) { sender.ServerMessage(Localization.Get("Item not found.")); return CommandResult.Okay; }

			sender.ServerMessage("[{0}] {1}", data.ClassId, data.Name);
			return CommandResult.Okay;
		}

		private CommandResult WhoDrops(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var ident = args.Get(0);
			var classId = 0;
			if (!int.TryParse(ident, out classId))
			{
				var data = ZoneServer.Instance.Data.Items.Find(a => string.Equals(a.Name, ident, StringComparison.OrdinalIgnoreCase));
				if (data == null) { sender.ServerMessage(Localization.Get("Item not found.")); return CommandResult.Okay; }
				classId = data.ClassId;
			}

			var hits = 0;
			foreach (var m in ZoneServer.Instance.Data.Monsters.Entries.Values)
			{
				var drop = m.Drops?.FirstOrDefault(d => d.ItemId == classId);
				if (drop != null)
				{
					sender.ServerMessage("{0} (id {1}) — {2:0.##}%", m.Name, (int)m.Id, drop.Chance);
					if (++hits >= 25) { sender.ServerMessage("..."); break; }
				}
			}
			sender.ServerMessage(Localization.Get("Drop sources: {0}"), hits);
			return CommandResult.Okay;
		}

		private CommandResult WhereIs(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var ident = args.Get(0);

			Sabine.Shared.Const.IdentityId targetId;
			if (int.TryParse(ident, out var idInt))
				targetId = (Sabine.Shared.Const.IdentityId)idInt;
			else if (ZoneServer.Instance.Data.Monsters.TryFind(ident, out var data))
				targetId = data.Id;
			else { sender.ServerMessage(Localization.Get("Monster '{0}' not found."), ident); return CommandResult.Okay; }

			var hits = 0;
			ZoneServer.Instance.World.Maps.Do(m =>
			{
				using var monsters = Yggdrasil.Collections.PooledList<Monster>.Rent();
				m.GetMonsters(monsters, targetId, static (s, x) => x.IdentityId == s);
				if (monsters.Count > 0)
				{
					sender.ServerMessage("{0}: {1} live", m.StringId, monsters.Count);
					hits++;
				}
			});
			sender.ServerMessage(Localization.Get("Maps with live spawns: {0}"), hits);
			return CommandResult.Okay;
		}

		private CommandResult Version(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage("Sabine — protocol {0}", Sabine.Shared.Game.Version);
			return CommandResult.Okay;
		}

		private CommandResult ServerTime(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage("{0:yyyy-MM-dd HH:mm:ss} UTC", DateTime.UtcNow);
			return CommandResult.Okay;
		}

		private CommandResult Uptime(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var up = DateTime.UtcNow - ServerStartTime;
			sender.ServerMessage("Up {0} d {1} h {2} m", up.Days, up.Hours, up.Minutes);
			return CommandResult.Okay;
		}

		private CommandResult Rates(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Server rate display is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult SkillTree(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Skill tree display is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult Stats(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var p = target.Parameters;
			sender.ServerMessage("{0} — Lv{1} {2}/{3} HP {4}/{5} SP {6}/{7}", target.Name, p.BaseLevel, p.JobLevel, target.JobId, p.Hp, p.HpMax, p.Sp, p.SpMax);
			sender.ServerMessage("Str{0} Agi{1} Vit{2} Int{3} Dex{4} Luk{5}", p.Str, p.Agi, p.Vit, p.Int, p.Dex, p.Luk);
			return CommandResult.Okay;
		}

		private CommandResult IdSearch(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var q = args.Get(0).ToLowerInvariant();
			var results = ZoneServer.Instance.Data.Items.Entries.Values.Where(i => i.Name != null && i.Name.ToLowerInvariant().Contains(q)).Take(20).ToList();
			foreach (var r in results)
				sender.ServerMessage("[{0}] {1}", r.ClassId, r.Name);
			sender.ServerMessage(Localization.Get("Found {0} item(s)."), results.Count);
			return CommandResult.Okay;
		}

		private CommandResult Gat(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage("Gat at ({0},{1}) on {2}", target.Position.X, target.Position.Y, target.Map.StringId);
			return CommandResult.Okay;
		}

		private CommandResult ItemList(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var sb = new StringBuilder();
			foreach (var item in target.Inventory.GetItems())
			{
				if (sb.Length > 0) sb.Append(", ");
				sb.AppendFormat("{0} x{1}", item.Data?.Name ?? item.ClassId.ToString(), item.Amount);
				if (sb.Length > 80) { sender.ServerMessage(sb.ToString()); sb.Clear(); }
			}
			if (sb.Length > 0) sender.ServerMessage(sb.ToString());
			return CommandResult.Okay;
		}

		private CommandResult StorageList(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Storage listing is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult CartList(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Cart listing is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult Where(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var mapStringId = target.Map.StringId;
			var mapName = target.Map.Data.Name;
			var x = target.Position.X;
			var y = target.Position.Y;
			var dir = target.Direction;

			var firstLine = sender == target
				? Localization.Get("You're here: {1}")
				: Localization.Get("{0} is here: {1}");

			sender.ServerMessage(firstLine, target.Name, mapName);
			sender.ServerMessage(Localization.Get("Coordinates: {0}, {1}, {2}"), mapStringId, x, y);
			sender.ServerMessage(Localization.Get("Direction: {0} ({1})"), dir, (int)dir);
			return CommandResult.Okay;
		}
	}
}
