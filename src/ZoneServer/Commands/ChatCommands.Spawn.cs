using System;
using System.Linq;
using Sabine.Shared.Const;
using Sabine.Shared.Util;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;
using Yggdrasil.Util;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadSpawn()
		{
			this.Add("spawn", "<monster id|name> [amount] [hp:amount] [ai:none|name]", Localization.Get("Spawns monsters."), this.Spawn);
			this.AddAlias("spawn", "monster");
			this.Add("summon", "<monster>", Localization.Get("Summons a monster as your slave."), this.Summon);
			this.Add("killmonster", "", Localization.Get("Kills every monster on the target's map."), this.KillMonster);
			this.Add("killmonster2", "", Localization.Get("Kills every monster on the target's map (alternate)."), this.KillMonster);
			this.Add("mobsearch", "<monster>", Localization.Get("Locates spawned monsters by id/name on the current map."), this.MobSearch);
			this.Add("showmobs", "<monster>", Localization.Get("Highlights matching monsters on the map."), this.MobSearch);
			this.Add("clone", "<name>", Localization.Get("Spawns a clone of the target player."), this.Clone);
			this.Add("slaveclone", "<name>", Localization.Get("Spawns a slave clone of the target."), this.Clone);
			this.Add("evilclone", "<name>", Localization.Get("Spawns a hostile clone of the target."), this.Clone);
			this.Add("monstersmall", "<monster>", Localization.Get("Spawns a small-size monster."), this.Spawn);
			this.Add("monsterbig", "<monster>", Localization.Get("Spawns a big-size monster."), this.Spawn);
		}

		private CommandResult Summon(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var ident = args.Get(0);
			IdentityId id;

			if (int.TryParse(ident, out var idInt)) id = (IdentityId)idInt;
			else if (ZoneServer.Instance.Data.Monsters.TryFind(ident, out var data)) id = data.Id;
			else { sender.ServerMessage(Localization.Get("Monster '{0}' not found."), ident); return CommandResult.Okay; }

			if (!ZoneServer.Instance.Data.Monsters.Contains(id))
			{
				sender.ServerMessage(Localization.Get("Monster id {0} unknown."), (int)id);
				return CommandResult.Okay;
			}

			var monster = new Monster(id);
			monster.Warp(target.MapId, target.Position.GetRandomInRange(3));
			sender.ServerMessage(Localization.Get("Summoned {0}."), id);
			return CommandResult.Okay;
		}

		private CommandResult KillMonster(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			using var monsters = PooledList<Monster>.Rent();
			target.Map.GetMonsters(monsters, 0, static (_, _) => true);
			foreach (var m in monsters)
			{
				try { m.Kill(sender); } catch { }
			}
			sender.ServerMessage(Localization.Get("Killed {0} monster(s)."), monsters.Count);
			return CommandResult.Okay;
		}

		private CommandResult MobSearch(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			var ident = args.Get(0);
			using var monsters = PooledList<Monster>.Rent();

			if (int.TryParse(ident, out var idInt))
				target.Map.GetMonsters(monsters, idInt, static (s, m) => (int)m.IdentityId == s);
			else
				target.Map.GetMonsters(monsters, ident, static (s, m) => string.Equals(m.Name, s, StringComparison.OrdinalIgnoreCase));

			foreach (var m in monsters.Take(20))
				sender.ServerMessage("{0} at ({1},{2})", m.Name, m.Position.X, m.Position.Y);
			sender.ServerMessage(Localization.Get("Found {0}."), monsters.Count);
			return CommandResult.Okay;
		}

		private CommandResult Clone(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Player cloning is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult Spawn(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count == 0)
				return CommandResult.InvalidArgument;

			Sabine.Shared.Data.Databases.MonsterData monsterData = null;
			var identityId = IdentityId.JT_PORING;

			if (!int.TryParse(args.Get(0), out var monsterId))
			{
				var monsterName = args.Get(0);
				if (!ZoneServer.Instance.Data.Monsters.TryFind(monsterName, out monsterData))
				{
					sender.ServerMessage(Localization.Get("Monster '{0}' not found."), monsterName);
					return CommandResult.Okay;
				}
				identityId = monsterData.Id;
			}
			else
			{
				identityId = (IdentityId)monsterId;
			}

			if (monsterData == null)
			{
				if (!ZoneServer.Instance.Data.Monsters.TryFind(identityId, out monsterData))
				{
					sender.ServerMessage(Localization.Get("Monster '{0}' not found."), monsterId);
					return CommandResult.Okay;
				}
			}

			var amount = 1;
			if (args.Contains(1))
			{
				if (!int.TryParse(args.Get(1), out amount))
					return CommandResult.InvalidArgument;
				amount = Math2.Clamp(1, 1000, amount);
			}

			var hpMax = -1;
			if (args.Contains("hp"))
			{
				if (!int.TryParse(args.Get("hp"), out hpMax))
					return CommandResult.InvalidArgument;
				hpMax = Math2.Clamp(1, 1_000_000, hpMax);
			}

			var aiName = args.Get("ai", monsterData.AiName ?? "none");
			var useAi = aiName != "none";

			if (!ZoneServer.Instance.Data.Monsters.Contains(identityId))
			{
				sender.ServerMessage(Localization.Get("Monster with id '{0}' not found."), identityId);
				return CommandResult.Okay;
			}

			for (var i = 0; i < amount; ++i)
			{
				var monster = new Monster(identityId);

				if (useAi)
				{
					if (aiName != monsterData.AiName && !ZoneServer.Instance.AiManager.Exists(aiName))
					{
						sender.ServerMessage(Localization.Get("AI '{0}' not found."), aiName);
						return CommandResult.Okay;
					}
					monster.AttachAi(aiName);
				}

				if (hpMax > 0)
					monster.Parameters.Hp = monster.Parameters.HpMax = hpMax;

				var pos = target.Position.GetRandomInSquareRange(4);
				monster.Warp(sender.MapId, pos);
			}

			sender.ServerMessage(Localization.Get("Monsters spawned."));
			return CommandResult.Okay;
		}
	}
}
