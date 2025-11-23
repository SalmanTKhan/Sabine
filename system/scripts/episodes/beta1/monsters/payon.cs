//--- Sabine Script Conversion ------------------------------------------
// Automatically converted by convert_spawns.py utility.
//---------------------------------------------------------------------------

using System;
using Sabine.Zone.Scripting;
using static Sabine.Zone.Scripting.Shortcuts;

public class PayonMonstersScript : GeneralScript
{
	public override void Load()
	{
		if (!MapsExist("pay_fild01", "pay_fild02", "pay_fild03"))
			return;

		// Spawns for pay_fild01
		AddSpawner("pay_fild01", "Willow", 1010, 10);
		AddSpawner("pay_fild01", "Poporing", 1031, 10);
		AddSpawner("pay_fild01", "Poring", 1002, 10);
		AddSpawner("pay_fild01", "Spore", 1014, 100);
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 340, 89, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 336, 116, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 231, 258, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 215, 323, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 340, 89, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 225, 310, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 129, 288, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 75, 269, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 80, 226, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 89, 177, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 95, 85, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 57, 85, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 64, 113, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 64, 190, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Black Mushroom", 1084, 1, 70, 246, 0, 0, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));
		AddSpawner("pay_fild01", "Green Plant", 1080, 5, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));

		// Spawns for pay_fild02
		AddSpawner("pay_fild02", "Boa", 1025, 30);
		AddSpawner("pay_fild02", "Wolf", 1013, 70);
		AddSpawner("pay_fild02", "Spore", 1014, 10);
		AddSpawner("pay_fild02", "Poporing", 1031, 20);
		AddSpawner("pay_fild02", "Green Plant", 1080, 4, 105, 256, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));
		AddSpawner("pay_fild02", "Red Mushroom", 1085, 4, 105, 256, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(6));

		// Spawns for pay_fild03
		AddSpawner("pay_fild03", "Poring", 1002, 30);
		AddSpawner("pay_fild03", "Willow", 1010, 30);
		AddSpawner("pay_fild03", "Pupa", 1008, 50);
		AddSpawner("pay_fild03", "Lunatic", 1063, 50);
		AddSpawner("pay_fild03", "Red Mushroom", 1085, 6, 153, 216, 10, 10, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(15));
		AddSpawner("pay_fild03", "Green Plant", 1080, 4, 372, 64, 15, 15, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(3));

	}
}