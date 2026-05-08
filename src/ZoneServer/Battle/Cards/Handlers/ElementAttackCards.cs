using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Cards.Handlers
{
	// Classic +20% element-damage weapon cards. Item ids and effects
	// match eAthena db/item_db.txt (look for
	// `bonus2 bAddEle,Ele_<element>,20`). Bonus flows through
	// BattleCalculator.ApplyBonusModifiers.

	/// <summary>Mandragora Card (4030): +20% damage vs Wind targets.</summary>
	[CardHandler(4030)]
	public class MandragoraCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Wind] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Wind] -= 20;
	}

	/// <summary>Vadon Card (4049): +20% damage vs Fire targets.</summary>
	[CardHandler(4049)]
	public class VadonCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Fire] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Fire] -= 20;
	}

	/// <summary>Anacondaq Card (4062): +20% damage vs Poison targets.</summary>
	[CardHandler(4062)]
	public class AnacondaqCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Poison] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Poison] -= 20;
	}

	/// <summary>Kaho Card (4065): +20% damage vs Earth targets.</summary>
	[CardHandler(4065)]
	public class KahoCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Earth] += 20;

		public void OnUnequip(Character character, World.Actors.Item item)
			=> character.Modifiers.AddElement[(int)ElementType.Earth] -= 20;
	}
}
