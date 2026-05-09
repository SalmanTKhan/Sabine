using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_WINDWALK (SN_WINDWALK) — flat Flee bonus and a movement-speed
	/// buff. Mirrors eAthena status.c:5640: <c>val2 = (val1+1)/2</c>, so
	/// the flee bonus is 1/1/2/2/3/3/4/4/5/5 across levels 1–10. Speed
	/// follows the existing IncreaseAgi pattern (negative SpeedDelta =
	/// faster); chosen as <c>level * 5</c> to scale with skill level
	/// without overlapping IncAgi's flat 50ms.
	/// </summary>
	[StatusEffectHandler(StatusId.WindWalk)]
	public class WindWalkStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var fleeBonus = (effect.Level + 1) / 2;
			var speedDelta = effect.Level * 5;

			effect.Val2 = fleeBonus;
			effect.Val3 = speedDelta;

			target.AddBonusStat(ParameterType.Flee, fleeBonus);
			target.Parameters.Modify(ParameterType.Speed, -speedDelta);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Flee, -effect.Val2);
			if (effect.Val3 != 0)
				target.Parameters.Modify(ParameterType.Speed, effect.Val3);
		}
	}

	/// <summary>
	/// SC_TRUESIGHT (SN_SIGHT / Falcon Eyes) — self buff. eAthena
	/// status.c:5871 sets <c>val2 = 10*val1</c> (critical) and
	/// <c>val3 = 3*val1</c> (hit), and the SCB mask (status.c:300) also
	/// pulls every primary stat up by <c>level/3</c>. Only the hit leg
	/// rides through the existing parameter cache today; the crit and
	/// per-stat legs are deferred — neither bucket is wired into the
	/// status pipeline yet, and grafting them on would balloon this
	/// commit. Hit alone is enough for the skill to feel real on a
	/// Sniper rotation.
	/// </summary>
	[StatusEffectHandler(StatusId.TrueSight)]
	public class TrueSightStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var hitBonus = 3 * effect.Level;
			effect.Val3 = hitBonus;
			target.AddBonusStat(ParameterType.Hit, hitBonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val3 != 0)
				target.AddBonusStat(ParameterType.Hit, -effect.Val3);
		}
	}
}
