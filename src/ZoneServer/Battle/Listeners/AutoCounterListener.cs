using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Knight KN_AUTOCOUNTER: while the 1-second window is open, the
	/// next melee physical hit against the bearer triggers an immediate
	/// guaranteed-crit counter swing on the attacker. The buff is then
	/// consumed. Skill hits and ranged attacks do not trigger it.
	/// </summary>
	public static class AutoCounterListener
	{
		public static void Subscribe()
		{
			CombatEvents.AttackResolved += OnAttackResolved;
		}

		private static void OnAttackResolved(AttackContext ctx, AttackResult result)
		{
			if (ctx.Kind != AttackKind.Physical) return;
			if (ctx.SkillId != SkillId.None) return;
			if (ctx.IsLongRange) return;
			if (BattleHelpers.IsRangedWeapon(ctx.Attacker)) return;

			var target = ctx.Target;
			var attacker = ctx.Attacker;
			if (target == null || attacker == null || target == attacker) return;
			if (target.IsDead) return;
			if (target.StatusEffects == null) return;
			if (!target.StatusEffects.TryGet(StatusId.AutoCounter, out var effect)) return;

			// Consume the window first so the counter swing's own
			// AttackResolved doesn't recurse into this listener.
			target.StatusEffects.Stop(StatusId.AutoCounter);

			var counterCtx = new AttackContext(target, attacker)
			{
				ForceCrit = true,
				AlwaysHits = true,
			};
			var counterResult = BattleCalculator.Calc(counterCtx);

			if (!counterResult.IsMiss && counterResult.Damage > 0)
				attacker.TakeDamage(counterResult.Damage, target);

			Send.ZC_NOTIFY_SKILL(target, attacker.Handle, SkillId.KN_AUTOCOUNTER, effect.Level, counterResult.Damage, 0, 1, ActionType.Skill);
		}
	}
}
