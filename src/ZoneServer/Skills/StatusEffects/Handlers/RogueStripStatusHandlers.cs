using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// Shared unequip-on-apply helper for Rogue's RG_STRIP_*. eAthena
	/// skill.c skill_strip_equip clears the matching equip slot when
	/// the SC_STRIP* lands; the status duration then blocks re-equip,
	/// but Sabine doesn't enforce that gate yet (deferred until the
	/// equip pipeline grows a check). The slot is unequipped here so
	/// the gameplay impact at least matches the eAthena outcome for the
	/// duration's first frame.
	/// </summary>
	internal static class StripUnequip
	{
		public static void UnequipSlot(Character target, EquipSlots slot)
		{
			if (target is not PlayerCharacter pc) return;
			var item = pc.Inventory.FindEquipped(slot);
			if (item == null) return;
			pc.Inventory.UnequipItem(item);
		}
	}

	[StatusEffectHandler(StatusId.StripWeapon)]
	public class StripWeaponStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> StripUnequip.UnequipSlot(target, EquipSlots.RightHand);

		public void OnEnd(Character target, StatusEffect effect) { }
	}

	[StatusEffectHandler(StatusId.StripShield)]
	public class StripShieldStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> StripUnequip.UnequipSlot(target, EquipSlots.LeftHand);

		public void OnEnd(Character target, StatusEffect effect) { }
	}

	[StatusEffectHandler(StatusId.StripArmor)]
	public class StripArmorStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> StripUnequip.UnequipSlot(target, EquipSlots.Body);

		public void OnEnd(Character target, StatusEffect effect) { }
	}

	[StatusEffectHandler(StatusId.StripHelm)]
	public class StripHelmStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> StripUnequip.UnequipSlot(target, EquipSlots.HeadTop);

		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
