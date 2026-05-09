using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_AURABLADE (LK_AURABLADE) — +10*level flat ATK on hits.
	/// Marker today; the flat-ATK addition lives in the damage
	/// pipeline (eAthena battle.c) and isn't wired in Sabine yet.
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
	/// the target. eAthena reads the status in the damage pipeline
	/// (battle.c); marker-only until that hook lands in
	/// BattleCalculator. Registering it explicitly keeps the
	/// dispatch table complete.
	/// </summary>
	[StatusEffectHandler(StatusId.Assumptio)]
	public class AssumptioStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_MAGICPOWER (HW_MAGICPOWER) — +5%/level MATK on the next
	/// spell cast. We ride BonusMatkRate so the multiplier flows
	/// through CalcMagic without a separate code path. Val2 stores
	/// the snapshotted percent so the revert is symmetric. The
	/// "single-shot, breaks on cast" gate is not yet wired — the
	/// status simply expires on its 30s timer for now.
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
}
