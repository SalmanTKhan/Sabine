using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Monk MO_BLADESTOP: when the catch window is active, the next
	/// melee physical hit against the caster is cancelled and both
	/// the attacker and the catcher are stunned for a few seconds.
	/// Scales with skill level: stun = 2s + level seconds.
	/// </summary>
	public static class BladeStopListener
	{
		public static void Subscribe()
		{
			CombatEvents.PreAttackResolve += OnPreAttackResolve;
		}

		private static void OnPreAttackResolve(AttackContext ctx)
		{
			if (ctx.Kind != AttackKind.Physical) return;

			var target = ctx.Target;
			var attacker = ctx.Attacker;
			if (target == null || attacker == null) return;
			if (target == attacker) return;
			if (target.StatusEffects == null) return;

			if (!target.StatusEffects.TryGet(StatusId.BladeStop, out var effect)) return;

			// Only catch melee — skill hits and ranged attacks are
			// allowed through. eAthena: catches non-skill physical
			// hits in the caster's own cell range.
			if (ctx.SkillId != SkillId.None) return;
			if (ctx.IsLongRange) return;

			// Cancel the hit and consume the window.
			ctx.Cancelled = true;
			target.StatusEffects.Stop(StatusId.BladeStop);

			// Stun both combatants. Window scales with the captured
			// skill level (effect.Level).
			var stunDuration = TimeSpan.FromSeconds(2 + effect.Level);
			attacker.StatusEffects?.Start(StatusId.Stun, effect.Level, stunDuration, target);
			target.StatusEffects.Start(StatusId.Stun, effect.Level, stunDuration, target);
		}
	}
}
