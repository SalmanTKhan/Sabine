//--- Sabine Script Conversion ------------------------------------------
// Automatically converted by convert_spawns.py utility.
//---------------------------------------------------------------------------

using System;
using Sabine.Zone.Scripting;
using static Sabine.Zone.Scripting.Shortcuts;

public class PrtMazeMonstersScript : GeneralScript
{
	public override void Load()
	{
		if (!MapsExist("prt_maze01", "prt_maze02", "prt_maze03"))
			return;

		// Spawns for prt_maze01
		AddSpawner("prt_maze01", "Poring", 1002, 5, 179, 20, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Lunatic", 1063, 5, 139, 20, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Fabre", 1007, 5, 99, 20, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Creamy", 1018, 1, 99, 20, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Pupa", 1008, 5, 59, 20, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Poporing", 1031, 5, 19, 20, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Rocker", 1052, 5, 179, 60, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Bigfoot", 1060, 5, 139, 60, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Smokie", 1056, 5, 99, 60, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Boa", 1025, 5, 59, 60, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Wolf", 1013, 5, 19, 60, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Argiope", 1099, 3, 179, 100, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Argos", 1100, 3, 139, 100, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Chonchon", 1011, 5, 99, 100, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Horn", 1128, 5, 59, 100, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Hunter Fly", 1035, 3, 19, 100, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Mantis", 1139, 3, 179, 140, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Stainer", 1174, 5, 139, 140, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Side Winder", 1037, 3, 99, 140, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Yoyo", 1057, 4, 59, 140, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Choco", 1214, 2, 59, 140, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Steel Chonchon", 1042, 5, 19, 140, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Coco", 1104, 5, 179, 180, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Caramel", 1103, 5, 139, 180, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Dustiness", 1114, 5, 99, 180, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Martin", 1145, 5, 59, 180, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Savage", 1166, 3, 19, 180, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Savage", 1166, 5, 19, 180, 21, 21, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(2));
		AddSpawner("prt_maze01", "Hunter Fly", 1035, 1);
		AddSpawner("prt_maze01", "Vagabond Wolf", 1092, 1, initialDelay: TimeSpan.Zero, respawnDelay: Hours(1));
		AddSpawner("prt_maze01", "Shining Plant", 1083, 1, 101, 138, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze01", "White Plant", 1082, 1, 101, 138, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze01", "White Plant", 1082, 2, 99, 181, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze01", "Blue Plant", 1079, 1, 60, 140, 10, 15, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze01", "Yellow Plant", 1081, 1, 61, 101, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze01", "Shining Plant", 1083, 1, 141, 100, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze01", "Blue Plant", 1079, 1, 141, 100, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze01", "Red Plant", 1078, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("prt_maze01", "Blue Plant", 1079, 1, 100, 100, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze01", "Yellow Plant", 1081, 1, 100, 100, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze01", "Black Mushroom", 1084, 3, 176, 132, 5, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("prt_maze01", "Black Mushroom", 1084, 2, 181, 149, 3, 3, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("prt_maze01", "Red Mushroom", 1085, 3, 178, 60, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("prt_maze01", "Red Mushroom", 1085, 2, 168, 60, 3, 3, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));

		// Spawns for prt_maze02
		AddSpawner("prt_maze02", "Poporing", 1031, 25);
		AddSpawner("prt_maze02", "Bigfoot", 1060, 5);
		AddSpawner("prt_maze02", "Sasquatch", 1243, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));
		AddSpawner("prt_maze02", "Leib Olmai", 1306, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));

		// Spawns for prt_maze03
		AddSpawner("prt_maze03", "Poporing", 1031, 45);
		AddSpawner("prt_maze03", "Side Winder", 1037, 10);
		AddSpawner("prt_maze03", "Hunter Fly", 1035, 30);
		AddSpawner("prt_maze03", "Ghostring", 1120, 1, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(6800.0));
		AddSpawner("prt_maze03", "Killer Mantis", 1294, 1);
		AddSpawner("prt_maze03", "Side Winder", 1037, 20, 150, 50, 70, 70, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("prt_maze03", "Vocal", 1088, 1, 150, 50, 70, 70, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(32));
		AddSpawner("prt_maze03", "Stem Worm", 1215, 20, 50, 150, 70, 70, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("prt_maze03", "Vagabond Wolf", 1092, 1, 50, 150, 70, 70, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(32));
		AddSpawner("prt_maze03", "Mantis", 1139, 30, 170, 170, 70, 70, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(1));
		AddSpawner("prt_maze03", "Eclipse", 1093, 1, 170, 170, 70, 70, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(32));
		AddSpawner("prt_maze03", "Mastering", 1090, 1, 23, 23, 70, 70, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(32));
		AddSpawner("prt_maze03", "Baphomet Jr.", 1101, 25, 100, 100, 80, 80);
		AddSpawner("prt_maze03", "Baphomet", 1039, 1, initialDelay: Hours(1), respawnDelay: Hours(2));
		AddSpawner("prt_maze03", "Shining Plant", 1083, 1, 61, 98, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze03", "Blue Plant", 1079, 1, 61, 98, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze03", "Yellow Plant", 1081, 1, 61, 98, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze03", "Shining Plant", 1083, 1, 57, 56, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze03", "Shining Plant", 1083, 1, 15, 15, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze03", "Shining Plant", 1083, 1, 137, 140, 3, 3, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze03", "Shining Plant", 1083, 1, 17, 58, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze03", "Blue Plant", 1079, 1, 17, 58, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze03", "Blue Plant", 1079, 2, 99, 141, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze03", "Blue Plant", 1079, 1, 99, 21, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("prt_maze03", "Red Plant", 1078, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("prt_maze03", "Black Mushroom", 1084, 3, 99, 21, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("prt_maze03", "Black Mushroom", 1084, 3, 54, 15, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("prt_maze03", "Red Mushroom", 1085, 2, 171, 180, 3, 3, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("prt_maze03", "Red Mushroom", 1085, 3, 174, 187, 3, 3, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));

	}
}