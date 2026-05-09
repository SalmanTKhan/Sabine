using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_MARIONETTE / MARIONETTE2 in eAthena pair the two performers
	/// and transfer half of each side's primary stats. Sabine collapses
	/// the two SC entries into a single marker — the linkage and the
	/// stat transfer both wait on the ensemble framework. Pure flag.
	/// </summary>
	[StatusEffectHandler(StatusId.Marionette)]
	public class MarionetteStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_LONGING (CG_LONGINGFREEDOM) — flag that cancels the ensemble
	/// move lock. Pure flag; Sabine doesn't model the move lock yet.
	/// </summary>
	[StatusEffectHandler(StatusId.LongingFreedom)]
	public class LongingFreedomStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_HERMODE (CG_HERMODE) — flag for dispel immunity. Status-removal
	/// logic doesn't read it yet; pure flag.
	/// </summary>
	[StatusEffectHandler(StatusId.Hermode)]
	public class HermodeStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
