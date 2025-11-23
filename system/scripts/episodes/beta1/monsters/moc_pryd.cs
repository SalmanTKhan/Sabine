//--- Sabine Script Conversion ------------------------------------------
// Automatically converted by convert_spawns.py utility.
//---------------------------------------------------------------------------

using System;
using Sabine.Zone.Scripting;
using static Sabine.Zone.Scripting.Shortcuts;

public class MocPrydMonstersScript : GeneralScript
{
	public override void Load()
	{
		if (!MapsExist("moc_pryd01", "moc_pryd02", "moc_pryd03", "moc_pryd04"))
			return;

		// Spawns for moc_pryd01
		AddSpawner("moc_pryd01", "Familiar", 1005, 50);
		AddSpawner("moc_pryd01", "Poporing", 1031, 20);

		// Spawns for moc_pryd02
		AddSpawner("moc_pryd02", "Poporing", 1031, 20);
		AddSpawner("moc_pryd02", "Drainliar", 1111, 20);
		AddSpawner("moc_pryd02", "Mummy", 1041, 30);
		AddSpawner("moc_pryd02", "Isis", 1029, 5);
		AddSpawner("moc_pryd02", "Soldier Skeleton", 1028, 30);
		AddSpawner("moc_pryd02", "Archer Skeleton", 1016, 30);

		// Spawns for moc_pryd03
		AddSpawner("moc_pryd03", "Drainliar", 1111, 5);
		AddSpawner("moc_pryd03", "Mimic", 1191, 5);
		AddSpawner("moc_pryd03", "Matyr", 1146, 10);
		AddSpawner("moc_pryd03", "Mummy", 1041, 70);
		AddSpawner("moc_pryd03", "Verit", 1032, 20, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(10));

		// Spawns for moc_pryd04
		AddSpawner("moc_pryd04", "Mummy", 1041, 25);
		AddSpawner("moc_pryd04", "Ancient Mummy", 1297, 3);
		AddSpawner("moc_pryd04", "Isis", 1029, 50, initialDelay: TimeSpan.Zero, respawnDelay: Seconds(150.0));
		AddSpawner("moc_pryd04", "Mimic", 1191, 15, initialDelay: TimeSpan.Zero, respawnDelay: Minutes(10));
		AddSpawner("moc_pryd04", "Osiris", 1038, 1, initialDelay: Hours(1), respawnDelay: Hours(1));
		AddSpawner("moc_pryd04", "Matyr", 1146, 20);

	}
}