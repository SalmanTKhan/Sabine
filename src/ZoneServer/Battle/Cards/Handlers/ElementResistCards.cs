using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Cards.Handlers
{
	// Classic element-resist armor cards. eAthena db/item_db.txt:
	// `bonus2 bSubEle,Ele_<element>,30; bonus bFlee,5;` for the common
	// 30% Garment series; Raydric is the Neutral-resist standout at
	// 20% with no flee bonus. The resist flows through
	// BattleCalculator.ApplyBonusModifiers' SubElement bucket.

	/// <summary>Orc Zombie Card (4071): +30% Undead resist, +5 Flee.</summary>
	[CardHandler(4071)]
	public class OrcZombieCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Undead] += 30;
			character.Parameters.Modify(ParameterType.Flee, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Undead] -= 30;
			character.Parameters.Modify(ParameterType.Flee, -5);
		}
	}

	/// <summary>Hode Card (4081): +30% Earth resist, +5 Flee.</summary>
	[CardHandler(4081)]
	public class HodeCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Earth] += 30;
			character.Parameters.Modify(ParameterType.Flee, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Earth] -= 30;
			character.Parameters.Modify(ParameterType.Flee, -5);
		}
	}

	/// <summary>Marse Card (4095): +30% Water resist, +5 Flee.</summary>
	[CardHandler(4095)]
	public class MarseCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Water] += 30;
			character.Parameters.Modify(ParameterType.Flee, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Water] -= 30;
			character.Parameters.Modify(ParameterType.Flee, -5);
		}
	}

	/// <summary>Myst Card (4108): +30% Poison resist, +5 Flee.</summary>
	[CardHandler(4108)]
	public class MystCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Poison] += 30;
			character.Parameters.Modify(ParameterType.Flee, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Poison] -= 30;
			character.Parameters.Modify(ParameterType.Flee, -5);
		}
	}

	/// <summary>Jakk Card (4109): +30% Fire resist, +5 Flee.</summary>
	[CardHandler(4109)]
	public class JakkCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Fire] += 30;
			character.Parameters.Modify(ParameterType.Flee, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Fire] -= 30;
			character.Parameters.Modify(ParameterType.Flee, -5);
		}
	}

	/// <summary>Marionette Card (4113): +30% Ghost resist, +5 Flee.</summary>
	[CardHandler(4113)]
	public class MarionetteCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Ghost] += 30;
			character.Parameters.Modify(ParameterType.Flee, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Ghost] -= 30;
			character.Parameters.Modify(ParameterType.Flee, -5);
		}
	}

	/// <summary>Isis Card (4116): +30% Dark resist, +5 Flee.</summary>
	[CardHandler(4116)]
	public class IsisCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Dark] += 30;
			character.Parameters.Modify(ParameterType.Flee, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Dark] -= 30;
			character.Parameters.Modify(ParameterType.Flee, -5);
		}
	}

	/// <summary>Dustiness Card (4056): +30% Wind resist, +5 Flee.</summary>
	[CardHandler(4056)]
	public class DustinessCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Wind] += 30;
			character.Parameters.Modify(ParameterType.Flee, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.SubElement[(int)ElementType.Wind] -= 30;
			character.Parameters.Modify(ParameterType.Flee, -5);
		}
	}

	/// <summary>Raydric Card (4133): +20% Neutral resist (Garment).</summary>
	[CardHandler(4133)]
	public class RaydricCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.SubElement[(int)ElementType.Neutral] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.SubElement[(int)ElementType.Neutral] -= 20;
	}
}
