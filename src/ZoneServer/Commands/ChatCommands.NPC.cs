using System;
using System.Linq;
using Sabine.Shared.Util;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadNpc()
		{
			this.Add("shownpc", "<name>", Localization.Get("Reveals a hidden NPC."), this.ShowNpc);
			this.Add("hidenpc", "<name>", Localization.Get("Hides an NPC."), this.HideNpc);
			this.Add("loadnpc", "<path>", Localization.Get("Loads an NPC script file."), this.LoadNpc);
			this.Add("unloadnpc", "<name>", Localization.Get("Unloads an NPC."), this.UnloadNpc);
			this.Add("npctalk", "<name> <text>", Localization.Get("Makes an NPC say a line."), this.NpcTalk);
			this.Add("npctalkc", "<color> <name> <text>", Localization.Get("Makes an NPC say a colored line."), this.NpcTalkC);
			this.Add("tonpc", "<name>", Localization.Get("Warps to an NPC."), this.ToNpc);
			this.Add("npcmove", "<name> <x> <y>", Localization.Get("Moves an NPC."), this.NpcMove);
			this.Add("cleanmap", "", Localization.Get("Removes dropped items on the target's map."), this.CleanMap);
			this.Add("cleanarea", "", Localization.Get("Removes dropped items in the visible area."), this.CleanArea);
			this.Add("addwarp", "<name> <map> <x> <y>", Localization.Get("Adds a warp NPC."), this.AddWarp);
		}

		private static Npc FindNpc(Sabine.Zone.World.Maps.Map map, string ident)
		{
			var npcs = map.GetAllNpcs();
			if (int.TryParse(ident, out var handle))
				return npcs.FirstOrDefault(n => n.Handle == handle);
			return npcs.FirstOrDefault(n => string.Equals(n.Name, ident, StringComparison.OrdinalIgnoreCase));
		}

		private CommandResult ShowNpc(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var npc = FindNpc(target.Map, args.Get(0));
			if (npc == null) { sender.ServerMessage(Localization.Get("NPC '{0}' not found on this map."), args.Get(0)); return CommandResult.Okay; }
			npc.Show();
			sender.ServerMessage(Localization.Get("NPC '{0}' shown."), npc.Name);
			return CommandResult.Okay;
		}

		private CommandResult HideNpc(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var npc = FindNpc(target.Map, args.Get(0));
			if (npc == null) { sender.ServerMessage(Localization.Get("NPC '{0}' not found on this map."), args.Get(0)); return CommandResult.Okay; }
			npc.Hide();
			sender.ServerMessage(Localization.Get("NPC '{0}' hidden."), npc.Name);
			return CommandResult.Okay;
		}

		private CommandResult LoadNpc(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
			=> this.ReloadScripts(sender, target, message, commandName, args);

		private CommandResult UnloadNpc(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var npc = FindNpc(target.Map, args.Get(0));
			if (npc == null) { sender.ServerMessage(Localization.Get("NPC '{0}' not found on this map."), args.Get(0)); return CommandResult.Okay; }
			target.Map.RemoveNpc(npc);
			sender.ServerMessage(Localization.Get("Removed NPC '{0}'."), npc.Name);
			return CommandResult.Okay;
		}

		private CommandResult NpcTalk(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 2) return CommandResult.InvalidArgument;
			var npc = FindNpc(target.Map, args.Get(0));
			if (npc == null) { sender.ServerMessage(Localization.Get("NPC '{0}' not found on this map."), args.Get(0)); return CommandResult.Okay; }
			var text = string.Format("{0} : {1}", npc.Name, string.Join(" ", System.Linq.Enumerable.Skip(args.GetAll(), 1)));
			Send.ZC_NOTIFY_CHAT(npc, text);
			return CommandResult.Okay;
		}

		private CommandResult NpcTalkC(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
			=> this.NpcTalk(sender, target, message, commandName, args);

		private CommandResult ToNpc(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var npc = FindNpc(target.Map, args.Get(0));
			if (npc == null) { sender.ServerMessage(Localization.Get("NPC '{0}' not found on this map."), args.Get(0)); return CommandResult.Okay; }
			sender.Warp(target.MapId, npc.Position);
			sender.ServerMessage(Localization.Get("Warped to NPC '{0}'."), npc.Name);
			return CommandResult.Okay;
		}

		private CommandResult NpcMove(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 3) return CommandResult.InvalidArgument;
			var npc = FindNpc(target.Map, args.Get(0));
			if (npc == null) { sender.ServerMessage(Localization.Get("NPC '{0}' not found on this map."), args.Get(0)); return CommandResult.Okay; }
			if (!int.TryParse(args.Get(1), out var x) || !int.TryParse(args.Get(2), out var y))
				return CommandResult.InvalidArgument;
			npc.Warp(target.MapId, new Position(x, y));
			sender.ServerMessage(Localization.Get("Moved NPC '{0}' to ({1},{2})."), npc.Name, x, y);
			return CommandResult.Okay;
		}

		private CommandResult AddWarp(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("addwarp must be added via NPC script; runtime warp NPC creation is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult CleanMap(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var n = this.RemoveItemsInRange(target.Map, target.Position, int.MaxValue);
			sender.ServerMessage(Localization.Get("Removed {0} item(s) from map."), n);
			return CommandResult.Okay;
		}

		private CommandResult CleanArea(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var n = this.RemoveItemsInRange(target.Map, target.Position, target.Map.VisibleRange);
			sender.ServerMessage(Localization.Get("Removed {0} nearby item(s)."), n);
			return CommandResult.Okay;
		}

		private int RemoveItemsInRange(Sabine.Zone.World.Maps.Map map, Position center, int range)
		{
			var items = new System.Collections.Generic.List<Item>();
			// No "all items on map" accessor; use a large range to approximate
			// a full sweep when the caller wants the whole map.
			map.GetItemsInRange(center, range == int.MaxValue ? 4096 : range, items);
			foreach (var item in items)
			{
				try { map.RemoveItem(item); } catch { }
			}
			return items.Count;
		}
	}
}
