//--- Sabine Script Conversion ------------------------------------------
// Automatically converted by convert_spawns.py utility.
//---------------------------------------------------------------------------

using System;
using Sabine.Zone.Scripting;
using static Sabine.Zone.Scripting.Shortcuts;

public class GeffenMonstersScript : GeneralScript
{
	public override void Load()
	{
		if (!MapsExist("gef_fild00", "gef_fild01", "gef_fild02"))
			return;

		// Spawns for gef_fild00
		AddSpawner("gef_fild00", "Poring", 1002, 50);
		AddSpawner("gef_fild00", "Fabre", 1007, 50);
		AddSpawner("gef_fild00", "Pupa", 1008, 20);
		AddSpawner("gef_fild00", "Poporing", 1031, 10);
		AddSpawner("gef_fild00", "Blue Plant", 1079, 1, 95, 128, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));
		AddSpawner("gef_fild00", "Blue Plant", 1079, 1, 124, 321, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));
		AddSpawner("gef_fild00", "Green Plant", 1080, 3, 54, 212, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));
		AddSpawner("gef_fild00", "Green Plant", 1080, 3, 54, 186, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));

		// Spawns for gef_fild01
		AddSpawner("gef_fild01", "Roda Frog", 1012, 50);
		AddSpawner("gef_fild01", "Poporing", 1031, 20);
		AddSpawner("gef_fild01", "Toad", 1089, 1, initialDelay: TimeSpan.Zero, respawnDelay: Hours(1));
		AddSpawner("gef_fild01", "Green Plant", 1080, 5, 215, 225, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));

		// Spawns for gef_fild02
		AddSpawner("gef_fild02", "Yoyo", 1057, 10);
		AddSpawner("gef_fild02", "Orc Warrior", 1023, 60);
		AddSpawner("gef_fild02", "Coco", 1104, 10);
		AddSpawner("gef_fild02", "Smokie", 1056, 10);
		AddSpawner("gef_fild02", "Choco", 1214, 1);
		AddSpawner("gef_fild02", "Orc Hero", 1087, 1, initialDelay: Hours(1), respawnDelay: Hours(24));
		AddSpawner("gef_fild02", "Green Plant", 1080, 8, 227, 316, 6, 6, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));
		AddSpawner("gef_fild02", "Red Mushroom", 1085, 5, 87, 48, 6, 6, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));
		AddSpawner("gef_fild02", "Blue Plant", 1079, 2, 215, 209, 2, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));
		AddSpawner("gef_fild02", "Blue Plant", 1079, 1, 207, 214, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));
		AddSpawner("gef_fild02", "Blue Plant", 1079, 1, 220, 214, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));
		AddSpawner("gef_fild02", "Shining Plant", 1083, 1, 164, 194, 1, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));

	}
}