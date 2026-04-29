using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_ANGELUS — multiplicative defense bonus. Formula per eAthena
	/// classic (status.c:5862, 3716): val2 = 5 * level, applied as
	/// def2 += def2 * val2 / 100. The exact applied delta is snapshotted
	/// on Val3 so that revert is symmetric even if Defense2 changes
	/// during the buff.
	/// </summary>
	[StatusEffectHandler(StatusId.Angelus)]
	public class AngelusStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var percent = 5 * effect.Level;
			effect.Val2 = percent;

			var currentDef2 = target.Parameters.Get(ParameterType.MeleeDefenseBonus);
			var delta = currentDef2 * percent / 100;
			effect.Val3 = delta;

			if (delta != 0)
				target.Parameters.Modify(ParameterType.MeleeDefenseBonus, delta);

			target.SetHealthStateFlag(HealthState.Angelus, true);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val3 != 0)
				target.Parameters.Modify(ParameterType.MeleeDefenseBonus, -effect.Val3);

			target.SetHealthStateFlag(HealthState.Angelus, false);
		}
	}
}
