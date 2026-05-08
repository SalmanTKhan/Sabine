using Sabine.Shared.Const;
using Sabine.Zone.Skills.Handlers.Hunter;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Auto-Blitz Beat: a Hunter wielding a bow with an active Falcon
	/// auto-fires Blitz Beat on every basic-attack hit. Mirrors eAthena
	/// skill.c:585 — chance = LUK*10/3+1 per 1000, cast level capped at
	/// min(blitz_level, (job_level+9)/10) so low-job-level Hunters can't
	/// proc the highest tier.
	/// </summary>
	public static class AutoBlitzListener
	{
		public static void Subscribe()
		{
			CombatEvents.AttackResolved += OnAttackResolved;
		}

		private static void OnAttackResolved(AttackContext ctx, AttackResult result)
		{
			if (ctx.SkillId != SkillId.None) return;
			if (ctx.Kind != AttackKind.Physical) return;
			if (result.IsMiss || result.Damage <= 0) return;

			var attacker = ctx.Attacker;
			var target = ctx.Target;
			if (attacker is not PlayerCharacter pc || target == null || target.IsDead) return;
			if (attacker == target) return;

			if (attacker.StatusEffects?.Has(StatusId.FalconFly) != true) return;
			if (!BattleHelpers.IsRangedWeapon(attacker)) return;

			var blitzLevel = pc.Skills.GetLevel(SkillId.HT_BLITZBEAT);
			if (blitzLevel <= 0) return;

			var luk = attacker.Parameters.Luk;
			var chancePer1000 = luk * 10 / 3 + 1;
			if (RandomProvider.Get().Next(1000) >= chancePer1000)
				return;

			var jobCap = (attacker.Parameters.JobLevel + 9) / 10;
			var castLevel = System.Math.Min(blitzLevel, System.Math.Max(1, jobCap));

			BlitzBeatHandler.Cast(attacker, target, castLevel, autocast: true);
		}
	}
}
