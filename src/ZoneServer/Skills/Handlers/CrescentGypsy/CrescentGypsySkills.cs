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
	/// vertical waterfall around the caster. Reuses the SongUnit
	/// pipeline (5×5 footprint with the Moonlit-marker status) so the
	/// performer movement-lock applies the same way as Bard / Dancer
	/// solos. Duration: 20s + 5s/level (skill_cast_db.txt:658).
	/// </summary>
	[SkillHandler(SkillId.CG_MOONLIT)]
	public class MoonlitHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;
			var center = caster.Position;

			for (var dx = -2; dx <= 2; dx++)
			{
				for (var dy = -2; dy <= 2; dy++)
				{
					var pos = new Sabine.Shared.World.Position((short)(center.X + dx), (short)(center.Y + dy));
					caster.Map.AddSkillUnit(new Sabine.Zone.World.Maps.SkillUnits.SongUnit(
						caster, pos, skill.Id, level, StatusId.DancerDance));
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);
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
	/// lock applied while a Bard / Dancer / Clown / Gypsy is performing.
	/// <see cref="Sabine.Zone.World.Actors.Character.IsPerforming"/>
	/// short-circuits when this status is active, so the performer
	/// can stride out of their own song unit. Duration: 180s
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
	/// CG_TAROTCARD (Tarot Card of Fate) — rolls 1 of 14 outcomes on
	/// the target. Success rate is <c>level * 8</c>%; on failure the
	/// cast simply fizzles. Mirrors eAthena skill.c:5253 with these
	/// substitutions where Sabine doesn't expose the eA SC:
	/// SC_INCATKRATE / SC_INCMATKRATE / SC_INCHITRATE / SC_INCFLEERATE
	/// / SC_INCDEFRATE aren't wired, so the stat-debuff cards collapse
	/// to a single-status alternative (Stun / Curse). The 6666-damage
	/// "Star" and 4444-damage "Moon" land verbatim. Card 6 (eA's
	/// "draw 2 more") rolls two extra outcomes inline.
	/// </summary>
	[SkillHandler(SkillId.CG_TAROTCARD)]
	public class TarotCardHandler : ITargetedSkillHandler
	{
		private static readonly TimeSpan EffectDuration = TimeSpan.FromSeconds(30);

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;
			if (target == null) return;

			var rnd = Random.Shared;
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			// Success roll: level * 8% (eA skill.c:5256). Boss / Emperium
			// guards from eA aren't checked here — Sabine doesn't model
			// emperium guardian state on the boss flag yet.
			if (rnd.Next(100) >= level * 8) return;

			var draws = 1;
			while (draws-- > 0)
			{
				switch (rnd.Next(14))
				{
					case 0:  // Fool — drain target SP to 0.
						target.TrySpendSp(target.Parameters.Sp);
						break;
					case 1:  // Lovers — eA halves MATK; Sabine subs in Curse.
						target.StatusEffects.Start(StatusId.Curse, level, EffectDuration, caster);
						break;
					case 2:  // Empress — strip buffs (matches Dispel set).
						foreach (var sid in DispelTargets)
							if (target.StatusEffects.Has(sid))
								target.StatusEffects.Stop(sid);
						break;
					case 3:  // Hanged Man — 1000 damage + random armor break.
					         // CR_FULLPROTECTION shields the break.
						target.TakeDamage(1000, caster);
						if (!target.IsDead && target is PlayerCharacter ptarget
							&& ptarget.StatusEffects?.Has(StatusId.FullProtection) != true)
						{
							Sabine.Zone.Battle.Listeners.EquipBreakListener.BreakRandomEquip(
								ptarget,
								Sabine.Zone.Battle.Listeners.EquipBreakListener.ArmorSlots);
						}
						break;
					case 4:  // Strength — eA halves ATK; subs in Stun (5s).
						target.StatusEffects.Start(StatusId.Stun, level, TimeSpan.FromSeconds(5), caster);
						break;
					case 5:  // Sun — heal caster 2000 HP. Teleport leg deferred.
						caster.HealHp(2000);
						break;
					case 6:  // Hermit — draw two extra outcomes (eA "count=3" path).
						draws += 2;
						break;
					case 7:  // Wheel of Fortune — eA random Stop / Freeze / Stone.
						{
							var pick = rnd.Next(3);
							var sid = pick switch
							{
								0 => StatusId.Stop,
								1 => StatusId.Freeze,
								_ => StatusId.Stone,
							};
							target.StatusEffects.Start(sid, level, EffectDuration, caster);
						}
						break;
					case 8:  // Devil — Curse + Poison. eA also adds Coma; Sabine
					         // has no Coma status, so we skip it.
						target.StatusEffects.Start(StatusId.Curse, level, EffectDuration, caster);
						target.StatusEffects.Start(StatusId.Poison, level, EffectDuration, caster);
						break;
					case 9:  // Tower — Confusion.
						target.StatusEffects.Start(StatusId.Confusion, level, EffectDuration, caster);
						break;
					case 10: // Star — 6666 fixed damage + Curse (eA's stat halves
					         // collapse to Curse).
						target.TakeDamage(6666, caster);
						if (!target.IsDead)
							target.StatusEffects.Start(StatusId.Curse, level, EffectDuration, caster);
						break;
					case 11: // Moon — 4444 fixed damage.
						target.TakeDamage(4444, caster);
						break;
					case 12: // Emperor — 5s Stun (eA verbatim).
						target.StatusEffects.Start(StatusId.Stun, level, TimeSpan.FromSeconds(5), caster);
						break;
					case 13: // World — eA's all-stat -20% combo collapses to Sleep,
					         // since none of the rate buckets are wired today.
						target.StatusEffects.Start(StatusId.Sleep, level, EffectDuration, caster);
						break;
				}
				if (target.IsDead) break;
			}
		}

		// Same buff set SA_DISPELL strips. Kept narrow on purpose — the
		// "world" / dispel card only knocks off offensive buffs, not the
		// defensive shields it'd be unfair to wipe in a tarot roll.
		private static readonly StatusId[] DispelTargets = new[]
		{
			StatusId.Blessing, StatusId.IncreaseAgi, StatusId.Angelus,
			StatusId.Concentration, StatusId.TwoHandQuicken,
			StatusId.SpearQuicken, StatusId.AdrenalineRush,
			StatusId.OverThrust, StatusId.MaxOverThrust,
			StatusId.WeaponPerfection, StatusId.PowerMaximize,
		};
	}
}
