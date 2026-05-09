using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.CrescentGypsy
{
	/// <summary>
	/// CG_ARROWVULCAN (Vulcan Arrow) — multi-hit ranged physical with
	/// a big skill ratio. eAthena battle.c:1616 sets
	/// <c>skillratio += 100 + 100*level</c>, so the multiplier is
	/// <c>2.0 + 1.0*level</c>. Hit count is fixed at 9 per the alpha
	/// classic profile. Bow requirement is enforced upstream by the
	/// AC_OWL prerequisite chain.
	/// </summary>
	[SkillHandler(SkillId.CG_ARROWVULCAN)]
	public class ArrowVulcanHandler : ITargetedSkillHandler
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
				SkillRatio = 2.0f + 1.0f * level,
				HitCount = 9,
				IsLongRange = true,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, result.HitCount, result.ActionType);
		}
	}

	/// <summary>
	/// CG_MOONLIT (Sphere of Moonlight) — toggle aura that drops a
	/// vertical waterfall around the caster. Marker today: Sabine's
	/// performer-toggle pipeline (Bard/Dancer ensemble framework)
	/// doesn't have a one-side aura unit yet; the visual will play
	/// from the cast packet, but the per-tile water sphere doesn't
	/// spawn. Duration: 20s + 5s/level (skill_cast_db.txt:658).
	/// </summary>
	[SkillHandler(SkillId.CG_MOONLIT)]
	public class MoonlitHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	/// <summary>
	/// CG_MARIONETTE (Marionette Control) — paired buff between two
	/// performers; eAthena status.c transfers half of each side's
	/// primary stats to the other. Marker today — the stat-transfer
	/// arm depends on a "linked partner" status pair Sabine doesn't
	/// model yet, and the cast-fail check (no-self, partner must
	/// also be a performer) lives in the cast pipeline. The status
	/// flag still applies so future linkage can read it. Duration:
	/// 60s flat (cast db: 1000ms cooldown only; full SC duration
	/// inferred from eAthena classic skill_db).
	/// </summary>
	[SkillHandler(SkillId.CG_MARIONETTE)]
	public class MarionetteHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.Marionette;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60);
	}

	/// <summary>
	/// CG_LONGINGFREEDOM (Longing for Freedom) — cancels the movement
	/// lock applied by an active Bard/Dancer ensemble (Apple of Idun
	/// etc.) so the performer can move while the song plays. Marker
	/// today — Sabine doesn't model the ensemble movement lock yet,
	/// so there's nothing to release; the marker still lands so the
	/// future ensemble system can read it. Duration: 180s
	/// (skill_cast_db.txt:821).
	/// </summary>
	[SkillHandler(SkillId.CG_LONGINGFREEDOM)]
	public class LongingFreedomHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.LongingFreedom;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(180);
	}

	/// <summary>
	/// CG_HERMODE (Wand of Hermode) — toggle aura that grants the
	/// performer's party immunity to dispels. Marker today — Sabine
	/// doesn't have a "dispel-immune" guard wired into status removal
	/// logic. Duration: 10s + 5s/level (skill_cast_db.txt:823).
	/// </summary>
	[SkillHandler(SkillId.CG_HERMODE)]
	public class HermodeHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.Hermode;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(10 + 5 * level);
	}

	/// <summary>
	/// CG_TAROTCARD (Tarot Card of Fate) — rolls one of 14 random
	/// debuffs on the target (heal, drain, stat shift, status inflict,
	/// or instant-death at low odds). Marker today — Sabine's per-skill
	/// status-inflict dispatcher isn't built, and the 14-card random
	/// table is non-trivial to graft on cleanly. The cast plays its
	/// animation and ends; nothing applies to the target. The handler
	/// shape is left as a damage-skill stub so the dispatcher can land
	/// later without changing the wire format.
	/// </summary>
	[SkillHandler(SkillId.CG_TAROTCARD)]
	public class TarotCardHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;
			if (target == null) return;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}
}
