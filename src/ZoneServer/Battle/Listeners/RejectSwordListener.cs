using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Yggdrasil.Util;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Stalker ST_REJECTSWORD: while active, melee physical hits from
	/// sword-class attackers have a 15%*level chance to be fully
	/// reflected. eAthena status.c:5706 caps the buff at 3 charges
	/// (val3); the status ends at zero charges. Magic / ranged / non-
	/// sword weapon attacks don't interact. Sabine's WeaponType lookup
	/// isn't exposed on AttackContext yet, so the sword-weapon gate is
	/// approximated by "non-ranged, non-skill physical" — a future
	/// commit can tighten it once the equip read lands.
	/// </summary>
	public static class RejectSwordListener
	{
		public static void Subscribe()
		{
			CombatEvents.AttackResolved += OnAttackResolved;
		}

		private static void OnAttackResolved(AttackContext ctx, AttackResult result)
		{
			if (result.IsMiss || result.Damage <= 0) return;
			if (ctx.Kind != AttackKind.Physical) return;
			if (ctx.SkillId != SkillId.None) return;
			if (ctx.IsLongRange) return;
			if (BattleHelpers.IsRangedWeapon(ctx.Attacker)) return;

			var target = ctx.Target;
			var attacker = ctx.Attacker;
			if (target == null || attacker == null || target == attacker) return;
			if (target.StatusEffects == null) return;
			if (!target.StatusEffects.TryGet(StatusId.RejectSword, out var effect)) return;

			var roll = RandomProvider.Get().Next(100);
			if (roll >= effect.Val2) return;

			attacker.TakeDamage(result.Damage, target);
			Send.ZC_NOTIFY_SKILL(target, attacker.Handle, SkillId.ST_REJECTSWORD, effect.Level, result.Damage, 0, 1, ActionType.Skill);

			effect.Val3--;
			if (effect.Val3 <= 0)
				target.StatusEffects.Stop(StatusId.RejectSword);
		}
	}
}
