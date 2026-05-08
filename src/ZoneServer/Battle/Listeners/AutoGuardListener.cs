using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Yggdrasil.Util;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Rolls a chance to fully block incoming physical hits when the
	/// target has the AutoGuard buff. Block rate = 5% per level.
	/// Mirrors eAthena CR_AUTOGUARD.
	/// </summary>
	public static class AutoGuardListener
	{
		public static void Subscribe()
		{
			CombatEvents.PreAttackResolve += OnPreAttackResolve;
		}

		private static void OnPreAttackResolve(AttackContext ctx)
		{
			if (ctx.Kind != AttackKind.Physical) return;
			var target = ctx.Target;
			if (target == null || target.StatusEffects == null) return;
			if (!target.StatusEffects.TryGet(StatusId.AutoGuard, out var effect)) return;

			// Skills flagged IgnoreAutoGuard pierce the buff entirely
			// (e.g., MO_EXTREMITYFIST, ASC_BREAKER). Mirrors eAthena's
			// is_attack_piercing / IgnoreAutoGuard skill_db check.
			if (ctx.SkillId != SkillId.None
				&& ZoneServer.Instance.Data.Skills.TryFind(ctx.SkillId, out var skillData)
				&& skillData.PropertyFlags.HasFlag(SkillPropertyFlags.IgnoreAutoGuard))
				return;

			var blockChance = 5 * effect.Level;
			if (RandomProvider.Get().Next(100) < blockChance)
				ctx.Cancelled = true;
		}
	}
}
