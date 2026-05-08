using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Lifesteal on basic-attack hits — heals the attacker for a flat
	/// <see cref="BonusModifiers.BonusHpDrainValue"/> plus a percent of
	/// damage rolled against
	/// <see cref="BonusModifiers.BonusHpDrainRate"/> (per 1000), and the
	/// SP equivalent. Mirrors eAthena <c>battle.c:battle_drain</c> and
	/// <c>battle_calc_drain</c>: the flat value is unconditional, the
	/// percent contribution is gated on the per-1000 rate roll. Skill
	/// hits don't proc — eAthena sets the drain hooks only on weapon
	/// attack frames (battle_weapon_attack), and we mirror that by
	/// gating on <see cref="AttackContext.SkillId"/>.
	/// </summary>
	public static class DrainListener
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
			if (attacker is not PlayerCharacter pc) return;

			var target = ctx.Target;
			if (target == null || target == attacker || target.IsDead) return;

			var mods = attacker.Modifiers;
			var rnd = RandomProvider.Get();

			var hpHeal = mods.BonusHpDrainValue;
			if (mods.BonusHpDrainRate > 0 && rnd.Next(1000) < mods.BonusHpDrainRate)
				hpHeal += result.Damage * mods.BonusHpDrainPercent / 100;

			var spHeal = mods.BonusSpDrainValue;
			if (mods.BonusSpDrainRate > 0 && rnd.Next(1000) < mods.BonusSpDrainRate)
				spHeal += result.Damage * mods.BonusSpDrainPercent / 100;

			if (hpHeal > 0)
				pc.HealHp(hpHeal);
			if (spHeal > 0)
				pc.HealSp(spHeal);
		}
	}
}
