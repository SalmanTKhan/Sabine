//--- Sabine Script Conversion ------------------------------------------
// Automatically converted by convert_spawns.py utility.
//---------------------------------------------------------------------------

using System;
using Sabine.Zone.Scripting;
using static Sabine.Zone.Scripting.Shortcuts;

public class MoroccMonstersScriptBeta1 : GeneralScript
{
	public override void Load()
	{
		if (!MapsExist("moc_fild01", "moc_fild02", "moc_fild03", "moc_fild07"))
			return;

		// Spawns for moc_fild01
		AddSpawner("moc_fild01", "Baby Desert Wolf", 1107, 70);
		AddSpawner("moc_fild01", "Ant Egg", 1097, 20);
		AddSpawner("moc_fild01", "Peco Peco Egg", 1047, 20);
		AddSpawner("moc_fild01", "Picky", 1049, 10);
		AddSpawner("moc_fild01", "Drops", 1113, 30);
		AddSpawner("moc_fild01", "Poring", 1002, 10);
		AddSpawner("moc_fild01", "Yellow Plant", 1081, 10, 194, 51, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));

		// Spawns for moc_fild02
		AddSpawner("moc_fild02", "Peco Peco", 1019, 70);
		AddSpawner("moc_fild02", "Ant Egg", 1097, 10);
		AddSpawner("moc_fild02", "Peco Peco Egg", 1047, 40);
		AddSpawner("moc_fild02", "Picky", 1049, 10);
		AddSpawner("moc_fild02", "Drops", 1113, 30);
		AddSpawner("moc_fild02", "Yellow Plant", 1081, 1, 89, 315, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Yellow Plant", 1081, 1, 99, 261, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Yellow Plant", 1081, 1, 94, 195, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Yellow Plant", 1081, 1, 139, 222, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Yellow Plant", 1081, 1, 132, 307, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Yellow Plant", 1081, 1, 194, 294, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Yellow Plant", 1081, 1, 275, 241, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Green Plant", 1080, 1, 342, 267, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Green Plant", 1080, 1, 359, 215, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Green Plant", 1080, 1, 313, 149, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Green Plant", 1080, 1, 230, 62, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Green Plant", 1080, 1, 299, 61, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Green Plant", 1080, 1, 353, 103, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild02", "Green Plant", 1080, 1, 337, 35, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));

		// Spawns for moc_fild03
		AddSpawner("moc_fild03", "Elder Willow", 1033, 70);
		AddSpawner("moc_fild03", "Greatest General", 1277, 30);
		AddSpawner("moc_fild03", "Poporing", 1031, 20);
		AddSpawner("moc_fild03", "Eggyra", 1116, 10);
		AddSpawner("moc_fild03", "Willow", 1010, 20);
		AddSpawner("moc_fild03", "Vagabond Wolf", 1092, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("moc_fild03", "Green Plant", 1080, 2, 77, 311, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild03", "Green Plant", 1080, 2, 108, 199, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild03", "Green Plant", 1080, 2, 96, 65, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild03", "Green Plant", 1080, 2, 216, 69, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild03", "Green Plant", 1080, 2, 261, 161, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild03", "Green Plant", 1080, 2, 213, 201, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("moc_fild03", "Green Plant", 1080, 2, 200, 263, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));

		// Spawns for moc_fild07
		AddSpawner("moc_fild07", "Drops", 1113, 70);
		AddSpawner("moc_fild07", "Peco Peco Egg", 1047, 50);
		AddSpawner("moc_fild07", "Picky", 1050, 30);
		AddSpawner("moc_fild07", "Picky", 1049, 30);
		AddSpawner("moc_fild07", "Yellow Plant", 1081, 5, 162, 333, 12, 12, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));

	}
}
