using System;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	// Each song / dance buff is modeled as a distinct status so the
	// gameplay bonus lives in OnStart/OnEnd, mirroring eAthena's SC_*
	// table. SongUnit.OnTouch refreshes the duration each tick while
	// the character stands inside the unit's footprint, so OnStart
	// fires once on entry and OnEnd once on leave — no per-tick
	// bonus thrashing.

	/// <summary>BA_WHISTLE: +Flee 4*level for the duration.</summary>
	[StatusEffectHandler(StatusId.Whistle)]
	public class WhistleStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var bonus = 4 * effect.Level;
			effect.Val2 = bonus;
			target.AddBonusStat(ParameterType.Flee, bonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Flee, -effect.Val2);
		}
	}

	/// <summary>BA_ASSASSINCROSS (Sunset): +ASPD 5*level%.</summary>
	[StatusEffectHandler(StatusId.AssassinCross)]
	public class AssassinCrossStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var bonus = 5 * effect.Level;
			effect.Val2 = bonus;
			target.AddBonusStat(ParameterType.AspdBonus, bonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.AspdBonus, -effect.Val2);
		}
	}

	/// <summary>BA_APPLEIDUN: +MaxHP 5*level%.</summary>
	[StatusEffectHandler(StatusId.AppleIdun)]
	public class AppleIdunStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var pct = 5 * effect.Level;
			var bonus = target.Parameters.HpMax * pct / 100;
			effect.Val2 = bonus;
			if (bonus > 0)
				target.AddBonusStat(ParameterType.HpMax, bonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.HpMax, -effect.Val2);
		}
	}

	/// <summary>BA_POEMBRAGI: marker only — cast-time reduction defers
	/// until the cast-bar pipeline ships. Snapshots the eAthena delta
	/// (3*level%) so the future consumer can read it.</summary>
	[StatusEffectHandler(StatusId.PoemBragi)]
	public class PoemBragiStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = 3 * effect.Level;
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>DC_HUMMING: +HIT 4*level.</summary>
	[StatusEffectHandler(StatusId.Humming)]
	public class HummingStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var bonus = 4 * effect.Level;
			effect.Val2 = bonus;
			target.AddBonusStat(ParameterType.Hit, bonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Hit, -effect.Val2);
		}
	}

	/// <summary>DC_DONTFORGETME: enemy debuff — slows movement and
	/// reduces ASPD by 3*level%.</summary>
	[StatusEffectHandler(StatusId.DontForgetMe)]
	public class DontForgetMeStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var slow = 3 * effect.Level;
			effect.Val2 = slow;
			effect.Val3 = slow * 2;
			target.AddBonusStat(ParameterType.AspdBonus, -slow);
			target.Parameters.Modify(ParameterType.Speed, effect.Val3);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.AspdBonus, effect.Val2);
			if (effect.Val3 != 0)
				target.Parameters.Modify(ParameterType.Speed, -effect.Val3);
		}
	}

	/// <summary>DC_FORTUNEKISS: +Critical 5+2*level.</summary>
	[StatusEffectHandler(StatusId.FortuneKiss)]
	public class FortuneKissStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var bonus = 5 + 2 * effect.Level;
			effect.Val2 = bonus;
			target.AddBonusStat(ParameterType.Critical, bonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Critical, -effect.Val2);
		}
	}

	/// <summary>DC_SERVICEFORYOU: +INT bonus (eAthena reduces SP cost,
	/// but Sabine has no skill-SP-cost hook today; +INT is the closest
	/// stand-in until the cast pipeline ships).</summary>
	[StatusEffectHandler(StatusId.ServiceForYou)]
	public class ServiceForYouStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var bonus = 1 + effect.Level / 2;
			effect.Val2 = bonus;
			target.Parameters.Modify(ParameterType.Int, bonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Parameters.Modify(ParameterType.Int, -effect.Val2);
		}
	}

	/// <summary>BA_DISSONANCE: enemy DoT — ticks small flat damage
	/// every refresh window. eAthena status.c:5613 stores level in
	/// val1; damage is 24+8*level per 3s.</summary>
	[StatusEffectHandler(StatusId.Dissonance)]
	public class DissonanceStatusHandler : IStatusEffectHandler
	{
		private const int TickIntervalMs = 3000;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val4 = 0;
		}

		public void OnEnd(Character target, StatusEffect effect) { }

		public void OnTick(Character target, StatusEffect effect, TimeSpan elapsed)
		{
			effect.Val4 += (int)elapsed.TotalMilliseconds;
			if (effect.Val4 < TickIntervalMs) return;
			effect.Val4 -= TickIntervalMs;

			var damage = 24 + 8 * effect.Level;
			target.TakeDamage(damage, effect.Caster);
		}
	}

	/// <summary>DC_UGLYDANCE: enemy debuff — drains SP every 3s.</summary>
	[StatusEffectHandler(StatusId.UglyDance)]
	public class UglyDanceStatusHandler : IStatusEffectHandler
	{
		private const int TickIntervalMs = 3000;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val4 = 0;
		}

		public void OnEnd(Character target, StatusEffect effect) { }

		public void OnTick(Character target, StatusEffect effect, TimeSpan elapsed)
		{
			effect.Val4 += (int)elapsed.TotalMilliseconds;
			if (effect.Val4 < TickIntervalMs) return;
			effect.Val4 -= TickIntervalMs;

			var drain = 5 + effect.Level * 3;
			target.TrySpendSp(Math.Min(drain, target.Parameters.Sp));
		}
	}

	/// <summary>BD_LULLABY: enemy debuff — rolls a Sleep proc every
	/// refresh window while inside the aura. eAthena status.c:5644 uses
	/// a level-scaled chance per tick.</summary>
	[StatusEffectHandler(StatusId.Lullaby)]
	public class LullabyStatusHandler : IStatusEffectHandler
	{
		private const int TickIntervalMs = 3000;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val4 = 0;
		}

		public void OnEnd(Character target, StatusEffect effect) { }

		public void OnTick(Character target, StatusEffect effect, TimeSpan elapsed)
		{
			if (target.StatusEffects.Has(StatusId.Sleep)) return;

			effect.Val4 += (int)elapsed.TotalMilliseconds;
			if (effect.Val4 < TickIntervalMs) return;
			effect.Val4 -= TickIntervalMs;

			var chance = 5 + effect.Level * 2;
			if (RandomProvider.Get().Next(100) >= chance) return;

			target.StatusEffects.Start(StatusId.Sleep, 1, TimeSpan.FromSeconds(10 + effect.Level), effect.Caster);
		}
	}

	/// <summary>BD_ROKISWEIL (MentalSensing / Loki's Veil): dispels
	/// Hide and Cloaking from enemies inside the aura, every tick.</summary>
	[StatusEffectHandler(StatusId.MentalSensing)]
	public class MentalSensingStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			if (target.StatusEffects.Has(StatusId.Hiding))
				target.StatusEffects.Stop(StatusId.Hiding);
			if (target.StatusEffects.Has(StatusId.Cloaking))
				target.StatusEffects.Stop(StatusId.Cloaking);
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>BD_RICHMANKIM ("Mr. Kim a Rich Man"): +25+25*level%
	/// EXP gain for party members in the aura. The actual multiplier
	/// lives in <c>RewardModifiers.GetExpMultiplier</c>; this handler
	/// is a marker so the status icon broadcasts.</summary>
	[StatusEffectHandler(StatusId.RichmanKim)]
	public class RichmanKimStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
