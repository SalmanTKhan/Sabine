//--- Sabine Script ---------------------------------------------------------
// Endless Tower (Slice F)
//--- Description -----------------------------------------------------------
// First playable memorial dungeon. Proves the Instance framework
// end-to-end with a small linear-floor loop. Renewal-era `1@tower`...
// `6@tower` maps don't exist in classic Sabine, so the template in
// `system/data/instances.txt` substitutes six existing dungeon maps
// as 1F..6F.
//
// Flow:
//   1. Tower Keeper NPC at Alberta talks the player into entering.
//   2. InstanceCreate("Endless Tower") spawns a private clone of all
//      six floors (party-scoped — partymates can re-enter the same
//      run from the same NPC).
//   3. A "Climb" portal NPC is dynamically dropped on each cloned
//      floor; it warps the party to the next floor.
//   4. On the top floor, the portal awards the reward and persists
//      the highest cleared floor onto the player's perm vars.
//---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone;
using Sabine.Zone.Scripting;
using Sabine.Zone.Scripting.Dialogues;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;
using Yggdrasil.Util;
using static Sabine.Zone.Scripting.Shortcuts;
using static Sabine.Shared.Const.IdentityId;

[RequiresMaps("alberta")]
public class Beta1EndlessTowerScript : GeneralScript
{
	private const string TemplateName = "Endless Tower";
	private const string HighestFloorVar = "Sabine.Instance.EndlessTower.HighestFloor";

	// Floor base maps (must match `system/data/instances.txt` order).
	private static readonly string[] Floors =
	{
		"gef_dun00",
		"gef_dun01",
		"moc_pryd01",
		"moc_pryd02",
		"moc_pryd03",
		"moc_pryd04",
	};

	// Per-floor portal landing position. Picked roughly central on each
	// real map; players spawn here when they advance to that floor.
	private static readonly Position[] FloorEntry =
	{
		new(60, 165),  // gef_dun00
		new(115, 235), // gef_dun01
		new(190, 9),   // moc_pryd01
		new(33, 125),  // moc_pryd02
		new(290, 167), // moc_pryd03
		new(133, 60),  // moc_pryd04
	};

	// Per-floor mob waves. Each floor spawns this many copies of the
	// listed monster id near the entry; the climb portal is gated on
	// every mob being defeated. The top floor adds Osiris as a boss.
	private const int MobsPerFloor = 5;
	private static readonly IdentityId[] FloorMobs =
	{
		IdentityId.JT_PORING,   // 1F: lvl 1
		IdentityId.JT_FABRE,    // 2F: lvl 2
		IdentityId.JT_ZOMBIE,   // 3F: lvl 15
		IdentityId.JT_VERIT,    // 4F: lvl 38
		IdentityId.JT_MUMMY,    // 5F: lvl 37
		IdentityId.JT_MUMMY,    // 6F: mummies + Osiris boss (see SpawnFloorMobs)
	};

	// Reward inventory drop on top-floor clear. Zeny is awarded
	// separately in OnTowerCleared.
	private static readonly (int classId, int amount)[] ClearRewards =
	{
		(603, 1),  // Old_Blue_Box
		(607, 3),  // Yggdrasilberry
	};

	public override void Load()
	{
		// Tower Keeper at Alberta dock. Position picked near the
		// vendor row so it's findable on any client.
		AddNpc("Tower Keeper", JT_PRIEST, "alberta", 30, 240,
			Direction.South, RunKeeperDialog);
	}

	private static async Task RunKeeperDialog(Dialog dialog)
	{
		var player = dialog.Player;

		await dialog.Talk("[Tower Keeper]");
		await dialog.Talk("Beyond this gate lies the Endless Tower. Six floors of progressively stronger foes — and a reward at the top.");

		var highest = player.Vars.Perm.GetInt(HighestFloorVar, 0);
		if (highest > 0)
			await dialog.Talk($"Your best run reached floor {highest}.");

		var choice = await dialog.Select(
			Option("Enter the tower", "enter"),
			Option("Maybe later", "leave"));

		if (choice == "leave")
		{
			await dialog.Talk("Come back when you're ready to climb.");
			return;
		}

		// Reuse an active run (TTL 4h) so partymates can re-enter
		// the same instance the leader started.
		var instId = GetInstanceId(player);
		if (instId <= 0)
		{
			instId = InstanceCreate(player, TemplateName);
			if (instId <= 0)
			{
				await dialog.Talk("The tower's gate refuses to open. Try again later.");
				return;
			}

			// Drop the inter-floor portals + mob waves onto each cloned map.
			SpawnFloorPortals(instId);
			SpawnFloorMobs(instId);
		}

		WarpToFloor(player, instId, 1);
	}

	/// <summary>
	/// Drops a "Climb" warp-trigger NPC on each cloned floor. The
	/// trigger captures the floor number + instance id by closure;
	/// the top floor's portal grants the reward and warps back to
	/// Alberta.
	/// </summary>
	private static void SpawnFloorPortals(int instId)
	{
		for (var floorIdx = 0; floorIdx < Floors.Length; floorIdx++)
		{
			var floorNumber = floorIdx + 1; // 1-indexed for player-facing
			var instanceMapName = GetInstanceMapName(Floors[floorIdx], instId);

			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(instanceMapName, out var clone))
				continue;

			var entry = FloorEntry[floorIdx];

			// Position the portal a few tiles off the entry point so
			// the player doesn't immediately re-trigger it. +5 on x.
			var portalPos = new Position(entry.X + 5, entry.Y);
			var capturedInst = instId;
			var capturedFloor = floorNumber;

			AddWarpTrigger(instanceMapName, portalPos.X, portalPos.Y, 1, 1,
				(entered, _) =>
				{
					if (entered is PlayerCharacter pc)
						OnPortalTriggered(pc, capturedInst, capturedFloor);
				});
		}
	}

	/// <summary>
	/// Spawns <see cref="MobsPerFloor"/> monsters per floor at random
	/// offsets around the floor entry. The top floor also gets one
	/// Osiris as a boss capper.
	/// </summary>
	private static void SpawnFloorMobs(int instId)
	{
		for (var floorIdx = 0; floorIdx < Floors.Length; floorIdx++)
		{
			var instanceMapName = GetInstanceMapName(Floors[floorIdx], instId);
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(instanceMapName, out var clone))
				continue;

			var mobId = FloorMobs[floorIdx];
			var entry = FloorEntry[floorIdx];

			for (var i = 0; i < MobsPerFloor; i++)
			{
				var pos = entry.GetRandomInRange(4, 8);
				var monster = new Monster(mobId);
				monster.Warp(clone.Id, pos);
			}

			// Top-floor boss.
			if (floorIdx == Floors.Length - 1)
			{
				var boss = new Monster(IdentityId.JT_OSIRIS);
				boss.Warp(clone.Id, entry);
			}
		}
	}

	private static void OnPortalTriggered(PlayerCharacter player, int instId, int currentFloor)
	{
		// Gate: defeat all live mobs on this floor before advancing.
		if (!IsFloorClear(player, instId, currentFloor))
		{
			player.ServerMessage("Defeat all monsters on this floor before climbing.");
			return;
		}

		// Top floor: complete the run.
		if (currentFloor >= Floors.Length)
		{
			OnTowerCleared(player, instId);
			return;
		}

		WarpToFloor(player, instId, currentFloor + 1);
	}

	private static bool IsFloorClear(PlayerCharacter player, int instId, int currentFloor)
	{
		var floorIdx = currentFloor - 1;
		if (floorIdx < 0 || floorIdx >= Floors.Length)
			return true;

		var instMap = GetInstanceMapName(Floors[floorIdx], instId);
		if (!ZoneServer.Instance.World.Maps.TryGetByStringId(instMap, out var map))
			return true;

		using var monsters = PooledList<Monster>.Rent();
		map.GetMonsters(monsters, 0, static (_, m) => !m.IsDead);
		return monsters.Count == 0;
	}

	private static void WarpToFloor(PlayerCharacter player, int instId, int floorNumber)
	{
		var floorIdx = floorNumber - 1;
		if (floorIdx < 0 || floorIdx >= Floors.Length)
			return;

		var instMap = GetInstanceMapName(Floors[floorIdx], instId);
		if (!ZoneServer.Instance.World.Maps.TryGetByStringId(instMap, out var map))
		{
			player.ServerMessage($"Tower floor {floorNumber} is unavailable.");
			return;
		}

		var entry = FloorEntry[floorIdx];
		player.Warp(new Location(map.Id, entry));
		player.ServerMessage($"Endless Tower — Floor {floorNumber}");
	}

	private static void OnTowerCleared(PlayerCharacter player, int instId)
	{
		var prevHighest = player.Vars.Perm.GetInt(HighestFloorVar, 0);
		var cleared = Floors.Length;

		if (cleared > prevHighest)
		{
			player.Vars.Perm.SetInt(HighestFloorVar, cleared);
			player.ServerMessage($"New record! You cleared floor {cleared}.");
		}
		else
		{
			player.ServerMessage($"Tower cleared (floor {cleared}). Best: {prevHighest}.");
		}

		// Reward: zeny + a small drop bundle.
		player.Parameters.Zeny += 100000;
		foreach (var (classId, amount) in ClearRewards)
			player.Inventory.AddItem(new Item(classId, amount));
		player.ServerMessage("Reward: 100,000 zeny + Old Blue Box + 3 Yggdrasil Berries.");

		// Warp back to Alberta. The instance stays alive on its TTL
		// so partymates still inside aren't kicked.
		if (ZoneServer.Instance.World.Maps.TryGetByStringId("alberta", out var alberta))
			player.Warp(new Location(alberta.Id, new Position(30, 235)));
	}
}
