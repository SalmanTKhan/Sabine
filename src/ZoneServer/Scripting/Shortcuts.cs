﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Sabine.Shared.Configuration.Files;
using Sabine.Shared.Data;
using Sabine.Shared.Util;
using Sabine.Shared.World;
using Sabine.Zone.Commands;
using Sabine.Zone.Scripting.Dialogues;
using Sabine.Zone.World.Entities;
using Sabine.Zone.World.Shops;
using Sabine.Zone.World.Spawning;

namespace Sabine.Zone.Scripting
{
	public static class Shortcuts
	{
		private static long AnonymousShopCounter = 1;

		/// <summary>
		/// A function that initializes a shop.
		/// </summary>
		/// <param name="shop"></param>
		public delegate void ShopCreationFunc(NpcShop shop);

		/// <summary>
		/// Returns an option element, to be used with the Menu function.
		/// </summary>
		/// <param name="text">Text to display in the menu.</param>
		/// <param name="key">Key to return if the option was selected.</param>
		/// <returns></returns>
		public static DialogOption Option(string text, string key)
			=> new(text, key);

		/// <summary>
		/// Returns a localized version of the given string.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string L(string key)
			=> Localization.Get(key);

		/// <summary>
		/// Returns a localized version of the given string, formatted
		/// with the optional arguments.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static string LF(string key, params object[] args)
			=> string.Format(Localization.Get(key), args);

		/// <summary>
		/// Returns a localized plural version of the given string.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="keyPlural"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static string LN(string key, string keyPlural, int n)
			=> Localization.GetPlural(key, keyPlural, n);

		/// <summary>
		/// Returns a localized plural version of the given string,
		/// formatted with the optional arguments.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="keyPlural"></param>
		/// <param name="n"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static string LNF(string key, string keyPlural, int n, params object[] args)
			=> string.Format(Localization.GetPlural(key, keyPlural, n), args);

		/// <summary>
		/// Spawns an NPC at the given location.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="classId"></param>
		/// <param name="mapStringId"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="dialogFunc"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Npc AddNpc(string name, int classId, string mapStringId, int x, int y, DialogFunc dialogFunc = null)
			=> AddNpc(name, classId, mapStringId, x, y, Direction.South, dialogFunc);

		/// <summary>
		/// Spawns an NPC at the given location.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="classId"></param>
		/// <param name="mapStringId"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="direction"></param>
		/// <param name="dialogFunc"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Npc AddNpc(string name, int classId, string mapStringId, int x, int y, int direction, DialogFunc dialogFunc = null)
			=> AddNpc(name, classId, mapStringId, x, y, (Direction)direction, dialogFunc);

		/// <summary>
		/// Spawns an NPC at the given location.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="classId"></param>
		/// <param name="mapStringId"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="direction"></param>
		/// <param name="dialogFunc"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Npc AddNpc(string name, int classId, string mapStringId, int x, int y, Direction direction, DialogFunc dialogFunc = null)
		{
			if (mapStringId.EndsWith(".gat"))
				mapStringId = mapStringId.Substring(0, mapStringId.Length - 4);

			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(mapStringId, out var map))
				throw new ArgumentException($"Map '{mapStringId}' not found.");

			var npc = new Npc(classId);
			npc.Name = name;
			npc.Direction = direction;
			npc.DialogFunc = dialogFunc;

			npc.Warp(map.Id, new Position(x, y));

			return npc;
		}

		/// <summary>
		/// Spawns an NPC at the given location that opens a shop.
		/// </summary>
		/// <param name="name">Name of the NPC.</param>
		/// <param name="classId">NPC sprite id.</param>
		/// <param name="mapStringId">Map to spawn the NPC on.</param>
		/// <param name="x">X-coordinate to spawn the NPC on.</param>
		/// <param name="y">Y-coordinate to spawn the NPC on.</param>
		/// <param name="creationFunc">Optional function that can be used to fill the shop.</param>
		/// <returns></returns>
		public static (Npc, NpcShop) AddShopNpc(string name, int classId, string mapStringId, int x, int y, int direction, ShopCreationFunc creationFunc = null)
		{
			var shopName = "__AnonymousShop__" + Interlocked.Increment(ref AnonymousShopCounter);
			var shop = AddShop(shopName, creationFunc);

			var npc = AddNpc(name, classId, mapStringId, x, y, direction, async dialog =>
			{
				dialog.OpenShop(shop);
				await Task.Yield();
			});

			return (npc, shop);
		}

		/// <summary>
		/// Spawns a warp at the given location.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Npc AddWarp(Location from, Location to)
		{
			if (!ZoneServer.Instance.World.Maps.TryGet(from.MapId, out var mapFrom))
				throw new ArgumentException($"Map '{from.MapId}' not found.");

			if (!ZoneServer.Instance.World.Maps.TryGet(to.MapId, out var mapTo))
				throw new ArgumentException($"Map '{from.MapId}' not found.");

			var npc = new Npc(45);
			npc.WarpDestination = to;
			//npc.Trigger = WarpOnTouch

			npc.Warp(from);

			return npc;
		}

		/// <summary>
		/// Creates shop and returns it. It can be filled via the callback
		/// function or with the returned object.
		/// </summary>
		/// <param name="name">Name of the shop, which can be used to reference it from other scripts.</param>
		/// <param name="creationFunc">Optional function that can be used to fill the shop.</param>
		/// <returns></returns>
		public static NpcShop AddShop(string name, ShopCreationFunc creationFunc = null)
		{
			var shop = new NpcShop(name);
			creationFunc?.Invoke(shop);

			if (ZoneServer.Instance.World.NpcShops.ContainsKey(name))
				throw new ArgumentException($"A shop with the name '{name}' already exists.");

			ZoneServer.Instance.World.NpcShops.Add(name, shop);

			return shop;
		}

		/// <summary>
		/// Converts parameters into a Location.
		/// </summary>
		/// <param name="mapStringId"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static Location From(string mapStringId, int x, int y)
			=> ToLocation(mapStringId, x, y);

		/// <summary>
		/// Converts parameters into a Location.
		/// </summary>
		/// <param name="mapStringId"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static Location To(string mapStringId, int x, int y)
			=> ToLocation(mapStringId, x, y);

		/// <summary>
		/// Converts parameters into a Location.
		/// </summary>
		/// <param name="mapStringId"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		private static Location ToLocation(string mapStringId, int x, int y)
		{
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(mapStringId, out var map))
				throw new ArgumentException($"Map '{mapStringId}' not found.");

			return new Location(map.Id, new Position(x, y));
		}

		/// <summary>
		/// Adds new chat command.
		/// </summary>
		/// <param name="name">Name of the command.</param>
		/// <param name="usage">Description of the command's arguments.</param>
		/// <param name="description">A description of what the command does.</param>
		/// <param name="func">The handler function to execute when the command is used.</param>
		/// <param name="selfAuthLevel">Authority level required to execute the command for yourself.</param>
		/// <param name="targetAuthLevel">Authority level required to execute the command for someone else.</param>
		public static void AddChatCommand(string name, string usage, string description, CommandFunc func, int selfAuthLevel, int targetAuthLevel)
		{
			ZoneServer.Instance.ChatCommands.Add(name, usage, description, func);
			ZoneServer.Instance.Conf.Commands.Levels[name] = new AuthLevels() { Self = selfAuthLevel, Target = targetAuthLevel };
		}

		/// <summary>
		/// Creates a permanent monster spawner.
		/// </summary>
		/// <param name="mapStringId"></param>
		/// <param name="monsterName"></param>
		/// <param name="monsterId"></param>
		/// <param name="amount"></param>
		public static void AddSpawner(string mapStringId, string monsterName, int monsterId, int amount)
			=> AddSpawner(mapStringId, monsterName, monsterId, amount, TimeSpan.Zero, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));

		/// <summary>
		/// Creates a permanent monster spawner.
		/// </summary>
		/// <param name="mapStringId"></param>
		/// <param name="monsterName"></param>
		/// <param name="monsterId"></param>
		/// <param name="amount"></param>
		/// <param name="initialDelay"></param>
		/// <param name="respawnDelay"></param>
		public static void AddSpawner(string mapStringId, string monsterName, int monsterId, int amount, TimeSpan initialDelay, TimeSpan respawnDelay)
			=> AddSpawner(mapStringId, monsterName, monsterId, amount, initialDelay, respawnDelay, respawnDelay);

		/// <summary>
		/// Creates a permanent monster spawner.
		/// </summary>
		/// <param name="mapStringId"></param>
		/// <param name="monsterName"></param>
		/// <param name="monsterId"></param>
		/// <param name="amount"></param>
		/// <param name="initialDelay"></param>
		/// <param name="respawnDelayMin"></param>
		/// <param name="respawnDelayMax"></param>
		public static void AddSpawner(string mapStringId, string monsterName, int monsterId, int amount, TimeSpan initialDelay, TimeSpan respawnDelayMin, TimeSpan respawnDelayMax)
		{
			if (mapStringId.EndsWith(".gat"))
				mapStringId = mapStringId.Substring(0, mapStringId.Length - 4);

			if (!SabineData.Maps.TryFind(mapStringId, out var map))
				throw new ArgumentException($"Map '{mapStringId}' not found.");

			var spawner = new Spawner(monsterId, amount, initialDelay, respawnDelayMin, respawnDelayMax, map.Id);
			ZoneServer.Instance.World.Spawners.Add(spawner);
		}

		/// <summary>
		/// Returns false if any of the given maps don't exist in the world.
		/// </summary>
		/// <param name="mapStringIds"></param>
		/// <returns></returns>
		public static bool MapsExist(params string[] mapStringIds)
		{
			foreach (var stringId in mapStringIds)
			{
				if (!ZoneServer.Instance.World.Maps.TryGetByStringId(stringId, out _))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Returns a time span of the given amount of hours.
		/// </summary>
		/// <param name="hours"></param>
		/// <returns></returns>
		public static TimeSpan Hours(double hours)
			=> TimeSpan.FromHours(hours);

		/// <summary>
		/// Returns a time span of the given amount of minutes.
		/// </summary>
		/// <param name="minutes"></param>
		/// <returns></returns>
		public static TimeSpan Minutes(double minutes)
			=> TimeSpan.FromMinutes(minutes);

		/// <summary>
		/// Returns a time span of the given amount of seconds.
		/// </summary>
		/// <param name="seconds"></param>
		/// <returns></returns>
		public static TimeSpan Seconds(double seconds)
			=> TimeSpan.FromSeconds(seconds);

		/// <summary>
		/// Returns a time span of the given amount of milliseconds.
		/// </summary>
		/// <param name="milliseconds"></param>
		/// <returns></returns>
		public static TimeSpan Milliseconds(double milliseconds)
			=> TimeSpan.FromMilliseconds(milliseconds);
	}
}
