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
			if (ctx.IsLongRange) return;

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
