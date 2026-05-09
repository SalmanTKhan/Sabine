using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Cards.Handlers
{
	// Classic +20% race-damage weapon cards. Item ids and effects match
	// eAthena db/item_db.txt (look for `bonus2 bAddRace,RC_<race>,20`).
	// Each card is a one-liner: equip adds 20 to the AddRace bucket on
	// the wearer; unequip subtracts the same amount. The damage flows
	// through BattleCalculator.ApplyBonusModifiers without further
	// plumbing.

	/// <summary>Pecopeco Egg Card (4007): +20% damage vs Formless race.</summary>
	[CardHandler(4007)]
	public class PecopecoEggCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Formless] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Formless] -= 20;
	}

	/// <summary>Goblin Card (4060): +20% damage vs Brute race.</summary>
	[CardHandler(4060)]
	public class GoblinCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Brute] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Brute] -= 20;
	}

	/// <summary>Caramel Card (4063): +20% damage vs Insect race.</summary>
	[CardHandler(4063)]
	public class CaramelCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Insect] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Insect] -= 20;
	}

	/// <summary>Scorpion Card (4068): +20% damage vs Plant race.</summary>
	[CardHandler(4068)]
	public class ScorpionCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Plant] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Plant] -= 20;
	}

	/// <summary>Flora Card (4080): +20% damage vs Fish race.</summary>
	[CardHandler(4080)]
	public class FloraCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Fish] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Fish] -= 20;
	}

	/// <summary>Strouf Card (4111): +20% damage vs Demon race.</summary>
	[CardHandler(4111)]
	public class StroufCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Demon] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Demon] -= 20;
	}

	/// <summary>Petite Card (4118): +20% damage vs Dragon race.</summary>
	[CardHandler(4118)]
	public class PetiteCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Dragon] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Dragon] -= 20;
	}

	/// <summary>Hydra Card (4054): +20% damage vs DemiHuman race.</summary>
	[CardHandler(4054)]
	public class HydraCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.DemiHuman] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.DemiHuman] -= 20;
	}

	/// <summary>Skeleton Worker Card variant — Andre Card (4042): +20% damage vs Insect race.</summary>
	[CardHandler(4042)]
	public class AndreCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Insect] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Insect] -= 20;
	}

	/// <summary>Santa Poring Card (4150): +20% damage vs Undead race.</summary>
	[CardHandler(4150)]
	public class SantaPoringCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Undead] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Undead] -= 20;
	}

	/// <summary>Hode Worm Card (4034): +20% damage vs Angel race.</summary>
	[CardHandler(4034)]
	public class AngelRaceCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Angel] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddRace[(int)RaceType.Angel] -= 20;
	}

	/// <summary>
	/// Deviace Card (4125): +7% damage vs DemiHuman / Brute / Plant / Insect.
	/// The classic four-race universal hunting card — eAthena bonus2
	/// bAddRace,RC_DemiHuman/Brute/Plant/Insect,7 stacked.
	/// </summary>
	[CardHandler(4125)]
	public class DeviaceCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.AddRace[(int)RaceType.DemiHuman] += 7;
			character.Modifiers.AddRace[(int)RaceType.Brute] += 7;
			character.Modifiers.AddRace[(int)RaceType.Plant] += 7;
			character.Modifiers.AddRace[(int)RaceType.Insect] += 7;
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.AddRace[(int)RaceType.DemiHuman] -= 7;
			character.Modifiers.AddRace[(int)RaceType.Brute] -= 7;
			character.Modifiers.AddRace[(int)RaceType.Plant] -= 7;
			character.Modifiers.AddRace[(int)RaceType.Insect] -= 7;
		}
	}

	/// <summary>Thara Frog Card (4058): -30% damage taken from DemiHuman.</summary>
	[CardHandler(4058)]
	public class TharaFrogCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.SubRace[(int)RaceType.DemiHuman] += 30;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.SubRace[(int)RaceType.DemiHuman] -= 30;
	}

	/// <summary>Orc Warrior Card (4066): -30% damage taken from Brute.</summary>
	[CardHandler(4066)]
	public class OrcWarriorCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.SubRace[(int)RaceType.Brute] += 30;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.SubRace[(int)RaceType.Brute] -= 30;
	}

	/// <summary>Bigfoot Card (4074): -30% damage taken from Insect.</summary>
	[CardHandler(4074)]
	public class BigfootCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.SubRace[(int)RaceType.Insect] += 30;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.SubRace[(int)RaceType.Insect] -= 30;
	}
}
