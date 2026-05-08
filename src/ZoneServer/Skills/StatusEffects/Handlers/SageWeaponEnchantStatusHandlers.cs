using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	// Sage elemental weapon enchants (SA_FLAMELAUNCHER, SA_FROSTWEAPON,
	// SA_SEISMICWEAPON, SA_LIGHTNINGLOADER) override the wearer's weapon
	// element to Fire / Water / Earth / Wind for the duration. eAthena
	// status.c maps these skills to SC_FIREWEAPON / WATERWEAPON /
	// EARTHWEAPON / WINDWEAPON with the SCB_ATK_ELE flag — there's no
	// stat delta, the element swap is read by the battle calculator.
	//
	// Sabine models them as flag-only handlers; the actual swap lives
	// in BattleCalculator.ResolveAttackElement next to the SC_ASPERSIO
	// branch.
	//
	// eAthena: status.c SC_FIREWEAPON / SC_WATERWEAPON / SC_EARTHWEAPON
	// / SC_WINDWEAPON; battle.c battle_get_atk_ele case branches.

	/// <summary>SC_FIREWEAPON — overrides weapon element to Fire.</summary>
	[StatusEffectHandler(StatusId.EndowFire)]
	public class EndowFireStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>SC_WATERWEAPON — overrides weapon element to Water.</summary>
	[StatusEffectHandler(StatusId.EndowWater)]
	public class EndowWaterStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>SC_EARTHWEAPON — overrides weapon element to Earth.</summary>
	[StatusEffectHandler(StatusId.EndowEarth)]
	public class EndowEarthStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>SC_WINDWEAPON — overrides weapon element to Wind.</summary>
	[StatusEffectHandler(StatusId.EndowWind)]
	public class EndowWindStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
