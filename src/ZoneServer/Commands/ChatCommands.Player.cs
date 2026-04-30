using Sabine.Shared.Const;
using Sabine.Shared.Util;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadPlayer()
		{
			this.Add("stat", "<str|agi|vit|int|dex|luck|stp|skp> <modifier>", Localization.Get("Modifies the character's stats."), this.Stat);
			this.Add("item", "<item> [amount]", Localization.Get("Spawns item for character."), this.Item);
			this.Add("heal", "", Localization.Get("Restores character's health."), this.Heal);
			this.Add("level", "<level>", Localization.Get("Sets the character's base level."), this.Level);
			this.Add("speed", "<speed>", Localization.Get("Sets the character's speed."), this.Speed);
			this.Add("zeny", "<modifier>", Localization.Get("Changes the character's zeny."), this.Zeny);

			this.Add("str", "<modifier>", Localization.Get("Modifies STR."), this.StatStr);
			this.Add("agi", "<modifier>", Localization.Get("Modifies AGI."), this.StatAgi);
			this.Add("vit", "<modifier>", Localization.Get("Modifies VIT."), this.StatVit);
			this.Add("int", "<modifier>", Localization.Get("Modifies INT."), this.StatInt);
			this.Add("dex", "<modifier>", Localization.Get("Modifies DEX."), this.StatDex);
			this.Add("luk", "<modifier>", Localization.Get("Modifies LUK."), this.StatLuk);
			this.Add("luck", "<modifier>", Localization.Get("Modifies LUK."), this.StatLuk);
			this.Add("statall", "<value>", Localization.Get("Sets all stats."), this.StatAll);
			this.Add("allstats", "<value>", Localization.Get("Sets all stats."), this.StatAll);
			this.Add("allstat", "<value>", Localization.Get("Sets all stats."), this.StatAll);
			this.Add("statsall", "<value>", Localization.Get("Sets all stats."), this.StatAll);
			this.Add("stpoint", "<modifier>", Localization.Get("Adjusts the target's stat points."), this.StPoint);
			this.Add("skpoint", "<modifier>", Localization.Get("Adjusts the target's skill points."), this.SkPoint);

			this.Add("dropall", "", Localization.Get("Drops the target's entire inventory."), this.DropAll);
			this.Add("storeall", "", Localization.Get("Sends all of the target's items to storage."), this.StoreAll);
			this.Add("itemreset", "", Localization.Get("Removes all items from the target's inventory."), this.ItemReset);
			this.Add("identify", "", Localization.Get("Identifies all unidentified items in the target's inventory."), this.Identify);
			this.Add("refine", "<slot> <amount>", Localization.Get("Refines an equipped item."), this.Refine);
			this.Add("repairall", "", Localization.Get("Repairs the target's broken equipment."), this.RepairAll);
			this.Add("joblvl", "<level>", Localization.Get("Sets the target's job level."), this.JobLvl);

			this.AddAlias("level", "blvl");
			this.AddAlias("level", "blevel");
			this.AddAlias("level", "baselvl");
			this.AddAlias("level", "baselvup");
			this.AddAlias("level", "baselevel");
			this.AddAlias("level", "baselvlup");
			this.AddAlias("joblvl", "jlvl");
			this.AddAlias("joblvl", "jlevel");
			this.AddAlias("joblvl", "joblevel");
			this.AddAlias("joblvl", "joblvup");
			this.AddAlias("joblvl", "joblvlup");
		}

		private CommandResult ModifyStat(PlayerCharacter sender, PlayerCharacter target, Arguments args, ParameterType type, string label)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var modifier)) return CommandResult.InvalidArgument;
			target.Parameters.Modify(type, modifier);
			sender.ServerMessage(Localization.Get("{0} modified by {1}."), label, modifier);
			return CommandResult.Okay;
		}

		private CommandResult StatStr(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ModifyStat(sender, target, args, ParameterType.Str, "STR");
		private CommandResult StatAgi(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ModifyStat(sender, target, args, ParameterType.Agi, "AGI");
		private CommandResult StatVit(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ModifyStat(sender, target, args, ParameterType.Vit, "VIT");
		private CommandResult StatInt(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ModifyStat(sender, target, args, ParameterType.Int, "INT");
		private CommandResult StatDex(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ModifyStat(sender, target, args, ParameterType.Dex, "DEX");
		private CommandResult StatLuk(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ModifyStat(sender, target, args, ParameterType.Luk, "LUK");
		private CommandResult StPoint(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ModifyStat(sender, target, args, ParameterType.StatPoints, "Stat points");
		private CommandResult SkPoint(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ModifyStat(sender, target, args, ParameterType.SkillPoints, "Skill points");

		private CommandResult StatAll(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var v)) return CommandResult.InvalidArgument;
			var p = target.Parameters;
			p.Modify(ParameterType.Str, v - p.Str);
			p.Modify(ParameterType.Agi, v - p.Agi);
			p.Modify(ParameterType.Vit, v - p.Vit);
			p.Modify(ParameterType.Int, v - p.Int);
			p.Modify(ParameterType.Dex, v - p.Dex);
			p.Modify(ParameterType.Luk, v - p.Luk);
			sender.ServerMessage(Localization.Get("All stats set to {0}."), v);
			return CommandResult.Okay;
		}

		private CommandResult DropAll(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var n = 0;
			foreach (var item in target.Inventory.GetItems())
			{
				try { target.Inventory.RemoveItem(item); n++; } catch { }
			}
			sender.ServerMessage(Localization.Get("Dropped {0} item(s)."), n);
			return CommandResult.Okay;
		}

		private CommandResult StoreAll(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Storage transfer is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult ItemReset(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var n = 0;
			foreach (var item in target.Inventory.GetItems())
			{
				try { target.Inventory.RemoveItem(item); n++; } catch { }
			}
			sender.ServerMessage(Localization.Get("Removed {0} item(s)."), n);
			return CommandResult.Okay;
		}

		private CommandResult Identify(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var n = 0;
			foreach (var item in target.Inventory.GetItems())
			{
				if (!item.IsIdentified) { item.IsIdentified = true; n++; }
			}
			sender.ServerMessage(Localization.Get("Identified {0} item(s)."), n);
			return CommandResult.Okay;
		}

		private CommandResult Refine(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var amount = 1;
			if (args.Count >= 1 && !int.TryParse(args.Get(0), out amount))
				return CommandResult.InvalidArgument;

			var n = 0;
			foreach (var item in target.Inventory.GetItems())
			{
				if (!item.IsEquipped) continue;
				var newLevel = System.Math.Max(0, item.RefineLevel + amount);
				item.RefineLevel = (byte)System.Math.Min(byte.MaxValue, newLevel);
				n++;
			}
			sender.ServerMessage(Localization.Get("Refined {0} equipped item(s) by {1}."), n, amount);
			return CommandResult.Okay;
		}

		private CommandResult RepairAll(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var n = 0;
			foreach (var item in target.Inventory.GetItems())
			{
				if (item.IsDamaged) { item.IsDamaged = false; n++; }
			}
			sender.ServerMessage(Localization.Get("Repaired {0} item(s)."), n);
			return CommandResult.Okay;
		}

		private CommandResult JobLvl(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var lvl)) return CommandResult.InvalidArgument;
			target.Parameters.JobLevel = lvl;
			Send.ZC_PAR_CHANGE(target, ParameterType.JobLevel);
			sender.ServerMessage(Localization.Get("Job level set to {0}."), lvl);
			return CommandResult.Okay;
		}

		private CommandResult Stat(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 2) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(1), out var modifier)) return CommandResult.InvalidArgument;

			var type = args.Get(0);
			switch (type)
			{
				case "str": target.Parameters.Modify(ParameterType.Str, modifier); break;
				case "agi": target.Parameters.Modify(ParameterType.Agi, modifier); break;
				case "vit": target.Parameters.Modify(ParameterType.Vit, modifier); break;
				case "int": target.Parameters.Modify(ParameterType.Int, modifier); break;
				case "dex": target.Parameters.Modify(ParameterType.Dex, modifier); break;
				case "luk": target.Parameters.Modify(ParameterType.Luk, modifier); break;
				case "stp": target.Parameters.Modify(ParameterType.StatPoints, modifier); break;
				case "skp": target.Parameters.Modify(ParameterType.SkillPoints, modifier); break;
				default:
					sender.ServerMessage(Localization.Get("Unknown stat type '{0}'."), type);
					return CommandResult.Okay;
			}

			sender.ServerMessage(Localization.Get("Stat {0} has been modified by {1}."), type, modifier);
			if (sender != target) target.ServerMessage(Localization.Get("{0} has modified your stats."), sender.Name);
			return CommandResult.Okay;
		}

		private CommandResult Item(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;

			var itemIdent = args.Get(0);
			if (!int.TryParse(itemIdent, out var classId))
			{
				var itemData = ZoneServer.Instance.Data.Items.Find(a => a.Name == itemIdent);
				if (itemData == null)
				{
					sender.ServerMessage(Localization.Get("Item '{0}' not found."), itemIdent);
					return CommandResult.Okay;
				}
				classId = itemData.ClassId;
			}
			else if (!ZoneServer.Instance.Data.Items.Contains(classId))
			{
				sender.ServerMessage(Localization.Get("Item with id '{0}' not found."), classId);
				return CommandResult.Okay;
			}

			var amount = 1;
			if (args.Count > 1)
			{
				if (!int.TryParse(args.Get(1), out amount)) return CommandResult.InvalidArgument;
			}

			var item = new Sabine.Zone.World.Actors.Item(classId);
			item.Amount = System.Math.Max(1, amount);
			target.Inventory.AddItem(item);

			sender.ServerMessage(Localization.Get("Item '{0}' was added to inventory."), item.Data.Name);
			if (target != sender)
				target.ServerMessage(Localization.Get("{0} added item '{1}' to your inventory."), sender.Name, item.Data.Name);
			return CommandResult.Okay;
		}

		private CommandResult Heal(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Heal();
			sender.ServerMessage(Localization.Get("Healed."));
			if (target != sender)
				target.ServerMessage(Localization.Get("You were healed by {0}."), sender.Name);
			return CommandResult.Okay;
		}

		private CommandResult Level(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var newLevel)) return CommandResult.InvalidArgument;
			target.Parameters.BaseLevel = newLevel;
			Send.ZC_PAR_CHANGE(target, ParameterType.BaseLevel);
			sender.ServerMessage(Localization.Get("Base level was set to {0}."), newLevel);
			if (target != sender)
				target.ServerMessage(Localization.Get("Your base level was set to {0} by {1}."), newLevel, sender.Name);
			return CommandResult.Okay;
		}

		private CommandResult Speed(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var speed = 200;
			if (args.Count > 0)
			{
				if (!int.TryParse(args.Get(0), out speed)) return CommandResult.InvalidArgument;
			}
			target.Parameters.Speed = speed;
			Send.ZC_PAR_CHANGE(target, ParameterType.Speed);
			sender.ServerMessage(Localization.Get("Speed was set to {0}."), speed);
			if (target != sender)
				target.ServerMessage(Localization.Get("Your speed was set to {0} by {1}."), speed, sender.Name);
			return CommandResult.Okay;
		}

		private CommandResult Zeny(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var modifier)) return CommandResult.InvalidArgument;
			target.Parameters.Modify(ParameterType.Zeny, modifier);
			sender.ServerMessage(Localization.Get("Zeny has been modified by {0}."), modifier);
			if (sender != target) target.ServerMessage(Localization.Get("Your zeny were modified by {0}."), sender.Name);
			return CommandResult.Okay;
		}
	}
}
