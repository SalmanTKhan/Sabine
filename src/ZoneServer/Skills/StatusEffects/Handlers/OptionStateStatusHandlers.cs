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
	/// Knight / Crusader is rendered on a peco-peco. Movement-speed
	/// bonus is layered on separately via the parameter cache; this
	/// handler just owns the visual flag.
	/// </summary>
	[StatusEffectHandler(StatusId.Riding)]
	public class RidingStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> target.SetOptionFlag(OptionState.Riding, true);

		public void OnEnd(Character target, StatusEffect effect)
			=> target.SetOptionFlag(OptionState.Riding, false);
	}
}
