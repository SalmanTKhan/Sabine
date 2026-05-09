using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_AURABLADE (LK_AURABLADE) — +10*level flat ATK on hits.
	/// The flat-ATK addition is read by
	/// <c>BattleCalculator.GetAuraBladeBonus</c> from the buff's
	/// level, so this handler stays a marker — it just registers the
	/// status in the dispatch table.
	/// </summary>
	[StatusEffectHandler(StatusId.AuraBlade)]
	public class AuraBladeStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_CONCENTRATION (LK_CONCENTRATION) — eAthena status.c gives
	/// +5%/level BAtk via status_calc_atk and +10%/level Hit via
	/// status_calc_hit. We ride the existing BonusAtkRate / BonusHit
	/// buckets so the bonuses flow through the calculator and the
	/// equip-bonus recalc without a separate code path. The
	/// -5%/level self-DEF half is marker-only until the damage
	/// pipeline grows a hook for caster-side defense reduction.
	/// </summary>
	[StatusEffectHandler(StatusId.LkConcentration)]
	public class LkConcentrationStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var atkPct = 5 * effect.Level;
			var hit = 10 * effect.Level;

			effect.Val2 = atkPct;
			effect.Val3 = hit;

			target.Modifiers.BonusAtkRate += atkPct;
			target.AddBonusStat(ParameterType.Hit, hit);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Modifiers.BonusAtkRate -= effect.Val2;
			if (effect.Val3 != 0)
				target.AddBonusStat(ParameterType.Hit, -effect.Val3);
		}
	}

	/// <summary>
	/// SC_ASSUMPTIO (HP_ASSUMPTIO) — halves physical damage taken on
	/// the target. The 50% reduction is now consumed in
	/// <c>BattleCalculator.ApplyAssumptioReduction</c>, so the
	/// handler stays a marker.
	/// </summary>
	[StatusEffectHandler(StatusId.Assumptio)]
	public class AssumptioStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_MAGICPOWER (HW_MAGICPOWER) — +5%/level MATK on the next
	/// spell cast. The MATK% rides BonusMatkRate; CalcMagic ends
	/// the status after the first amplified hit (the "single-shot,
	/// breaks on cast" semantic). Val2 stores the snapshotted
	/// percent so the OnEnd revert is symmetric.
	/// </summary>
	[StatusEffectHandler(StatusId.MagicPower)]
	public class MagicPowerStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var matkPct = 5 * effect.Level;
			effect.Val2 = matkPct;
			target.Modifiers.BonusMatkRate += matkPct;
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Modifiers.BonusMatkRate -= effect.Val2;
		}
	}

	/// <summary>
	/// SC_PARRYING (LK_PARRYING) — block-chance buff. The roll
	/// lives in <c>BattleCalculator.TryParryingBlock</c>, which
	/// reads the buff's level directly. Marker handler.
	/// </summary>
	[StatusEffectHandler(StatusId.Parrying)]
	public class ParryingStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_TENSIONRELAX (LK_TENSIONRELAX) — accelerated HP regen.
	/// The 3x amplification on the natural HP tick lives in
	/// <c>RegenComponent.RegenHp</c>, which reads the status by
	/// Has, so this handler stays a marker.
	/// </summary>
	[StatusEffectHandler(StatusId.TensionRelax)]
	public class TensionRelaxStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_BERSERK (LK_BERSERK / Frenzy) — frenzy state. Pays a 50%
	/// current-HP + 100% MaxSP entry cost, grants +200% ATK and
	/// +100% MaxHP, locks skill use, and drains 5% MaxHP every 15s.
	/// HealHp is guarded centrally by Berserk on Character.cs so
	/// every heal source (regen tick / items / AL_HEAL) is blocked
	/// without scattering checks. On expiry, HP is forced to 100
	/// and SP is zeroed (eAthena classic fail-state). The +30% move
	/// speed half is deferred — StatusEffect's Val1 (level) and
	/// Val4 (tick accumulator) are claimed and there's no slot for
	/// a snapshotted speed delta; that lands with a future
	/// BonusSpeedRate pass.
	/// </summary>
	[StatusEffectHandler(StatusId.Berserk)]
	public class BerserkStatusHandler : IStatusEffectHandler
	{
		private const int AtkRatePct = 200;
		private const int MaxHpRatePct = 100;
		private const int DrainTickMs = 15_000;
		private const int DrainPctOfMaxHp = 5;

		public void OnStart(Character target, StatusEffect effect)
		{
			var p = target.Parameters;

			// Entry cost: -50% current HP, all SP. Compute pre-recalc
			// so the rate boost below applies to the post-cost MaxHp,
			// matching eAthena's "pay then recalc" ordering.
			p.Modify(ParameterType.Hp, -(p.Hp / 2));
			p.Modify(ParameterType.Sp, -p.Sp);

			target.Modifiers.BonusAtkRate += AtkRatePct;
			target.Modifiers.AddBonus(ParameterType.MaxHpRate, MaxHpRatePct);
			effect.Val2 = AtkRatePct;
			effect.Val3 = MaxHpRatePct;
			effect.Val4 = 0;

			if (target is PlayerCharacter pc)
			{
				pc.Parameters.RecalculateAll();
				pc.SetSkillBlock(true);
			}
		}

		public void OnTick(Character target, StatusEffect effect, System.TimeSpan elapsed)
		{
			effect.Val4 += (int)elapsed.TotalMilliseconds;
			if (effect.Val4 < DrainTickMs)
				return;
			effect.Val4 -= DrainTickMs;

			var damage = System.Math.Max(1, target.Parameters.HpMax * DrainPctOfMaxHp / 100);
			target.TakeDamage(damage, effect.Caster ?? target);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Modifiers.BonusAtkRate -= effect.Val2;
			if (effect.Val3 != 0)
				target.Modifiers.AddBonus(ParameterType.MaxHpRate, -effect.Val3);

			if (target is PlayerCharacter pc)
			{
				pc.Parameters.RecalculateAll();
				pc.SetSkillBlock(false);
			}

			// eAthena fail-state: drop HP to 100, SP to 0. Only when
			// the buff expires naturally — IsDead means we're being
			// torn down by death cleanup and the HP write is moot.
			if (!target.IsDead)
			{
				var hpDelta = 100 - target.Parameters.Hp;
				if (hpDelta != 0)
					target.Parameters.Modify(ParameterType.Hp, hpDelta);
				target.Parameters.Modify(ParameterType.Sp, -target.Parameters.Sp);
			}
		}
	}
}
