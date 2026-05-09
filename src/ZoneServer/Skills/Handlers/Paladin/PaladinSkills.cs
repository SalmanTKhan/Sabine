using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Paladin
{
	/// <summary>
	/// PA_PRESSURE (Pressure) — flat damage that bypasses DEF, MDEF, and
	/// every status reduction. eAthena battle.c:2761 sets the damage to
	/// <c>500 + 300*level</c> as <c>BF_MISC</c>; battle.c:309-310 returns
	/// it untouched from the post-damage pipeline; skill.c:751 drains
	/// (15 + 5*level)% of the target's MaxSP. AlwaysHits + the existing
	/// <c>SkillId == PA_PRESSURE</c> guard in
	/// <see cref="BattleCalculator.ApplyOverThrust"/> already line up
	/// with the bypass semantics.
	/// </summary>
	[SkillHandler(SkillId.PA_PRESSURE)]
	public class PressureHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var damage = 500 + 300 * level;
			target.TakeDamage(damage, caster);

			var spDrainPct = 15 + 5 * level;
			var spDrain = target.Parameters.SpMax * spDrainPct / 100;
			if (spDrain > 0)
				target.TrySpendSp(Math.Min(spDrain, target.Parameters.Sp));

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 1, ActionType.Skill);
		}
	}

	/// <summary>
	/// PA_SHIELDCHAIN (Shield Chain) — physical attack that ignores the
	/// equipped weapon (eAthena battle.c:1043 sets <c>flag.weapon = 0</c>)
	/// and adds <c>30%*level</c> on top of the base ATK ratio
	/// (battle.c:1631). The full eAthena formula also folds in the
	/// equipped shield's weight (<c>weight/10</c> ATK_ADD at
	/// battle.c:1354), but Sabine's BattleCalculator doesn't expose a
	/// shield-weight read on <see cref="AttackContext"/> yet, so the
	/// shield-weight leg is deferred. A doc-comment marks the spot for
	/// when the equip read lands.
	/// </summary>
	[SkillHandler(SkillId.PA_SHIELDCHAIN)]
	public class ShieldChainHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.3f * level,
				WeaponRequired = false,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, result.HitCount, result.ActionType);
		}
	}

	/// <summary>
	/// PA_SACRIFICE (Martyr's Reckoning) — buff that redirects incoming
	/// damage from the linked target to the caster, consuming a charge
	/// per hit. eAthena status.c:5307 stores <c>val2 = 5</c> (hits) and
	/// <c>tick = -1</c> (no timed expiry). Marker today: the redirect
	/// listener lives on the on-hit pipeline — once that listener lands,
	/// it can decrement val2 and end the buff at zero, mirroring Kyrie's
	/// hit-count consume pattern. Duration: 5 hits / no time cap.
	/// </summary>
	[SkillHandler(SkillId.PA_SACRIFICE)]
	public class SacrificeHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.Sacrifice;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromMinutes(10);
	}

	/// <summary>
	/// PA_GOSPEL (Battle Chant) — toggle aura that randomly grants
	/// party-wide buffs or inflicts enemy-side debuffs every 10s.
	/// eAthena status.c:5660 sets <c>tick = 10000</c> and <c>val2 =
	/// total_ticks</c>; the random-roll table lives in skill.c.
	/// Marker today — Sabine's status pipeline doesn't have a per-tick
	/// random-roll dispatcher, and grafting the table on warrants its
	/// own commit. The base buff still applies, the rolls just don't
	/// fire. Duration: 60s flat (skill_cast_db.txt:593).
	/// </summary>
	[SkillHandler(SkillId.PA_GOSPEL)]
	public class GospelHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.Gospel;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60);
	}
}
