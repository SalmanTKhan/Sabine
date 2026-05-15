using Sabine.Shared.Const;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Paladin PA_SACRIFICE (Martyr's Reckoning): while active, the
	/// caster's basic weapon attacks deal fixed damage equal to 9% of
	/// their MaxHP and cost the same HP. eAthena status.c:5307 caps the
	/// buff at 5 charges (val2) and expires it at zero. Skill hits and
	/// ranged weapons don't consume the buff.
	/// </summary>
	public static class SacrificeListener
	{
		private const int HpPercentPerHit = 9;

		public static void Subscribe()
		{
			CombatEvents.PreAttackResolve += OnPreAttackResolve;
		}

		private static void OnPreAttackResolve(AttackContext ctx)
		{
			if (ctx.Kind != AttackKind.Physical) return;
			if (ctx.SkillId != SkillId.None) return;

			var attacker = ctx.Attacker;
			var target = ctx.Target;
			if (attacker == null || target == null || attacker == target) return;
			if (attacker.StatusEffects == null) return;
			if (!attacker.StatusEffects.TryGet(StatusId.Sacrifice, out var effect)) return;

			var hpCost = attacker.Parameters.HpMax * HpPercentPerHit / 100;
			if (hpCost <= 0) return;

			// If the cost would kill the caster, end the buff and let
			// the normal attack proceed instead.
			if (attacker.Parameters.Hp <= hpCost)
			{
				attacker.StatusEffects.Stop(StatusId.Sacrifice);
				return;
			}

			attacker.TakeDamage(hpCost, attacker);

			ctx.FixedDamage = hpCost;
			ctx.AlwaysHits = true;
			ctx.IgnoreDefense = true;
			ctx.IgnoreFlee = true;

			effect.Val2--;
			if (effect.Val2 <= 0)
				attacker.StatusEffects.Stop(StatusId.Sacrifice);
		}
	}
}
