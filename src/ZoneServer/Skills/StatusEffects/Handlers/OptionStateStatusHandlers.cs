using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// HT_FALCON / SC_FALCONFLY — flips the OPTION_FALCON bit so the
	/// alpha / beta1 client renders the falcon companion sprite next to
	/// the Hunter. Mirrors eAthena pc.c pc_setfalcon, where the bit lives
	/// in <c>sd->sc.option</c> and is broadcast through the actor spawn
	/// / state-change packets. The status itself remains a flag for
	/// gameplay checks (Blitz Beat / Auto-Blitz preconditions).
	/// </summary>
	[StatusEffectHandler(StatusId.FalconFly)]
	public class FalconFlyStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> target.SetOptionFlag(OptionState.Falcon, true);

		public void OnEnd(Character target, StatusEffect effect)
			=> target.SetOptionFlag(OptionState.Falcon, false);
	}

	/// <summary>
	/// KN_RIDING / SC_RIDING — flips the OPTION_RIDING bit so the
	/// Knight / Crusader is rendered on a peco-peco, and grants the
	/// 25% movement-speed bonus from eAthena pc.c status_calc_pc
	/// (DEFAULT_WALK_SPEED * 75 / 100). Mirrors the IncreaseAgi
	/// Speed delta so the parameter cache stays consistent.
	/// </summary>
	[StatusEffectHandler(StatusId.Riding)]
	public class RidingStatusHandler : IStatusEffectHandler
	{
		private const int SpeedDelta = 50;

		public void OnStart(Character target, StatusEffect effect)
		{
			target.SetOptionFlag(OptionState.Riding, true);
			target.Parameters.Modify(ParameterType.Speed, -SpeedDelta);
			effect.Val2 = SpeedDelta;
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.SetOptionFlag(OptionState.Riding, false);
			if (effect.Val2 != 0)
				target.Parameters.Modify(ParameterType.Speed, effect.Val2);
		}
	}
}
