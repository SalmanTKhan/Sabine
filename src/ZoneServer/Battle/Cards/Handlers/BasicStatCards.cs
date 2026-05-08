using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Cards.Handlers
{
	// Classic-era stat-bonus cards. Item ids and stat deltas follow
	// eAthena db/pre-re/item_db.txt — see the bonus2/bonus blocks attached
	// to each card row. Only the parts that flow cleanly through the
	// existing Parameters / Modifiers pipeline are wired up here:
	//
	// - Base stats (Str / Agi / Vit / Int / Dex / Luk) are inputs to
	//   RecalculateAll, so card deltas survive an equip recalc.
	// - Critical and Flee2 (perfect dodge) aren't touched by recalc, so
	//   flat additions persist correctly.
	//
	// Bonuses that target stats *re-derived* on every equip (Attack, Hit,
	// Flee, MaxHp, MaxSp, MagicDefense, etc.) are intentionally skipped
	// here — they would be silently overwritten by RecalculateAll. Those
	// require a follow-up pass that gives the equip-bonus pipeline its own
	// add-on bucket the way Modifiers.AddRace already does.

	/// <summary>Poring Card (4001): +2 LUK, +1 Perfect Dodge, +10% Neutral resist.</summary>
	[CardHandler(4001)]
	public class PoringCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Luk, 2);
			character.Parameters.Modify(ParameterType.FleeBonus, 1);
			character.Modifiers.SubElement[(int)ElementType.Neutral] += 10;
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Luk, -2);
			character.Parameters.Modify(ParameterType.FleeBonus, -1);
			character.Modifiers.SubElement[(int)ElementType.Neutral] -= 10;
		}
	}

	/// <summary>Fabre Card (4002): +1 VIT.</summary>
	[CardHandler(4002)]
	public class FabreCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Vit, 1);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Vit, -1);
	}

	/// <summary>Lunatic Card (4006): +1 LUK, +1 Critical.</summary>
	[CardHandler(4006)]
	public class LunaticCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Luk, 1);
			character.Parameters.Modify(ParameterType.Critical, 1);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Luk, -1);
			character.Parameters.Modify(ParameterType.Critical, -1);
		}
	}

	/// <summary>Picky Card (4011): +1 STR. (Size-add bonuses skipped — see file header.)</summary>
	[CardHandler(4011)]
	public class PickyCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, 1);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, -1);
	}

	/// <summary>Andre Egg Card (4017): +1 STR.</summary>
	[CardHandler(4017)]
	public class AndreEggCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, 1);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, -1);
	}

	/// <summary>Andre Larva Card (4123): +2 INT.</summary>
	[CardHandler(4123)]
	public class AndreLarvaCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Int, 2);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Int, -2);
	}

	/// <summary>Spore Card (4023): +2 DEX.</summary>
	[CardHandler(4023)]
	public class SporeCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Dex, 2);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Dex, -2);
	}

	/// <summary>Coco Card (4036): +1 VIT. (SP recovery rate skipped.)</summary>
	[CardHandler(4036)]
	public class CocoCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Vit, 1);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Vit, -1);
	}

	/// <summary>Steel Chonchon Card (4044): +1 AGI, +10% Wind resist.</summary>
	[CardHandler(4044)]
	public class SteelChonchonCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Agi, 1);
			character.Modifiers.SubElement[(int)ElementType.Wind] += 10;
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Agi, -1);
			character.Modifiers.SubElement[(int)ElementType.Wind] -= 10;
		}
	}

	/// <summary>Hornet Card (4045): +2 STR.</summary>
	[CardHandler(4045)]
	public class HornetCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, 2);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, -2);
	}

	/// <summary>Tarou Card (4046): +1 STR.</summary>
	[CardHandler(4046)]
	public class TarouCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, 1);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, -1);
	}

	/// <summary>Rocker Card (4051): +1 DEX.</summary>
	[CardHandler(4051)]
	public class RockerCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Dex, 1);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Dex, -1);
	}

	/// <summary>Yoyo Card (4052): +1 AGI, +5 Perfect Dodge.</summary>
	[CardHandler(4052)]
	public class YoyoCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Agi, 1);
			character.Parameters.Modify(ParameterType.FleeBonus, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Agi, -1);
			character.Parameters.Modify(ParameterType.FleeBonus, -5);
		}
	}

	/// <summary>Soldier Skeleton Card (4083): +9 Critical, -1 LUK.</summary>
	[CardHandler(4083)]
	public class SoldierSkeletonCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Critical, 9);
			character.Parameters.Modify(ParameterType.Luk, -1);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Critical, -9);
			character.Parameters.Modify(ParameterType.Luk, 1);
		}
	}

	/// <summary>Drops Card (4004): +1 DEX. (Hit bonus skipped — recomputed on equip.)</summary>
	[CardHandler(4004)]
	public class DropsCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Dex, 1);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Dex, -1);
	}

	/// <summary>Zerom Card (4145): +3 DEX.</summary>
	[CardHandler(4145)]
	public class ZeromCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Dex, 3);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Dex, -3);
	}

	/// <summary>Mantis Card (4040): +3 STR.</summary>
	[CardHandler(4040)]
	public class MantisCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, 3);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, -3);
	}

	/// <summary>Sohee Card (4096): +3 INT.</summary>
	[CardHandler(4096)]
	public class SoheeCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Int, 3);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Int, -3);
	}

	/// <summary>Whisper Card (4093): +1 AGI, +20 Perfect Dodge.</summary>
	[CardHandler(4093)]
	public class WhisperCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Agi, 1);
			character.Parameters.Modify(ParameterType.FleeBonus, 20);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Parameters.Modify(ParameterType.Agi, -1);
			character.Parameters.Modify(ParameterType.FleeBonus, -20);
		}
	}

	/// <summary>Munak Card (4097): +3 STR.</summary>
	[CardHandler(4097)]
	public class MunakCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, 3);

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Parameters.Modify(ParameterType.Str, -3);
	}
}
