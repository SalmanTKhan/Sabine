using System;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_POISON — DOT, drains 1.5% max-HP every 1500ms. eAthena
	/// classic: total duration 30s + 30s/level. Val4 is repurposed as a
	/// milliseconds-since-last-tick accumulator.
	/// </summary>
	[StatusEffectHandler(StatusId.Poison)]
	public class PoisonStatusHandler : IStatusEffectHandler
	{
		private const int TickIntervalMs = 1500;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val4 = 0;
			target.SetHealthStateFlag(HealthState.Poison, true);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.SetHealthStateFlag(HealthState.Poison, false);
		}

		public void OnTick(Character target, StatusEffect effect, TimeSpan elapsed)
		{
			effect.Val4 += (int)elapsed.TotalMilliseconds;
			if (effect.Val4 < TickIntervalMs)
				return;
			effect.Val4 -= TickIntervalMs;

			var damage = Math.Max(1, target.Parameters.HpMax * 3 / 200); // 1.5%
			target.TakeDamage(damage, effect.Caster);
		}
	}

	/// <summary>
	/// SC_BLEEDING — DOT, drains a small flat amount every 10000ms.
	/// eAthena classic: ~1/100 max-HP per tick. Implemented identically
	/// to Poison but with a longer interval and smaller per-tick %.
	/// </summary>
	[StatusEffectHandler(StatusId.Bleeding)]
	public class BleedingStatusHandler : IStatusEffectHandler
	{
		private const int TickIntervalMs = 10000;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val4 = 0;
			target.SetHealthStateFlag(HealthState.Bleeding, true);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.SetHealthStateFlag(HealthState.Bleeding, false);
		}

		public void OnTick(Character target, StatusEffect effect, TimeSpan elapsed)
		{
			effect.Val4 += (int)elapsed.TotalMilliseconds;
			if (effect.Val4 < TickIntervalMs)
				return;
			effect.Val4 -= TickIntervalMs;

			var damage = Math.Max(1, target.Parameters.HpMax / 100);
			target.TakeDamage(damage, effect.Caster);
		}
	}
}
