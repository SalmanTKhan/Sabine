using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_DECREASEAGI — flat AGI penalty and movement-speed reduction.
	/// Cancels any active IncreaseAgi when applied. Formula per eAthena
	/// classic (status.c:5249): val2 = 2 + skill level.
	/// </summary>
	[StatusEffectHandler(StatusId.DecreaseAgi)]
	public class DecreaseAgiStatusHandler : IStatusEffectHandler
	{
		private const int SpeedDelta = 50;

		public void OnStart(Character target, StatusEffect effect)
		{
			if (target.StatusEffects.Has(StatusId.IncreaseAgi))
				target.StatusEffects.Stop(StatusId.IncreaseAgi);

			var agiPenalty = 2 + effect.Level;
			effect.Val2 = agiPenalty;

			target.Parameters.Modify(ParameterType.Agi, -agiPenalty);
			target.Parameters.Modify(ParameterType.Speed, SpeedDelta);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.Parameters.Modify(ParameterType.Agi, effect.Val2);
			target.Parameters.Modify(ParameterType.Speed, -SpeedDelta);
		}
	}
}
