using System;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Cross-system hooks fired by the combat pipeline. Listened to by
	/// subsystems that react to attacks or skill casts: Monk Combo,
	/// Sage Auto-Spell / Magic Rod, Rogue Plagiarism, Crusader
	/// Reflect Shield / Devotion. Add subscribers from subsystem init
	/// code; handlers should not subscribe per-cast.
	/// </summary>
	public static class CombatEvents
	{
		/// <summary>
		/// Fired before <see cref="BattleCalculator.Calc"/> rolls damage.
		/// Listeners may mutate <see cref="AttackContext.Target"/> to
		/// redirect (Devotion) or set <see cref="AttackContext.Cancelled"/>
		/// to short-circuit to a miss (Auto-Guard).
		/// </summary>
		public static event Action<AttackContext> PreAttackResolve;

		/// <summary>
		/// Fired after <see cref="BattleCalculator.Calc"/> resolves an
		/// attack, regardless of hit / miss. Listeners may inspect the
		/// result and react (Reflect Shield counter-hit, Auto-Spell
		/// proc) but should not mutate the AttackResult itself.
		/// </summary>
		public static event Action<AttackContext, AttackResult> AttackResolved;

		internal static void RaisePreAttackResolve(AttackContext ctx)
			=> PreAttackResolve?.Invoke(ctx);

		/// <summary>
		/// Fired when a skill handler begins execution, before damage is
		/// rolled. Used by Auto-Spell to roll an extra cast.
		/// </summary>
		public static event Action<Sabine.Zone.World.Actors.Character, Sabine.Zone.Skills.Skill> SkillCast;

		internal static void RaiseAttackResolved(AttackContext ctx, AttackResult result)
			=> AttackResolved?.Invoke(ctx, result);

		internal static void RaiseSkillCast(Sabine.Zone.World.Actors.Character caster, Sabine.Zone.Skills.Skill skill)
			=> SkillCast?.Invoke(caster, skill);
	}
}
