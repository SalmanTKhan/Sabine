using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_PROVOKE — +ATK% / -DEF2% on the target. eAthena: Val2 = 2 + 3*level
	/// (% ATK gain), Val3 = 5 + 5*level (% DEF reduction). Implemented
	/// here as flat additive deltas computed off the target's current
	/// values and snapshotted on the effect for symmetric revert.
	/// </summary>
	[StatusEffectHandler(StatusId.Provoke)]
	public class ProvokeStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var atkPct = 2 + 3 * effect.Level;
			var defPct = 5 + 5 * effect.Level;

			var atkDelta = target.Parameters.AttackBonus * atkPct / 100;
			if (atkDelta == 0)
				atkDelta = atkPct; // ensure visible bump even with 0 base

			var defDelta = target.Parameters.MeleeDefenseBonus * defPct / 100;

			effect.Val2 = atkDelta;
			effect.Val3 = defDelta;

			if (atkDelta != 0)
				target.Parameters.Modify(ParameterType.AttackBonus, atkDelta);
			if (defDelta != 0)
				target.Parameters.Modify(ParameterType.MeleeDefenseBonus, -defDelta);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Parameters.Modify(ParameterType.AttackBonus, -effect.Val2);
			if (effect.Val3 != 0)
				target.Parameters.Modify(ParameterType.MeleeDefenseBonus, effect.Val3);
		}
	}

	/// <summary>
	/// SC_STONE — petrification. Immobilizes the target and drains 1%
	/// max-HP every 5s while active. Val4 is the ms-since-last-tick
	/// accumulator (same convention as Poison/Bleeding).
	/// </summary>
	[StatusEffectHandler(StatusId.Stone)]
	public class StoneStatusHandler : IStatusEffectHandler
	{
		private const int TickIntervalMs = 5000;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val4 = 0;
			target.SetBodyState(BodyState.Stone);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.ClearBodyState(BodyState.Stone);
		}

		public void OnTick(Character target, StatusEffect effect, System.TimeSpan elapsed)
		{
			effect.Val4 += (int)elapsed.TotalMilliseconds;
			if (effect.Val4 < TickIntervalMs)
				return;
			effect.Val4 -= TickIntervalMs;

			var damage = System.Math.Max(1, target.Parameters.HpMax / 100);
			target.TakeDamage(damage, effect.Caster);
		}
	}

	/// <summary>
	/// SC_FREEZE — frozen, immobile. Flag-only. Calculator could later
	/// apply 1.5x damage from Wind / 2x from Fire (Fire also breaks the
	/// freeze).
	/// </summary>
	[StatusEffectHandler(StatusId.Freeze)]
	public class FreezeStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> target.SetBodyState(BodyState.Freeze);

		public void OnEnd(Character target, StatusEffect effect)
			=> target.ClearBodyState(BodyState.Freeze);
	}

	/// <summary>SC_STUN — short interrupt; sets opt1 = Stun.</summary>
	[StatusEffectHandler(StatusId.Stun)]
	public class StunStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> target.SetBodyState(BodyState.Stun);

		public void OnEnd(Character target, StatusEffect effect)
			=> target.ClearBodyState(BodyState.Stun);
	}

	/// <summary>
	/// SC_SLEEP — asleep, can't act. Damage taken is 1.5x (calculator)
	/// and the first hit ends sleep (Character.TakeDamage hook).
	/// </summary>
	[StatusEffectHandler(StatusId.Sleep)]
	public class SleepStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> target.SetBodyState(BodyState.Sleep);

		public void OnEnd(Character target, StatusEffect effect)
			=> target.ClearBodyState(BodyState.Sleep);
	}

	/// <summary>SC_SILENCE — can't cast skills. Sets opt2 = Silence.</summary>
	[StatusEffectHandler(StatusId.Silence)]
	public class SilenceStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> target.SetHealthStateFlag(HealthState.Silence, true);

		public void OnEnd(Character target, StatusEffect effect)
			=> target.SetHealthStateFlag(HealthState.Silence, false);
	}

	/// <summary>SC_BLIND — -25% HIT/FLEE applied in calc; sets opt2 = Blind.</summary>
	[StatusEffectHandler(StatusId.Blind)]
	public class BlindStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> target.SetHealthStateFlag(HealthState.Blind, true);

		public void OnEnd(Character target, StatusEffect effect)
			=> target.SetHealthStateFlag(HealthState.Blind, false);
	}

	/// <summary>SC_CONFUSION — randomised movement; sets opt2 = Confusion.</summary>
	[StatusEffectHandler(StatusId.Confusion)]
	public class ConfusionStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> target.SetHealthStateFlag(HealthState.Confusion, true);

		public void OnEnd(Character target, StatusEffect effect)
			=> target.SetHealthStateFlag(HealthState.Confusion, false);
	}

	/// <summary>SC_CHAOS — Sage / etc. No opt2 bit in Sabine's enum yet; flag-only.</summary>
	[StatusEffectHandler(StatusId.Chaos)]
	public class ChaosStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
