using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_SACRIFICE (PA_SACRIFICE) — flag handler that snapshots the
	/// hit-charge counter. Mirrors eAthena status.c:5307 (<c>val2 = 5</c>,
	/// <c>tick = -1</c>). The redirect arm hasn't shipped — the on-hit
	/// pipeline doesn't grow a "redirect to bonded ally" hook in this
	/// commit. When that hook lands, it decrements val2 and ends the
	/// buff at zero, the same way Kyrie's val3 is consumed.
	/// </summary>
	[StatusEffectHandler(StatusId.Sacrifice)]
	public class SacrificeStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = 5;
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_GOSPEL (PA_GOSPEL) — pure flag. The 10s random-roll dispatcher
	/// the buff is supposed to drive doesn't live anywhere yet, so this
	/// is no-op. Once the dispatcher lands, the OnTick read here will
	/// fire it.
	/// </summary>
	[StatusEffectHandler(StatusId.Gospel)]
	public class GospelStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
