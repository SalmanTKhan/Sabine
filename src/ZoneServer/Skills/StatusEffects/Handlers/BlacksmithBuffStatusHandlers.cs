using System;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_ADRENALINE (BS_ADRENALINE) — flat ASPD bonus while wielding an
	/// Axe or Mace. eAthena status.c:5849-5851 sets val3 = 300 for self-cast
	/// (val2 != 0 there means the buff is a party rebroadcast and gets the
	/// reduced 200/+20% rate). Sabine's <see cref="ParameterType.AspdBonus"/>
	/// is in whole-percent units like TwoHandQuicken, so the self-cast value
	/// here is +30. Party rebroadcast lands when BS_ADRENALINE2 ships.
	/// Weapon-type gate is enforced in the skill handler so the cast fails
	/// outright; this handler just snapshots the bonus for symmetric revert.
	/// </summary>
	[StatusEffectHandler(StatusId.AdrenalineRush)]
	public class AdrenalineRushStatusHandler : IStatusEffectHandler
	{
		private const int AspdBonus = 30;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = AspdBonus;
			target.Parameters.Modify(ParameterType.AspdBonus, AspdBonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Parameters.Modify(ParameterType.AspdBonus, -effect.Val2);
		}
	}

	/// <summary>
	/// SC_WEAPONPERFECTION (BS_WEAPONPERFECT) — bypasses the weapon-vs-size
	/// damage penalty. Pure flag handler; the bypass is consulted in
	/// <see cref="Sabine.Zone.Battle.BattleCalculator.ApplySizePenalty"/>.
	/// Mirrors eAthena battle.c:1371 where SC_WEAPONPERFECTION sets the
	/// `i |= 8` flag passed to battle_calc_base_damage to skip the penalty.
	/// </summary>
	[StatusEffectHandler(StatusId.WeaponPerfection)]
	public class WeaponPerfectionStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_OVERTHRUST (BS_OVERTHRUST) — +5% physical damage per skill level
	/// for the duration. eAthena status.c:5843-5845 stores val3 = 5*level
	/// and battle.c:1415-1416 adds it to the skill ratio. Sabine applies
	/// the bonus post-defense in
	/// <see cref="Sabine.Zone.Battle.BattleCalculator"/>; this handler
	/// only snapshots val3 so the bonus is stable across a buff refresh.
	/// </summary>
	[StatusEffectHandler(StatusId.OverThrust)]
	public class OverThrustStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val3 = 5 * effect.Level;
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_MAXIMIZEPOWER (BS_MAXIMIZE) — forces every weapon swing to roll
	/// the maximum ATK value. Mirrors eAthena battle.c:846 (atkmin = atkmax)
	/// and the SCB_REGEN-tied SP drain at status.c:7062-7067 (1 SP per
	/// second; the buff ends when SP can't cover the upkeep). The damage
	/// roll override is consumed in
	/// <see cref="Sabine.Zone.Battle.BattleCalculator"/>; this handler runs
	/// the upkeep tick. Only meaningful in pre-renewal — renewal already
	/// uses flat WeaponATK with no roll variance.
	/// </summary>
	[StatusEffectHandler(StatusId.PowerMaximize)]
	public class PowerMaximizeStatusHandler : IStatusEffectHandler
	{
		private const int TickIntervalMs = 1000;
		private const int SpCost = 1;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val4 = 0;
		}

		public void OnEnd(Character target, StatusEffect effect) { }

		public void OnTick(Character target, StatusEffect effect, TimeSpan elapsed)
		{
			effect.Val4 += (int)elapsed.TotalMilliseconds;
			if (effect.Val4 < TickIntervalMs)
				return;
			effect.Val4 -= TickIntervalMs;

			if (!target.TrySpendSp(SpCost))
				target.StatusEffects.Stop(StatusId.PowerMaximize);
		}
	}
}
