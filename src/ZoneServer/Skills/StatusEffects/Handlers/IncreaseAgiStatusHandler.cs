using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_INCREASEAGI — flat AGI bonus and movement-speed boost. Cancels
	/// any active DecreaseAgi when applied. Formula per eAthena classic
	/// (status.c:5249): val2 = 2 + skill level.
	/// </summary>
	[StatusEffectHandler(StatusId.IncreaseAgi)]
	public class IncreaseAgiStatusHandler : IStatusEffectHandler
	{
		private const int SpeedDelta = 50;

		public void OnStart(Character target, StatusEffect effect)
		{
			if (target.StatusEffects.Has(StatusId.DecreaseAgi))
				target.StatusEffects.Stop(StatusId.DecreaseAgi);

			var agiBonus = 2 + effect.Level;
			effect.Val2 = agiBonus;

			target.Parameters.Modify(ParameterType.Agi, agiBonus);
			target.Parameters.Modify(ParameterType.Speed, -SpeedDelta);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.Parameters.Modify(ParameterType.Agi, -effect.Val2);
			target.Parameters.Modify(ParameterType.Speed, SpeedDelta);
		}
	}
}
