//--- Sabine Script Conversion ------------------------------------------
// Automatically converted by convert_spawns.py utility.
//---------------------------------------------------------------------------

using System;
using Sabine.Zone.Scripting;
using static Sabine.Zone.Scripting.Shortcuts;

public class PayDunMonstersScript : GeneralScript
{
	public override void Load()
	{
		if (!MapsExist("pay_dun01", "pay_dun02"))
			return;

		// Spawns for pay_dun01
		AddSpawner("pay_dun01", "Drainliar", 1111, 5);
		AddSpawner("pay_dun01", "Eggyra", 1116, 15);
		AddSpawner("pay_dun01", "Soldier Skeleton", 1028, 50, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun01", "Archer Skeleton", 1016, 30, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun01", "Black Mushroom", 1084, 7, 235, 54, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));
		AddSpawner("pay_dun01", "Red Plant", 1078, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));

		// Spawns for pay_dun02
		AddSpawner("pay_dun02", "Soldier Skeleton", 1028, 15, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_dun02", "Munak", 1026, 40);
		AddSpawner("pay_dun02", "Bongun", 1188, 30);
		AddSpawner("pay_dun02", "Poporing", 1031, 10);
		AddSpawner("pay_dun02", "Archer Skeleton", 1016, 20, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 116, 205, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(345.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 116, 199, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(306.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 116, 196, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 116, 190, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(326.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 117, 206, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(302.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 117, 200, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(275.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 117, 197, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(314.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 117, 191, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 118, 207, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(332.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 118, 201, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 118, 198, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(312.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 118, 192, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(285.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 119, 205, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(324.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 119, 199, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 119, 196, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(315.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 120, 203, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 120, 200, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(303.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 120, 194, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 121, 200, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(333.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 122, 200, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 122, 197, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(321.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 110, 194, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 113, 194, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(319.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 109, 193, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 113, 193, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 109, 192, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(374.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 112, 192, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 108, 191, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(332.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 111, 191, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 107, 190, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(320.0));
		AddSpawner("pay_dun02", "Hydra", 1068, 1, 110, 190, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Mandragora", 1020, 1, 227, 185, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Mandragora", 1020, 1, 227, 184, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Mandragora", 1020, 1, 229, 183, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("pay_dun02", "Red Plant", 1078, 1, 214, 266, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(10));
		AddSpawner("pay_dun02", "Black Mushroom", 1084, 1, 37, 243, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(10));
		AddSpawner("pay_dun02", "Nine Tail", 1180, 1, 37, 243, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Hours(1));
		AddSpawner("pay_dun02", "Black Mushroom", 1084, 7, 111, 199, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));
		AddSpawner("pay_dun02", "White Plant", 1082, 3, 110, 216, 20, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_dun02", "White Plant", 1082, 3, 132, 84, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_dun02", "Red Plant", 1078, 4, 197, 113, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_dun02", "Red Plant", 1078, 4, 55, 254, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));

	}
}