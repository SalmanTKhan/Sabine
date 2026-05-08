using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Cards.Handlers
{
	// Classic +15% size-damage weapon cards. eAthena db/item_db.txt
	// pairs each with `bonus bBaseAtk,5;`, but Sabine's BonusModifier
	// path keeps the size delta separate from raw ATK so the +5 ATK
	// piece is added through Parameters.Modify(Attack, ...).

	/// <summary>Desert Wolf Card (4082): +15% damage to Small, +5 ATK.</summary>
	[CardHandler(4082)]
	public class DesertWolfCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.AddSize[(int)SizeType.Small] += 15;
			character.Modifiers.AddEquipBonus(ParameterType.Attack, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.AddSize[(int)SizeType.Small] -= 15;
			character.Modifiers.AddEquipBonus(ParameterType.Attack, -5);
		}
	}

	/// <summary>Skel Worker Card (4092): +15% damage to Medium, +5 ATK.</summary>
	[CardHandler(4092)]
	public class SkelWorkerCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.AddSize[(int)SizeType.Medium] += 15;
			character.Modifiers.AddEquipBonus(ParameterType.Attack, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.AddSize[(int)SizeType.Medium] -= 15;
			character.Modifiers.AddEquipBonus(ParameterType.Attack, -5);
		}
	}

	/// <summary>Minorous Card (4126): +15% damage to Large, +5 ATK.</summary>
	[CardHandler(4126)]
	public class MinorousCardHandler : ICardHandler
	{
		public void OnEquip(Character character, World.Actors.Item item)
		{
			character.Modifiers.AddSize[(int)SizeType.Large] += 15;
			character.Modifiers.AddEquipBonus(ParameterType.Attack, 5);
		}

		public void OnUnequip(Character character, World.Actors.Item item)
		{
			character.Modifiers.AddSize[(int)SizeType.Large] -= 15;
			character.Modifiers.AddEquipBonus(ParameterType.Attack, -5);
		}
	}
}
