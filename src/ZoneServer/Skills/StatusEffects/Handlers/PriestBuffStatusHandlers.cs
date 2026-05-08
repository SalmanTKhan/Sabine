using System;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_IMPOSITIO (PR_IMPOSITIO) — flat +5 ATK per skill level for the
	/// duration of the buff. Mirrors eAthena status.c (SC_IMPOSITIO branch,
	/// val1 = level → +5*val1 atk). Snapshot in Val2 so the revert stays
	/// symmetric across refreshes.
	/// </summary>
	[StatusEffectHandler(StatusId.ImpositioManus)]
	public class ImpositioManusStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var bonus = 5 * effect.Level;
			effect.Val2 = bonus;
			target.AddBonusStat(ParameterType.Attack, bonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Attack, -effect.Val2);
		}
	}

	/// <summary>
	/// SC_GLORIA (PR_GLORIA) — flat +30 LUK regardless of level. eAthena
	/// status.c SC_GLORIA: val2 = 30. Short duration; the LUK swing also
	/// pulls Critical/Perfect-Dodge along through the parameter cache.
	/// </summary>
	[StatusEffectHandler(StatusId.Gloria)]
	public class GloriaStatusHandler : IStatusEffectHandler
	{
		private const int LukBonus = 30;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = LukBonus;
			target.Parameters.Modify(ParameterType.Luk, LukBonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Parameters.Modify(ParameterType.Luk, -effect.Val2);
		}
	}

	/// <summary>
	/// SC_ASPERSIO (PR_ASPERSIO) — overrides the attacker's weapon
	/// element to Holy for the duration. Pure flag handler: the actual
	/// element swap is consulted by
	/// <see cref="Sabine.Zone.Battle.BattleCalculator.ResolveAttackElement"/>
	/// during physical-attack resolution. Mirrors eAthena
	/// battle_get_atk_ele's SC_ASPERSIO branch.
	/// </summary>
	[StatusEffectHandler(StatusId.Aspersio)]
	public class AspersioStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_LEXAETERNA (PR_LEXAETERNA) — the next single hit on the target
	/// deals 2x damage, then the status is consumed. Pure flag handler;
	/// the multiplier + Stop call are issued from
	/// <see cref="Sabine.Zone.Battle.BattleCalculator"/>. Mirrors eAthena
	/// battle.c SC_AETERNA: damage *= 2, status_change_end on consumption.
	/// </summary>
	[StatusEffectHandler(StatusId.LexAeterna)]
	public class LexAeternaStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_MAGNIFICAT (PR_MAGNIFICAT) — doubles the SP regen rate while
	/// active (eAthena status.c SC_MAGNIFICAT: regen.rate.sp *= 2). Pure
	/// flag handler; the multiplier is read by
	/// <see cref="Sabine.Zone.World.Actors.Components.Characters.RegenComponent.RegenSp"/>.
	/// HP regen is unchanged in the alpha-era formula (renewal also
	/// doubles HP regen, but that branch is left for a renewal pass).
	/// </summary>
	[StatusEffectHandler(StatusId.Magnificat)]
	public class MagnificatStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_KYRIE (PR_KYRIE) — damage shield that absorbs incoming weapon
	/// damage. Capacity is snapshotted at start so refresh casts re-roll
	/// it cleanly. Mirrors eAthena status.c SC_KYRIE init:
	///   val2 = max_hp * (level*2 + 10) / 100  (HP capacity)
	///   val3 = level / 2 + 5                  (hit capacity)
	/// The actual damage absorption is performed in
	/// <see cref="Sabine.Zone.Battle.BattleCalculator"/>.
	/// </summary>
	[StatusEffectHandler(StatusId.KyrieEleison)]
	public class KyrieEleisonStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var maxHp = Math.Max(1, target.Parameters.HpMax);
			effect.Val2 = (int)((long)maxHp * (effect.Level * 2 + 10) / 100);
			effect.Val3 = effect.Level / 2 + 5;
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
