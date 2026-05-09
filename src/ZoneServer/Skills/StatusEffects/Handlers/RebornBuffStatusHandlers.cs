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
	/// SC_BERSERK (LK_BERSERK / Frenzy) — frenzy state. Marker; the
	/// 2x MaxHP / 3x ATK / move-speed / skill-lock / heal-block /
	/// HP-drain effects all need their own pipeline hooks.
	/// </summary>
	[StatusEffectHandler(StatusId.Berserk)]
	public class BerserkStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
