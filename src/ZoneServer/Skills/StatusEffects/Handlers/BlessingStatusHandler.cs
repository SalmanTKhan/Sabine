using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_BLESSING — STR/INT/DEX and HIT bonuses. Formula per eAthena
	/// classic (status.c:5821-5826): val2 = skill level for non-undead /
	/// non-demon (or any PC target). v1 ignores undead/demon halving.
	/// HIT bonus is val1 * 2.
	/// </summary>
	[StatusEffectHandler(StatusId.Blessing)]
	public class BlessingStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var statBonus = effect.Level;
			var hitBonus = effect.Level * 2;
			effect.Val2 = statBonus;
			effect.Val3 = hitBonus;

			target.Parameters.Modify(ParameterType.Str, statBonus);
			target.Parameters.Modify(ParameterType.Int, statBonus);
			target.Parameters.Modify(ParameterType.Dex, statBonus);
			target.Parameters.Modify(ParameterType.Hit, hitBonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.Parameters.Modify(ParameterType.Str, -effect.Val2);
			target.Parameters.Modify(ParameterType.Int, -effect.Val2);
			target.Parameters.Modify(ParameterType.Dex, -effect.Val2);
			target.Parameters.Modify(ParameterType.Hit, -effect.Val3);
		}
	}
}
