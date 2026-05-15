using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_CHASEWALK (ST_CHASEWALK) — toggle hide-with-slow. eAthena
	/// status.c:5537 stores val3 = 35 - 5*level for the speed reduction
	/// and val4 = 10 + 2*level for the periodic SP drain. Marker today
	/// because Sabine has no ChaseWalk-aware hide flag, no per-status
	/// SP-drain tick scheduler, and no SC_SPIRIT/SL_ROGUE bonus path —
	/// the snapshot below stores the eAthena vals so the listener can
	/// land later without changing the wire format.
	/// </summary>
	[StatusEffectHandler(StatusId.ChaseWalk)]
	public class ChaseWalkStatusHandler : IStatusEffectHandler
	{
		// val3 in eAthena is a percent slowdown; Sabine's Speed param
		// is milliseconds-per-cell, so convert via *2 (base 200ms).
		public void OnStart(Character target, StatusEffect effect)
		{
			var slowPct = 35 - 5 * effect.Level;
			effect.Val3 = slowPct * 2;
			effect.Val4 = 10 + 2 * effect.Level;

			target.Parameters.Modify(ParameterType.Speed, effect.Val3);
			target.SetOptionFlag(OptionState.Hide, true);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val3 != 0)
				target.Parameters.Modify(ParameterType.Speed, -effect.Val3);
			target.SetOptionFlag(OptionState.Hide, false);
		}
	}

	/// <summary>
	/// SC_PRESERVE (ST_PRESERVE) — flag-only. Plagiarism's "lock copied
	/// skill" arm reads this on its overwrite check; until that handler
	/// ships there's nothing to lock, so OnStart/OnEnd are pure no-ops.
	/// </summary>
	[StatusEffectHandler(StatusId.Preserve)]
	public class PreserveStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_TUNNELDRIVE (RG_TUNNELDRIVE) — speed burst while Hiding is
	/// active. eAthena status.c:5639 grants the Rogue a per-level walk-
	/// speed delta that cancels Hiding's natural slowdown and tips
	/// faster at higher levels. Sabine's Hiding doesn't slow movement
	/// today, so this lands as a straight Speed bonus equivalent to the
	/// eAthena per-cell delta (30 ms per level, capped to keep level-5
	/// from instant-walking through walls).
	/// </summary>
	[StatusEffectHandler(StatusId.TunnelDrive)]
	public class TunnelDriveStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var delta = System.Math.Clamp(30 * effect.Level, 0, 120);
			effect.Val2 = delta;
			target.Parameters.Modify(ParameterType.Speed, -delta);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Parameters.Modify(ParameterType.Speed, effect.Val2);
		}
	}

	/// <summary>
	/// SC_REJECTSWORD (ST_REJECTSWORD) — sword-only reflect with limited
	/// charges. eAthena status.c:5706: val2 = 15*level (per-100 reflect
	/// chance), val3 = 3 (charges remaining). Snapshot here so the future
	/// on-hit listener can decrement val3 and end the status when it
	/// hits zero, the same way SC_KYRIE's val3 is consumed.
	/// </summary>
	[StatusEffectHandler(StatusId.RejectSword)]
	public class RejectSwordStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = 15 * effect.Level;
			effect.Val3 = 3;
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
