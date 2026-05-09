using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	// Sage elemental fields: SA_VOLCANO / SA_DELUGE / SA_VIOLENTGALE /
	// SA_LANDPROTECTOR. The skill handlers (Sage/SageActives.cs) spawn
	// ElementalFieldUnit ground cells; the cells' OnTouch starts the
	// status on a friendly entrant. The +%damage half is consumed by
	// BattleCalculator.GetElementalFieldBonus (Volcano/Deluge/Whirlwind
	// only); the per-stat bumps below ride the same enchant_eff[]
	// ratio table the calc uses, snapshotted in Val2 for clean revert.
	// MagneticEarth stays a marker — its classic effect (suppressing
	// magic on the cell) belongs on the SkillUnit, not on the
	// per-stander status. MagicRod stays a marker — its absorb hook
	// already lives in BattleCalculator.TryAbsorbWithMagicRod.

	/// <summary>
	/// SC_VOLCANO — caster gains +ATK% (half the calc enchant_eff
	/// ratio, eAthena pre-renewal classic) while standing in the
	/// volcano cells. Stacks on top of the +%fire-damage that
	/// GetElementalFieldBonus already applies.
	/// </summary>
	[StatusEffectHandler(StatusId.Volcano)]
	public class VolcanoStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var idx = Math.Clamp(effect.Level - 1, 0, BattleCalculator.EnchantEff.Length - 1);
			var pct = BattleCalculator.EnchantEff[idx] / 2;
			effect.Val2 = pct;
			target.Modifiers.BonusAtkRate += pct;
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Modifiers.BonusAtkRate -= effect.Val2;
		}
	}

	/// <summary>
	/// SC_DELUGE — caster gains +MaxHP% (full enchant_eff ratio)
	/// while standing in the deluge cells. Routes through the new
	/// BonusMaxHpRate bucket (commit A) so the recalc folds it in
	/// before flat MaxHP, matching eAthena status_calc_maxhp.
	/// </summary>
	[StatusEffectHandler(StatusId.Deluge)]
	public class DelugeStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var idx = Math.Clamp(effect.Level - 1, 0, BattleCalculator.EnchantEff.Length - 1);
			var pct = BattleCalculator.EnchantEff[idx];
			effect.Val2 = pct;
			target.Modifiers.AddBonus(ParameterType.MaxHpRate, pct);
			(target as PlayerCharacter)?.Parameters.RecalculateAll();
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 == 0)
				return;
			target.Modifiers.AddBonus(ParameterType.MaxHpRate, -effect.Val2);
			(target as PlayerCharacter)?.Parameters.RecalculateAll();
		}
	}

	/// <summary>
	/// SC_WHIRLWIND (SA_VIOLENTGALE) — caster gains +Flee (full
	/// enchant_eff ratio) while standing in the violent-gale cells.
	/// Routes through AddBonusStat so the bonus survives equip
	/// recalc.
	/// </summary>
	[StatusEffectHandler(StatusId.Whirlwind)]
	public class WhirlwindStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var idx = Math.Clamp(effect.Level - 1, 0, BattleCalculator.EnchantEff.Length - 1);
			var flee = BattleCalculator.EnchantEff[idx];
			effect.Val2 = flee;
			target.AddBonusStat(ParameterType.Flee, flee);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Flee, -effect.Val2);
		}
	}

	/// <summary>SC_MAGNETICEARTH — earth field aura (SA_LANDPROTECTOR). Marker.</summary>
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
