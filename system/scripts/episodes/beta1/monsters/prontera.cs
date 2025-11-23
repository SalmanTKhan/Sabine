//--- Sabine Script Conversion ------------------------------------------
// Automatically converted by convert_spawns.py utility.
//---------------------------------------------------------------------------

using System;
using Sabine.Zone.Scripting;
using static Sabine.Zone.Scripting.Shortcuts;

public class PronteraMonstersScriptBeta1 : GeneralScript
{
	public override void Load()
	{
		if (!MapsExist("prt_fild00", "prt_fild01", "prt_fild02", "prt_fild03", "prt_fild04", "prt_fild05", "prt_fild06", "prt_fild07", "prt_fild08"))
			return;

		// Spawns for prt_fild00
		AddSpawner("prt_fild00", "Creamy", 1018, 10);
		AddSpawner("prt_fild00", "Fabre", 1007, 20);
		AddSpawner("prt_fild00", "Pupa", 1008, 30);
		AddSpawner("prt_fild00", "Lunatic", 1063, 30);
		AddSpawner("prt_fild00", "Poring", 1002, 40);
		AddSpawner("prt_fild00", "Hornet", 1004, 70);
		AddSpawner("prt_fild00", "Shining Plant", 1083, 1, 227, 212, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_fild00", "Green Plant", 1080, 5, 285, 138, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));

		// Spawns for prt_fild01
		AddSpawner("prt_fild01", "Thief Bug", 1051, 30);
		AddSpawner("prt_fild01", "Fabre", 1007, 20);
		AddSpawner("prt_fild01", "Pupa", 1008, 10);
		AddSpawner("prt_fild01", "Lunatic", 1063, 80);
		AddSpawner("prt_fild01", "Poring", 1002, 30);
		AddSpawner("prt_fild01", "Green Plant", 1080, 3, 199, 266, 3, 3, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));
		AddSpawner("prt_fild01", "Blue Plant", 1079, 1, 199, 266, 3, 3, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));

		// Spawns for prt_fild02
		AddSpawner("prt_fild02", "Mandragora", 1020, 70);
		AddSpawner("prt_fild02", "Fabre", 1007, 50);
		AddSpawner("prt_fild02", "Pupa", 1008, 20);
		AddSpawner("prt_fild02", "Lunatic", 1063, 10);
		AddSpawner("prt_fild02", "Poring", 1002, 30);
		AddSpawner("prt_fild02", "Eclipse", 1093, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_fild02", "Shining Plant", 1083, 1, 339, 309, 3, 3, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_fild02", "Shining Plant", 1083, 2, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));

		// Spawns for prt_fild03
		AddSpawner("prt_fild03", "Yoyo", 1057, 80);
		AddSpawner("prt_fild03", "Smokie", 1056, 40);
		AddSpawner("prt_fild03", "Choco", 1214, 1);
		AddSpawner("prt_fild03", "Poporing", 1031, 10);
		AddSpawner("prt_fild03", "Green Plant", 1080, 5, 296, 58, 15, 15, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("prt_fild03", "Blue Plant", 1079, 2, 296, 58, 15, 15, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));
		AddSpawner("prt_fild03", "Green Plant", 1080, 5, 307, 75, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));
		AddSpawner("prt_fild03", "Green Plant", 1080, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));
		AddSpawner("prt_fild03", "Green Plant", 1080, 5, 148, 107, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));

		// Spawns for prt_fild04
		AddSpawner("prt_fild04", "Rocker", 1052, 70);
		AddSpawner("prt_fild04", "Creamy", 1018, 40);
		AddSpawner("prt_fild04", "Pupa", 1008, 10);
		AddSpawner("prt_fild04", "Poring", 1002, 30);
		AddSpawner("prt_fild04", "Vocal", 1088, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_fild04", "Green Plant", 1080, 5, 350, 114, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));

		// Spawns for prt_fild05
		AddSpawner("prt_fild05", "Poring", 1002, 70);
		AddSpawner("prt_fild05", "Thief Bug Egg", 1048, 20);
		AddSpawner("prt_fild05", "Lunatic", 1063, 30);
		AddSpawner("prt_fild05", "Pupa", 1008, 30);
		AddSpawner("prt_fild05", "Thief Bug", 1051, 10);
		AddSpawner("prt_fild05", "Green Plant", 1080, 6, 208, 37, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));
		AddSpawner("prt_fild05", "Blue Plant", 1079, 1, 208, 37, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));

		// Spawns for prt_fild06
		AddSpawner("prt_fild06", "Lunatic", 1063, 60);
		AddSpawner("prt_fild06", "Thief Bug Egg", 1048, 20);
		AddSpawner("prt_fild06", "Thief Bug", 1051, 10);
		AddSpawner("prt_fild06", "Pupa", 1008, 20);
		AddSpawner("prt_fild06", "Poring", 1002, 60);
		AddSpawner("prt_fild06", "Green Plant", 1080, 15, 222, 30, 40, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));

		// Spawns for prt_fild07
		AddSpawner("prt_fild07", "Rocker", 1052, 80);
		AddSpawner("prt_fild07", "Poporing", 1031, 30);
		AddSpawner("prt_fild07", "Vocal", 1088, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_fild07", "Black Mushroom", 1084, 3, 225, 110, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));

		// Spawns for prt_fild08
		AddSpawner("prt_fild08", "Lunatic", 1063, 40);
		AddSpawner("prt_fild08", "Pupa", 1008, 20);
		AddSpawner("prt_fild08", "Poring", 1002, 70);
		AddSpawner("prt_fild08", "Drops", 1113, 10);

	}
}
