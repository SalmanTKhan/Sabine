using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	// Chemical Protection (AM_CP_WEAPON / SHIELD / ARMOR / HELM) maps to
	// eAthena status.c SC_PROTECTWEAPON / SC_PROTECTSHIELD /
	// SC_PROTECTARMOR / SC_PROTECTHELM. They are flag-only — the strip
	// resistance check lives in the Rogue strip handler, mirroring
	// eAthena skill.c skill_strip_equip's early return when the matching
	// SC_PROTECT* is active.

	/// <summary>SC_PROTECTWEAPON — blocks RG_STRIPWEAPON.</summary>
	[StatusEffectHandler(StatusId.CpWeapon)]
	public class CpWeaponStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>SC_PROTECTSHIELD — blocks RG_STRIPSHIELD.</summary>
	[StatusEffectHandler(StatusId.CpShield)]
	public class CpShieldStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>SC_PROTECTARMOR — blocks RG_STRIPARMOR.</summary>
	[StatusEffectHandler(StatusId.CpArmor)]
	public class CpArmorStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>SC_PROTECTHELM — blocks RG_STRIPHELM.</summary>
	[StatusEffectHandler(StatusId.CpHelm)]
	public class CpHelmStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
