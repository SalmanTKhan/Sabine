using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_MINDBREAKER (PF_MINDBREAKER) — target debuff: +MATK bonus and
	/// -MDEF on the bearer. eAthena status.c:5760 stores
	///   val2 = 20 + (level - 1) * 2   (matk percent boost)
	///   val3 = 10 + (level - 1) * 4   (mdef percent reduction)
	/// Sabine applies the deltas through the existing AddBonusStat
	/// pipeline so the recalc folds them in cleanly.
	/// </summary>
	[StatusEffectHandler(StatusId.MindBreaker)]
	public class MindBreakerStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var lvl = effect.Level;

			// MDEF reduction (target loses some magic defense).
			var mdefCut = 10 + (lvl - 1) * 4;
			effect.Val3 = mdefCut;
			target.AddBonusStat(ParameterType.MagicDefenseBonus, -mdefCut);

			// MATK boost - target's outgoing magic damage is amplified
			// so reflected magic and self-cast spells hit harder. Stored
			// here as a Val2 marker; the BattleCalculator side picks it
			// up via Has-check when it ships.
			effect.Val2 = 20 + (lvl - 1) * 2;
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val3 != 0)
				target.AddBonusStat(ParameterType.MagicDefenseBonus, effect.Val3);
		}
	}
}
