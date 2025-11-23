//--- Sabine Script Conversion ------------------------------------------
// Automatically converted by convert_spawns.py utility.
//---------------------------------------------------------------------------

using System;
using Sabine.Zone.Scripting;
using static Sabine.Zone.Scripting.Shortcuts;

public class GefDunMonstersScript : GeneralScript
{
	public override void Load()
	{
		if (!MapsExist("gef_dun00", "gef_dun01"))
			return;

		// Spawns for gef_dun00
		AddSpawner("gef_dun00", "Hunter Fly", 1035, 30, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(1));
		AddSpawner("gef_dun00", "Poporing", 1031, 15);
		AddSpawner("gef_dun00", "Poison Spore", 1077, 25);
		AddSpawner("gef_dun00", "Red Plant", 1078, 1, 91, 106, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("gef_dun00", "Yellow Plant", 1081, 1, 92, 108, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("gef_dun00", "Green Plant", 1080, 1, 114, 106, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("gef_dun00", "Red Plant", 1078, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("gef_dun00", "Shining Plant", 1083, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(50));
		AddSpawner("gef_dun00", "Red Plant", 1078, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("gef_dun00", "Black Mushroom", 1084, 3, 89, 111, 3, 3, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("gef_dun00", "Black Mushroom", 1084, 3, 121, 109, 3, 3, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));

		// Spawns for gef_dun01
		AddSpawner("gef_dun01", "Drainliar", 1111, 20);
		AddSpawner("gef_dun01", "Nightmare", 1061, 30);
		AddSpawner("gef_dun01", "Ogretooth", 1204, 1, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(30));
		AddSpawner("gef_dun01", "Zombie", 1015, 25);
		AddSpawner("gef_dun01", "Ghoul", 1036, 40);
		AddSpawner("gef_dun01", "Jakk", 1130, 40, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("gef_dun01", "Blue Plant", 1079, 1, 234, 121, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(5));
		AddSpawner("gef_dun01", "Dracula", 1389, 1, initialDelay: Hours(1), respawnDelay: Hours(1));
		AddSpawner("gef_dun01", "Black Mushroom", 1084, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("gef_dun01", "White Plant", 1082, 3, 188, 104, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("gef_dun01", "White Plant", 1082, 3, 263, 115, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("gef_dun01", "White Plant", 1082, 2, 48, 67, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("gef_dun01", "White Plant", 1082, 2, 150, 237, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));

	}
}