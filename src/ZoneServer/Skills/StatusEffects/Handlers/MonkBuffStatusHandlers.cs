using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_EXPLOSIONSPIRITS (MO_EXPLOSIONSPIRITS) — +20 ATK and +50 CRIT
	/// while active. eAthena status.c:5283: val2 = 20, val3 = 50. The
	/// buff ends when the Monk takes any non-zero hit (eAthena ends on
	/// damage receipt), but Sabine doesn't have a generic "ends on hit"
	/// listener — that's deferred until a broader Berserk/Explosion
	/// shared listener lands. Today the buff simply runs for the full
	/// 60s window.
	/// </summary>
	[StatusEffectHandler(StatusId.ExplosionSpirits)]
	public class ExplosionSpiritsStatusHandler : IStatusEffectHandler
	{
		private const int AtkBonus = 20;
		private const int CritBonus = 50;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = AtkBonus;
			effect.Val3 = CritBonus;
			target.AddBonusStat(ParameterType.Attack, AtkBonus);
			target.AddBonusStat(ParameterType.Critical, CritBonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Attack, -effect.Val2);
			if (effect.Val3 != 0)
				target.AddBonusStat(ParameterType.Critical, -effect.Val3);
		}
	}

	/// <summary>
	/// SC_MENTALSTRENGTH (MO_STEELBODY) — flag-only; the 90% damage
	/// reduction is applied in <c>BattleCalculator.ApplyStatusMultipliers</c>.
	/// Registering the handler closes the dispatch table.
	/// </summary>
	[StatusEffectHandler(StatusId.MentalStrength)]
	public class MentalStrengthStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
