using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Reflects a percentage of incoming physical damage back to the
	/// attacker when the target has the ReflectShield buff active.
	/// Mirrors eAthena CR_REFLECTSHIELD: 10% + 3%*level returned.
	/// Magic, status, and ranged-bow hits are not reflected.
	/// </summary>
	public static class ReflectShieldListener
	{
		public static void Subscribe()
		{
			CombatEvents.AttackResolved += OnAttackResolved;
		}

		private static void OnAttackResolved(AttackContext ctx, AttackResult result)
		{
			if (result.IsMiss || result.Damage <= 0) return;
			if (ctx.Kind != AttackKind.Physical) return;

			// eAthena reflects BF_WEAPON|BF_SHORT but allows
			// short-range skills (Bowling Bash, Magnum Break) to be
			// reflected even though they may carry BF_LONG. The real
			// exclusion is bow/firearm hits, so check the weapon
			// rather than the skill's range flag.
			if (BattleHelpers.IsRangedWeapon(ctx.Attacker)) return;

			var target = ctx.Target;
			var attacker = ctx.Attacker;
			if (target == null || attacker == null || target == attacker) return;
			if (target.StatusEffects == null) return;
			if (!target.StatusEffects.TryGet(StatusId.ReflectShield, out var effect)) return;

			var pct = 10 + 3 * effect.Level;
			var reflected = result.Damage * pct / 100;
			if (reflected <= 0) return;

			attacker.TakeDamage(reflected, target);
			Send.ZC_NOTIFY_SKILL(target, attacker.Handle, SkillId.CR_REFLECTSHIELD, effect.Level, reflected, 0, 1, ActionType.Skill);
		}
	}
}
