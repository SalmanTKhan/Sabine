using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.HighWizard
{
	/// <summary>
	/// HW_SOULDRAIN — passive that bumps the wizard's MaxSP by 2% per
	/// level and grants a small SP refund on kills. The MaxSP leg is
	/// already wired via <see cref="HW_SOULDRAIN"/> in
	/// <see cref="Sabine.Zone.World.Actors.Components.Characters.PlayerCharacterParameters.RecalculateSp"/>;
	/// the SP-on-kill arm needs a kill-listener hook on the on-hit
	/// pipeline, which Sabine doesn't have yet, so it stays deferred.
	/// Pure passive — no handler attribute, no status effect; this
	/// stub exists for cross-reference from the recalc and to give
	/// future commits a place to graft on the kill listener.
	/// </summary>
	internal static class SoulDrainPassive { }

	/// <summary>
	/// HW_MAGICCRASHER — single-target physical hit that uses MATK as
	/// its damage source. eAthena battle.c:2529 lists it under the
	/// BF_WEAPON skill_attack branch. Sabine routes it as a magic
	/// attack with a flat 1.0 ratio so the existing magic damage
	/// pipeline (MATK source, INT scaling) handles the math; the
	/// "uses physical hit type" detail is cosmetic and matches what
	/// the alpha client renders today. No element override — the
	/// caster's weapon element rolls (default Neutral).
	/// </summary>
	[SkillHandler(SkillId.HW_MAGICCRASHER)]
	public class MagicCrasherHandler : ITargetedSkillHandler
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
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	/// <summary>
	/// HW_NAPALMVULCAN — targeted Ghost-element magic damage with a
	/// per-cast Curse roll. eAthena battle.c:2479 sets
	/// <c>skillratio += 10*level - 30</c> → multiplier
	/// <c>0.7 + 0.1*level</c>. eAthena skill.c:872 fires
	/// <c>sc_start(SC_CURSE, 5*level, ...)</c> on the target after the
	/// hit lands, with the duration coming from <c>skill_get_time2</c>.
	/// Curse already has a status handler (DebuffStatusHandlers) so the
	/// roll can land inline; the duration follows the eA classic
	/// skill_cast_db.txt:670 (45000ms / 45s).
	/// </summary>
	[SkillHandler(SkillId.HW_NAPALMVULCAN)]
	public class NapalmVulcanHandler : ITargetedSkillHandler
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
				Kind = AttackKind.Magic,
				SkillRatio = 0.7f + 0.1f * level,
				AttackElement = ElementType.Ghost,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				target.TakeDamage(result.Damage, caster);

				var rnd = Yggdrasil.Util.RandomProvider.Get();
				if (rnd.Next(100) < 5 * level)
					target.StatusEffects.Start(StatusId.Curse, level, TimeSpan.FromSeconds(45), caster);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	/// <summary>
	/// HW_GANBANTEIN — single-cell ground skill that wipes existing
	/// SkillUnits in a small radius (e.g. Pneuma, Safety Wall, Sanctuary,
	/// Magnus, Basilica). eAthena skill.c:6423 rolls 80% to succeed and
	/// then dispatches <c>skill_cell_overlap</c> across the splash. Here
	/// we mirror via <see cref="Sabine.Zone.World.Maps.Map.GetSkillUnitsInRange"/>
	/// + <c>RemoveSkillUnit</c>; failure is silent — the alpha client
	/// has no skill-fail packet plumbed through this path yet.
	/// </summary>
	[SkillHandler(SkillId.HW_GANBANTEIN)]
	public class GanbanteinHandler : IGroundSkillHandler
	{
		private const int Splash = 1;
		private const int SuccessRate = 80;

		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var center = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);

			if (Random.Shared.Next(100) >= SuccessRate)
				return;

			var units = caster.Map.GetSkillUnitsInRange(center, Splash);
			foreach (var unit in units)
				caster.Map.RemoveSkillUnit(unit);
		}
	}

	/// <summary>
	/// HW_GRAVITATION — ground field that locks enemies in place and
	/// hits them for fixed damage every second. eAthena skill.c:6435
	/// spawns a single skill_unit_group at the target cell and binds
	/// the caster with SC_GRAVITATION; battle.c:2796 sets the per-tick
	/// damage as <c>200 + 200*level</c>. Sabine spawns a 3×3 grid of
	/// <see cref="GravitationUnit"/> cells (no eA splash radius value
	/// handy on the alpha cast db, picked 1 to match Magnus / Sanctuary
	/// shapes); the caster-side movement lock is deferred — Sabine has
	/// no "caster pinned" flag on the status pipeline yet.
	/// Duration: 5s + 1s/level (skill_cast_db.txt:815).
	/// </summary>
	[SkillHandler(SkillId.HW_GRAVITATION)]
	public class GravitationHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var center = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;
			var duration = TimeSpan.FromSeconds(5 + level);

			for (var dx = -1; dx <= 1; dx++)
			{
				for (var dy = -1; dy <= 1; dy++)
				{
					var pos = new Position((short)(center.X + dx), (short)(center.Y + dy));
					caster.Map.AddSkillUnit(new GravitationUnit(caster, pos, level, duration));
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);
		}
	}
}
