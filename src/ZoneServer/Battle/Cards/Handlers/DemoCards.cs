using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Cards.Handlers
{
	// Card item ids match eAthena db/item_db.txt for the classic cards.
	// These five are shipped as a demonstration that the card-handler
	// pipeline (registry → equip hook → BonusModifiers → damage calc)
	// works end-to-end. Bulk card population comes in a follow-up.

	/// <summary>Andre Card (4055): +20% damage vs Insect race.</summary>
	[CardHandler(4055)]
	public class AndreCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Insect] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Insect] -= 20;
	}

	/// <summary>Skel Worker Card (4054): +15% damage vs Brute race.</summary>
	[CardHandler(4054)]
	public class SkelWorkerCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Brute] += 15;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Brute] -= 15;
	}

	/// <summary>Drainliar Card (4034): +20% damage vs Water-element targets.</summary>
	[CardHandler(4034)]
	public class DrainliarCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Water] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Water] -= 20;
	}

	/// <summary>Marina Card (4035): -15% damage taken from Water-element attacks.</summary>
	[CardHandler(4035)]
	public class MarinaCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.SubElement[(int)ElementType.Water] += 15;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.SubElement[(int)ElementType.Water] -= 15;
	}

	/// <summary>Vadon Card (4174): +20% damage vs Fire-element targets.</summary>
	[CardHandler(4174)]
	public class VadonCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Fire] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Fire] -= 20;
	}
}
