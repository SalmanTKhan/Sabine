//--- Sabine Script Conversion ------------------------------------------
// Automatically converted by convert_spawns.py utility.
//---------------------------------------------------------------------------

using System;
using Sabine.Zone.Scripting;
using static Sabine.Zone.Scripting.Shortcuts;

public class TreasureMonstersScript : GeneralScript
{
	public override void Load()
	{
		if (!MapsExist("treasure01", "treasure02"))
			return;

		// Spawns for treasure01
		AddSpawner("treasure01", "Kukre", 1070, 20);
		AddSpawner("treasure01", "Whisper", 1179, 5);
		AddSpawner("treasure01", "Whisper", 1179, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("treasure01", "Poison Spore", 1077, 3, 107, 39, 15, 15, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("treasure01", "Hydra", 1068, 1, 107, 39, 15, 15, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("treasure01", "Poison Spore", 1077, 3, 29, 38, 15, 15, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("treasure01", "Hydra", 1068, 4, 68, 66, 13, 11, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 5, 34, 112, 17, 11, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 5, 106, 111, 9, 9, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 5, 106, 111, 9, 9, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("treasure01", "Hydra", 1068, 1, 67, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(263.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 67, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(293.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 68, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(317.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 68, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 69, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(305.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 69, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(290.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 70, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(302.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 70, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(306.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 48, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(316.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 48, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 49, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(310.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 49, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 50, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(308.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 50, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(301.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 51, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(314.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 51, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(270.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 86, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(285.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 86, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(310.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 87, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 87, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(268.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 88, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(281.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 88, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(296.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 89, 161, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 89, 160, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(305.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 60, 182, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(313.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 76, 182, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(269.0));
		AddSpawner("treasure01", "Poison Spore", 1077, 3, 82, 154, 11, 9, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(100.0));
		AddSpawner("treasure01", "Poison Spore", 1077, 3, 54, 154, 11, 9, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(100.0));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 2, 58, 165, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 2, 69, 165, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 2, 79, 165, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 3, 150, 162, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 150, 162, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 3, 163, 161, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 163, 161, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 3, 163, 151, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 163, 151, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 3, 163, 141, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 163, 141, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 162, 71, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(273.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 162, 70, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(314.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 163, 71, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 163, 70, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 164, 71, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(305.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 164, 70, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 165, 71, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(307.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 165, 70, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(293.0));
		AddSpawner("treasure01", "Whisper", 1179, 5, 164, 59, 12, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 158, 63, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 159, 63, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 158, 62, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 159, 62, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Pirate Skeleton", 1071, 10, 160, 60, 27, 19, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 158, 45, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(302.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 158, 44, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 158, 43, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(305.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 158, 42, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 158, 41, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 158, 40, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(307.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 159, 45, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(285.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 159, 44, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(312.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 159, 43, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(318.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 159, 42, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 159, 41, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(306.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 159, 40, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(290.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 168, 45, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 168, 44, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(314.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 168, 43, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 168, 42, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(306.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 168, 41, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(290.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 168, 40, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(322.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 169, 45, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 169, 44, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(302.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 169, 43, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(305.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 169, 42, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure01", "Hydra", 1068, 1, 169, 41, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(313.0));
		AddSpawner("treasure01", "Hydra", 1068, 1, 169, 40, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));

		// Spawns for treasure02
		AddSpawner("treasure02", "Ghostring", 1120, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(33));
		AddSpawner("treasure02", "Whisper", 1179, 5);
		AddSpawner("treasure02", "Wanderer", 1208, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(10));
		AddSpawner("treasure02", "Penomena", 1216, 22, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Kukre", 1070, 20);
		AddSpawner("treasure02", "Pirate Skeleton", 1071, 5);
		AddSpawner("treasure02", "Mimic", 1191, 3);
		AddSpawner("treasure02", "Hydra", 1068, 1, 95, 57, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 96, 57, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 101, 57, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 102, 57, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 107, 57, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 108, 57, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Penomena", 1216, 5, 101, 50, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Whisper", 1179, 1, 102, 68, 13, 13, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Whisper", 1179, 1, 102, 80, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Whisper", 1179, 1, 102, 88, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 100, 85, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 101, 85, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 102, 85, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 103, 85, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 100, 136, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 101, 136, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 102, 136, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 103, 136, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 100, 143, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 101, 143, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 102, 143, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Hydra", 1068, 1, 103, 143, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Whisper", 1179, 2, 101, 150, 15, 10, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(200.0));
		AddSpawner("treasure02", "Drake", 1112, 1, 101, 151, 8, 8, initialDelay: Hours(1), respawnDelay: Hours(2));
		AddSpawner("treasure02", "Mimic", 1191, 3, 101, 151, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(500.0));
		AddSpawner("treasure02", "Mimic", 1191, 2, 101, 151, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(100.0));
		AddSpawner("treasure02", "Penomena", 1216, 4, 101, 151, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(100.0));
		AddSpawner("treasure02", "Penomena", 1216, 1, 101, 126, 2, 2, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(20));
		AddSpawner("treasure02", "Penomena", 1216, 1, 101, 135, 2, 2, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(1));
		AddSpawner("treasure02", "Pirate Skeleton", 1071, 3, 38, 74, 9, 9, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Penomena", 1216, 5, 38, 74, 9, 9, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Pirate Skeleton", 1071, 1, 170, 71, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(100.0));
		AddSpawner("treasure02", "Pirate Skeleton", 1071, 1, 168, 77, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(100.0));
		AddSpawner("treasure02", "Pirate Skeleton", 1071, 1, 164, 67, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(100.0));
		AddSpawner("treasure02", "Pirate Skeleton", 1071, 6, 155, 43, 21, 13, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Pirate Skeleton", 1071, 1, 178, 143, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Pirate Skeleton", 1071, 1, 178, 140, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Pirate Skeleton", 1071, 1, 184, 143, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("treasure02", "Whisper", 1179, 1, 157, 143, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(100.0));
		AddSpawner("treasure02", "Whisper", 1179, 1, 151, 138, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(100.0));
		AddSpawner("treasure02", "Whisper", 1179, 1, 45, 144, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(200.0));
		AddSpawner("treasure02", "Whisper", 1179, 1, 52, 139, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(200.0));
		AddSpawner("treasure02", "Marionette", 1143, 1, 23, 142, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(200.0));

	}
}