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

		/// <summary>
		/// Fired after a monster's death pipeline runs (EXP / drops
		/// already issued). Listeners react to "I just killed something"
		/// — HW_SOULDRAIN's SP refund, future kill quests / achievement
		/// counters. Killer may be null (timed despawn, GM kill).
		/// </summary>
		public static event Action<Sabine.Zone.World.Actors.Monster, Sabine.Zone.World.Actors.Character> MonsterKilled;

		internal static void RaiseMonsterKilled(Sabine.Zone.World.Actors.Monster monster, Sabine.Zone.World.Actors.Character killer)
			=> MonsterKilled?.Invoke(monster, killer);

		/// <summary>
		/// Fired when a PlayerCharacter is killed by another character.
		/// Killer may be null for self-deaths (poison tick, fall damage,
		/// suicide). Subscribers: Battleground score, future PvP-rank
		/// counters, achievement triggers.
		/// </summary>
		public static event Action<Sabine.Zone.World.Actors.PlayerCharacter, Sabine.Zone.World.Actors.Character> PlayerKilled;

		internal static void RaisePlayerKilled(Sabine.Zone.World.Actors.PlayerCharacter player, Sabine.Zone.World.Actors.Character killer)
			=> PlayerKilled?.Invoke(player, killer);

		/// <summary>
		/// Fired after <see cref="Sabine.Zone.World.Actors.Character.TakeDamage"/>
		/// applies damage. Subscribers see the remaining HP and can push
		/// the change to derived UI (mercenary panel, party HP bar, raid
		/// frames, etc.) without having to poll. Attacker may be null
		/// (poison tick, fall damage, environment).
		/// </summary>
		public static event Action<Sabine.Zone.World.Actors.Character, int, Sabine.Zone.World.Actors.Character, int> CharacterDamaged;

		internal static void RaiseCharacterDamaged(Sabine.Zone.World.Actors.Character target, int amount, Sabine.Zone.World.Actors.Character attacker, int remainingHp)
			=> CharacterDamaged?.Invoke(target, amount, attacker, remainingHp);

		/// <summary>
		/// Fired after <see cref="Sabine.Zone.World.Actors.Character.HealHp"/>
		/// applies a heal. Mirrors <see cref="CharacterDamaged"/>; pair
		/// the two when a subscriber needs every HP delta.
		/// </summary>
		public static event Action<Sabine.Zone.World.Actors.Character, int> CharacterHealed;

		internal static void RaiseCharacterHealed(Sabine.Zone.World.Actors.Character target, int amount)
			=> CharacterHealed?.Invoke(target, amount);
	}
}
