using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	// Sage elemental fields: SA_VOLCANO / SA_DELUGE / SA_VIOLENTGALE /
	// SA_LANDPROTECTOR. The skill handlers (Sage/SageActives.cs) spawn
	// ElementalFieldUnit ground cells; the cells' OnTouch starts the
	// status on a friendly entrant. The +damage% bonus
	// (BattleCalculator.GetElementalFieldBonus) reads the status
	// directly via Has(...), so the handlers below are markers that
	// make the dispatch table explicit and give future per-tick / stat
	// tweaks a clear hook (e.g., MagneticEarth's eAthena no-magic
	// guard, or a Deluge MaxHP+ aura).

	/// <summary>SC_VOLCANO — fire field aura on the caster while standing in the cells.</summary>
	[StatusEffectHandler(StatusId.Volcano)]
	public class VolcanoStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>SC_DELUGE — water field aura.</summary>
	[StatusEffectHandler(StatusId.Deluge)]
	public class DelugeStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>SC_WHIRLWIND — wind field aura (SA_VIOLENTGALE).</summary>
	[StatusEffectHandler(StatusId.Whirlwind)]
	public class WhirlwindStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>SC_MAGNETICEARTH — earth field aura (SA_LANDPROTECTOR).</summary>
	[StatusEffectHandler(StatusId.MagneticEarth)]
	public class MagneticEarthStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_MAGICROD — Sage magic-absorb buff. The combat-calc
	/// TryAbsorbWithMagicRod hook reads the status directly via
	/// Has(...), so this handler stays a marker; OnStart/OnEnd would
	/// own any future visual aura / icon work.
	/// </summary>
	[StatusEffectHandler(StatusId.MagicRod)]
	public class MagicRodStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
