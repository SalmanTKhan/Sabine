using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_CARTBOOST (WS_CARTBOOST) — flat movement-speed boost. eAthena
	/// status.c:3867 awards a +20% walk-speed cap; Sabine mirrors via a
	/// negative SpeedDelta (lower = faster). Picked at 40ms to sit just
	/// below IncreaseAgi's 50 in the existing scale.
	/// </summary>
	[StatusEffectHandler(StatusId.CartBoost)]
	public class CartBoostStatusHandler : IStatusEffectHandler
	{
		private const int SpeedDelta = 40;

		public void OnStart(Character target, StatusEffect effect)
		{
			target.Parameters.Modify(ParameterType.Speed, -SpeedDelta);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.Parameters.Modify(ParameterType.Speed, SpeedDelta);
		}
	}

	/// <summary>
	/// SC_MAXOVERTHRUST (WS_OVERTHRUSTMAX) — +20% physical damage per
	/// level. Mirrors eAthena status.c:5840 (val2 = 20*val1); Sabine
	/// stores the same scalar in <c>Val3</c> so the existing
	/// <see cref="Sabine.Zone.Battle.BattleCalculator"/> ApplyOverThrust
	/// path can read either OverThrust or MaxOverThrust uniformly.
	/// Cancels SC_OVERTHRUST on start to match status.c:5060-5063.
	/// </summary>
	[StatusEffectHandler(StatusId.MaxOverThrust)]
	public class MaxOverThrustStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			if (target.StatusEffects.Has(StatusId.OverThrust))
				target.StatusEffects.Stop(StatusId.OverThrust);

			effect.Val3 = 20 * effect.Level;
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_MELTDOWN (WS_MELTDOWN) — chance per hit to break the target's
	/// weapon or armor. Mirrors eAthena status.c:5867: val2 = 100*level
	/// (weapon-break per-1000), val3 = 70*level (armor-break per-1000).
	/// Pure flag handler today — the on-hit listener that would roll the
	/// break and dispatch <c>skill_break_equip</c> isn't present in
	/// Sabine's battle pipeline yet. The vals are still snapshotted so
	/// the listener can land without changing the wire format.
	/// </summary>
	[StatusEffectHandler(StatusId.MeltDown)]
	public class MeltDownStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = 100 * effect.Level;
			effect.Val3 = 70 * effect.Level;
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
