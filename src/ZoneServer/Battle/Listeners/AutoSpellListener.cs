using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Sage SA_AUTOSPELL: rolls a chance to auto-cast a stored bolt
	/// spell when the caster lands a melee physical hit. The bolt
	/// id is stored in the AutoSpell status's Val1 (defaulted to
	/// MG_FIREBOLT for v1; the proper choose-spell UI is a follow-up).
	/// </summary>
	public static class AutoSpellListener
	{
		public static void Subscribe()
		{
			CombatEvents.AttackResolved += OnAttackResolved;
		}

		private static void OnAttackResolved(AttackContext ctx, AttackResult result)
		{
			if (result.IsMiss) return;
			if (ctx.Kind != AttackKind.Physical) return;
			if (ctx.SkillId != SkillId.None) return;

			var attacker = ctx.Attacker;
			var target = ctx.Target;
			if (attacker == null || target == null) return;
			if (attacker.StatusEffects == null) return;
			if (!attacker.StatusEffects.TryGet(StatusId.AutoSpell, out var effect)) return;

			var procChance = 5 + 2 * effect.Level;
			if (RandomProvider.Get().Next(100) >= procChance) return;

			var boltId = effect.Val1 != 0 ? (SkillId)effect.Val1 : SkillId.MG_FIREBOLT;
			var element = boltId switch
			{
				SkillId.MG_FIREBOLT => ElementType.Fire,
				SkillId.MG_COLDBOLT => ElementType.Water,
				SkillId.MG_LIGHTNINGBOLT => ElementType.Wind,
				SkillId.WZ_EARTHSPIKE => ElementType.Earth,
				_ => ElementType.Neutral,
			};

			var boltCtx = new AttackContext(attacker, target)
			{
				SkillId = boltId,
				SkillLevel = effect.Level,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f,
				HitCount = effect.Level,
				AttackElement = element,
				AlwaysHits = true,
			};
			var boltResult = BattleCalculator.Calc(boltCtx);
			if (!boltResult.IsMiss)
				target.TakeDamage(boltResult.Damage, attacker);

			Send.ZC_NOTIFY_SKILL(attacker, target.Handle, boltId, effect.Level, boltResult.Damage, 0, effect.Level, ActionType.Skill);
		}
	}
}
