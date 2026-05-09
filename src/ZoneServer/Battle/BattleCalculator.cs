using System;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Zone.Network;
using Sabine.Zone.Skills.StatusEffects;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Centralised damage calculator. Mirrors eAthena's
	/// <c>battle_calc_attack</c> (pre-renewal) and rAthena's
	/// <c>battle_calc_attack</c> (renewal) at a coarse level.
	/// </summary>
	/// <remarks>
	/// Selects pre-renewal vs renewal via the
	/// <see cref="FeatureId.Renewal"/> flag in <c>features.txt</c>.
	///
	/// Implemented in v2: weapon element from <see cref="ItemData.Element"/>,
	/// 10×4×10 element table with defense-element level scaling, weapon
	/// vs target size penalty, weapon refinement (already baked into
	/// the parameter cache), per-character race / element / size bonus
	/// hooks, status-ATK / status-MATK split when renewal is enabled,
	/// renewal magic flee, and a stub for ground-effect blockers
	/// (Pneuma / Safety Wall).
	/// </remarks>
	public static class BattleCalculator
	{
		public static AttackResult Calc(AttackContext ctx)
		{
			CombatEvents.RaisePreAttackResolve(ctx);

			if (ctx.Cancelled || ctx.Target == null)
			{
				var miss = AttackResult.Miss();
				CombatEvents.RaiseAttackResolved(ctx, miss);
				return miss;
			}

			var result = ctx.Kind == AttackKind.Magic
				? CalcMagic(ctx)
				: CalcPhysical(ctx);

			CombatEvents.RaiseAttackResolved(ctx, result);
			return result;
		}

		/// <summary>
		/// True when the Renewal feature flag is enabled. Skill handlers
		/// branch their formulas on this; pre-renewal is the default.
		/// </summary>
		internal static bool IsRenewal()
			=> ZoneServer.Instance.Data.Features.IsEnabled(FeatureId.Renewal);

		// -----------------------------------------------------------------
		// Physical
		// -----------------------------------------------------------------

		private static AttackResult CalcPhysical(AttackContext ctx)
		{
			var attacker = ctx.Attacker;
			var target = ctx.Target;
			var rnd = RandomProvider.Get();

			var attackElement = ResolveAttackElement(attacker, ctx);
			var defenseElement = ResolveDefenseElement(target);
			var defenseLevel = ResolveDefenseElementLevel(target);

			var result = new AttackResult
			{
				HitCount = Math.Max(1, ctx.HitCount),
				ActionType = ctx.SkillId == SkillId.None ? ActionType.Attack : ActionType.Skill,
				AttackElement = attackElement,
				TargetElement = defenseElement,
			};

			var isRanged = IsRangedWeapon(attacker);
			if (GroundEffectGuard.IsBlocked(ctx, isRanged))
			{
				result.IsMiss = true;
				return result;
			}

			if (!ctx.AlwaysHits)
			{
				var perfectDodge = Math.Max(0, target.Parameters.Flee2);
				if (perfectDodge > 0 && rnd.Next(1000) < perfectDodge)
				{
					result.IsLuckyDodge = true;
					result.IsMiss = true;
					return result;
				}

				if (ctx.IgnoreFlee)
					goto hitConfirmed;

				var atkHit = attacker.Parameters.Hit;
				var defFlee = target.Parameters.Flee;

				// Blind: -25% hit on a blinded attacker, -25% flee on a
				// blinded target. Mirrors eAthena status_calc_hit/_flee.
				if (attacker.StatusEffects?.Has(StatusId.Blind) == true)
					atkHit = atkHit * 75 / 100;
				if (target.StatusEffects?.Has(StatusId.Blind) == true)
					defFlee = defFlee * 75 / 100;

				var hitChance = Math.Max(5, 80 + atkHit - defFlee);
				if (rnd.Next(100) >= hitChance)
				{
					result.IsMiss = true;
					return result;
				}
			}
			hitConfirmed:

			// SC_PARRYING (LK_PARRYING) — two-handed sword melee block.
			// eAthena classic battle.c: chance = 20 + 3*level%, applies
			// only to weapon attacks (BF_WEAPON, non-skill or weapon
			// skills) and only when the target wields a two-handed
			// sword. A successful parry zeroes the hit entirely;
			// modeled here as IsMiss so the rest of the pipeline
			// short-circuits without surfacing a "Parry" packet — the
			// alpha / beta1 client doesn't render a parry frame.
			if (TryParryingBlock(target, isRanged, ctx, rnd))
			{
				result.IsMiss = true;
				return result;
			}

			var renewal = IsRenewal();
			int baseDamage;

			// Race-mastery flat ATK bonus from passive skills against
			// matching enemy types (DemonBane vs Demon/Undead, BeastBane
			// vs Brute/Insect). eAthena battle.c:719-729 stacks these into
			// the base damage *before* skill ratio is applied.
			var raceMastery = GetRaceMasteryBonus(attacker, target);

			// SC_AURABLADE (LK_AURABLADE) adds a flat level*10 ATK to
			// every physical hit while the buff is active, applied in
			// the same pre-skill-ratio band as race mastery. Mirrors
			// eAthena battle.c:battle_calc_misc_attack — `dmg += 20*level`
			// on the rhw branch (some forks use 10/20 depending on era;
			// classic = 10*level).
			raceMastery += GetAuraBladeBonus(attacker);

			if (renewal)
			{
				// Renewal: WeaponATK * skillRatio + StatusATK + MasteryATK.
				var weaponAtk = attacker.Parameters.WeaponAtk;
				if (weaponAtk == 0) weaponAtk = attacker.Parameters.Attack;

				var statusAtk = attacker.Parameters.StatusAtk;
				var masteryAtk = attacker.Parameters.MasteryAtk + raceMastery;
				baseDamage = (int)Math.Floor(weaponAtk * ctx.SkillRatio) + statusAtk + masteryAtk;
			}
			else
			{
				// Pre-renewal: roll [AttackMin..AttackMax] (already
				// includes refinement and mastery) then apply ratio.
				// A small ±5% variance roll is applied on top to mirror
				// eAthena's WATK randomisation.
				int rolledAtk;
				if (attacker.StatusEffects?.Has(StatusId.PowerMaximize) == true)
				{
					// SC_MAXIMIZEPOWER (BS_MAXIMIZE): force max roll and skip
					// the variance pass. Mirrors eAthena battle.c:846 which
					// sets atkmin = atkmax for the duration of the buff.
					rolledAtk = attacker.Parameters.AttackMax;
				}
				else
				{
					rolledAtk = rnd.Next(attacker.Parameters.AttackMin, attacker.Parameters.AttackMax + 1);
					var variancePct = 95 + rnd.Next(11); // 95..105
					rolledAtk = rolledAtk * variancePct / 100;
				}
				rolledAtk += raceMastery;
				baseDamage = (int)Math.Floor(rolledAtk * ctx.SkillRatio);
			}

			// Critical: ignores DEF, deals max-roll damage. Skills don't
			// crit by default.
			var critChance = Math.Max(0, attacker.Parameters.Critical);
			if (critChance > 0 && rnd.Next(100) < critChance && ctx.SkillId == SkillId.None)
			{
				result.IsCritical = true;
				baseDamage = (int)Math.Floor(attacker.Parameters.AttackMax * ctx.SkillRatio);
			}

			// Size penalty (weapon vs target size).
			baseDamage = ApplySizePenalty(baseDamage, attacker, target);

			var afterDef = result.IsCritical || ctx.IgnoreDefense
				? baseDamage
				: ApplyPhysicalDefense(baseDamage, target, ctx.DefRatio);

			var afterElement = ApplyElement(afterDef, attacker, attackElement, defenseElement, defenseLevel);
			var afterBonuses = ApplyBonusModifiers(afterElement, attacker, target, attackElement);
			var afterOverThrust = ApplyOverThrust(afterBonuses, attacker, ctx);
			var afterAtkRate = ApplyAtkRate(afterOverThrust, attacker, ctx.IsLongRange || isRanged);
			var afterStatus = ApplyStatusMultipliers(afterAtkRate, target);
			var afterDefender = ApplyDefenderReduction(afterStatus, target, ctx.IsLongRange || isRanged);
			var afterEnergyCoat = ApplyEnergyCoatReduction(afterDefender, target);
			var afterAssumptio = ApplyAssumptioReduction(afterEnergyCoat, target);
			var afterLexAeterna = ApplyLexAeterna(afterAssumptio, target);
			var afterKyrie = ApplyKyrieEleison(afterLexAeterna, target, isWeaponHit: true);

			// Kyrie may legitimately drop damage to 0 — skip the +1 floor in
			// that case so the shield can fully absorb the hit.
			result.Damage = afterKyrie == 0
				? 0
				: Math.Max(ctx.MinDamage > 0 ? ctx.MinDamage : 1, afterKyrie);
			result.DamagePerHit = (int)Math.Ceiling(result.Damage / (float)result.HitCount);
			result.Damage = result.DamagePerHit * result.HitCount;

			TryProcBleeding(ctx, result, rnd);

			return result;
		}

		// SC_DEFENDER (CR_DEFENDER): long-range physical damage on the
		// target is reduced by val2 (5 + 15*level)%. Mirrors eAthena
		// battle.c:424 — only applied to BF_LONG|BF_WEAPON hits.
		private static int ApplyDefenderReduction(int damage, Character target, bool isLongRange)
		{
			if (!isLongRange || target.StatusEffects == null)
				return damage;

			var defender = target.StatusEffects.Get(StatusId.Defender);
			if (defender == null || defender.Val2 <= 0)
				return damage;

			var reduction = Math.Clamp(defender.Val2, 0, 100);
			return damage * (100 - reduction) / 100;
		}

		// SC_PARRYING (LK_PARRYING): two-handed sword block roll on
		// melee weapon hits. eAthena classic chance = 20 + 3*level%.
		// Returns true when the parry succeeds and the caller should
		// drop the hit; returns false otherwise. Skill hits are
		// allowed to pass through — only basic-attack and
		// weapon-class skills should ever be blocked, which Sabine
		// approximates today by gating on `ctx.SkillId == None`.
		private static bool TryParryingBlock(Character target, bool attackerIsRanged, AttackContext ctx, Random rnd)
		{
			if (attackerIsRanged) return false;
			if (ctx.SkillId != SkillId.None) return false;
			if (target.StatusEffects == null) return false;

			var effect = target.StatusEffects.Get(StatusId.Parrying);
			if (effect == null) return false;

			if (BattleHelpers.GetWeaponType(target) != WeaponType.TwoHandedSword)
				return false;

			var chance = 20 + 3 * effect.Level;
			return rnd.Next(100) < chance;
		}

		// SC_AURABLADE (LK_AURABLADE): adds level*10 flat ATK to every
		// physical hit while the buff is active. eAthena classic
		// stacks it on top of base damage in the pre-skill-ratio band
		// (the same band as race-mastery), so we surface it from
		// CalcPhysical via raceMastery accumulation.
		private static int GetAuraBladeBonus(Character attacker)
		{
			if (attacker.StatusEffects == null)
				return 0;

			var effect = attacker.StatusEffects.Get(StatusId.AuraBlade);
			if (effect == null)
				return 0;

			return effect.Level * 10;
		}

		// SC_ASSUMPTIO (HP_ASSUMPTIO): halves physical damage taken
		// on the target. Mirrors eAthena status.c — the reduction
		// lives on the BL_PC defense path. Placed after EnergyCoat
		// (which can drain the SC entirely on overflow) and before
		// LexAeterna so the 2x doubling still amplifies what's left.
		private static int ApplyAssumptioReduction(int damage, Character target)
		{
			if (target.StatusEffects?.Has(StatusId.Assumptio) != true)
				return damage;

			return damage / 2;
		}

		// SC_ENERGYCOAT (MG_ENERGYCOAT): physical-weapon damage taken
		// is reduced 6%/12%/18%/24%/30% based on current SP%, charging
		// 1%/1.5%/2%/2.5%/3% MaxSP per hit. Mirrors eAthena
		// battle.c:460 — per = 100*sp/maxsp - 1, bucketed by 20%, so
		// 100% SP gives the highest 30% reduction. The SC ends if SP
		// can't cover the upkeep cost.
		private static int ApplyEnergyCoatReduction(int damage, Character target)
		{
			if (target.StatusEffects?.Has(StatusId.EnergyCoat) != true)
				return damage;

			var maxSp = target.Parameters.SpMax;
			if (maxSp <= 0)
				return damage;

			var per = (100 * target.Parameters.Sp / maxSp - 1) / 20;
			if (per < 0) per = 0;
			if (per > 4) per = 4;

			var cost = (10 + 5 * per) * maxSp / 1000;
			if (!target.TrySpendSp(cost))
			{
				target.StatusEffects.Stop(StatusId.EnergyCoat);
				return damage;
			}

			var reductionPct = 6 * (1 + per);
			return damage * (100 - reductionPct) / 100;
		}

		// SC_KYRIE (PR_KYRIE): a holy barrier with both an HP pool (val2)
		// and a hit counter (val3). Mirrors eAthena battle.c:492 — the HP
		// pool is drained by every incoming hit, but only weapon attacks
		// have their damage reduced; magic still drains but passes through.
		// Holy Light ends the barrier outright. The barrier ends when val2
		// or val3 runs out.
		private static int ApplyKyrieEleison(int damage, Character target, bool isWeaponHit, SkillId skillId = SkillId.None)
		{
			if (damage <= 0 || target.StatusEffects == null)
				return damage;

			var effect = target.StatusEffects.Get(StatusId.KyrieEleison);
			if (effect == null)
				return damage;

			effect.Val2 -= damage;
			effect.Val3 -= 1;

			if (isWeaponHit)
				damage = effect.Val2 >= 0 ? 0 : -effect.Val2;

			if (effect.Val2 <= 0 || effect.Val3 <= 0 || skillId == SkillId.AL_HOLYLIGHT)
				target.StatusEffects.Stop(StatusId.KyrieEleison);

			return damage;
		}

		// SC_LEXAETERNA (PR_LEXAETERNA): the next single hit on the
		// target deals 2x damage, then the status is dispelled. Applies
		// to physical and magical attacks alike. Mirrors eAthena
		// battle.c — damage *= 2, status_change_end on consumption.
		private static int ApplyLexAeterna(int damage, Character target)
		{
			if (target.StatusEffects == null)
				return damage;

			if (!target.StatusEffects.Has(StatusId.LexAeterna))
				return damage;

			target.StatusEffects.Stop(StatusId.LexAeterna);
			return damage * 2;
		}

		// Sleeping/petrified targets take 1.5x damage. Mirrors eAthena
		// battle_calc_damage's status multiplier branch.
		private static int ApplyStatusMultipliers(int damage, Character target)
		{
			if (target.StatusEffects == null)
				return damage;

			if (target.StatusEffects.Has(StatusId.Sleep) || target.StatusEffects.Has(StatusId.Stone))
				return damage * 3 / 2;

			return damage;
		}

		// eAthena classic: physical hits have a small chance to inflict
		// Bleeding (~1% baseline, modified by VIT). Skill hits and
		// 0-damage hits don't proc.
		private static void TryProcBleeding(AttackContext ctx, AttackResult result, Random rnd)
		{
			if (result.IsMiss || result.Damage <= 0) return;
			if (ctx.SkillId != SkillId.None) return;
			if (ctx.Target.StatusEffects == null) return;
			if (ctx.Target.StatusEffects.Has(StatusId.Bleeding)) return;

			// eAthena classic per-hit bleed: chance per 10000 = max(1, 30 - vit/5).
			// At Vit 0 → 0.30%; Vit 50 → 0.20%; Vit 99 → 0.11%. Mild but
			// not negligible over a long fight.
			var chancePer10000 = Math.Max(1, 30 - ctx.Target.Parameters.Vit / 5);
			if (rnd.Next(10000) < chancePer10000)
			{
				var duration = TimeSpan.FromMinutes(1);
				ctx.Target.StatusEffects.Start(StatusId.Bleeding, 1, duration, ctx.Attacker);
			}
		}

		private static int ApplyPhysicalDefense(int damage, Character target, int defRatio = 100)
		{
			var hardDef = Math.Max(0, target.Parameters.MeleeDefense);
			var softDef = Math.Max(0, target.Parameters.MeleeDefenseBonus);

			// Skills like Pierce / Mammonite reduce DEF rather than bypass it.
			// defRatio is the percentage of DEF that still applies (100 = full).
			var ratio = Math.Clamp(defRatio, 0, 100);
			hardDef = hardDef * ratio / 100;
			softDef = softDef * ratio / 100;

			// Frozen / fully-petrified targets lose half of their hard DEF.
			// Mirrors eAthena status.c:status_calc_def `def >>= 1` on
			// SC_FREEZE / SC_STONE(opt1=OPT1_STONE). Soft DEF is untouched.
			if (IsHardCcImmobilised(target))
				hardDef /= 2;

			if (IsRenewal())
			{
				var denom = 4000 + hardDef * 100;
				if (denom <= 0) denom = 1;
				var reduced = (int)Math.Floor(damage * 4000.0 / denom);
				return Math.Max(1, reduced - softDef);
			}

			var equipPct = Math.Clamp(hardDef, 0, 100);
			var afterEquip = (int)Math.Floor(damage * (100 - equipPct) / 100.0);
			return Math.Max(1, afterEquip - softDef);
		}

		private static int ApplySizePenalty(int damage, Character attacker, Character target)
		{
			// SC_WEAPONPERFECTION (BS_WEAPONPERFECT): bypass weapon-vs-size
			// damage modifier entirely. eAthena battle.c:1371 passes flag
			// bit 8 to battle_calc_base_damage which short-circuits the
			// size table lookup; equivalent to returning the raw damage
			// here.
			if (attacker.StatusEffects?.Has(StatusId.WeaponPerfection) == true)
				return damage;

			var weaponType = WeaponType.Unknown;
			if (attacker is PlayerCharacter pc)
				weaponType = pc.Inventory.RightHand?.Data.GetWeaponType() ?? WeaponType.Unknown;

			var size = SizeType.Medium;
			if (target is Monster monster)
				size = monster.Data.Size;

			var pct = SizeTable.GetModifier(weaponType, size);
			if (pct == 100)
				return damage;

			return (int)Math.Floor(damage * pct / 100.0);
		}

		// Flat ATK bonus from passive race-bane skills, applied before
		// skill ratio. Mirrors eAthena battle.c:719-729 (battle_addmastery):
		//   AL_DEMONBANE — vs Demon race or Undead-element targets:
		//     +level * (3 + (BaseLv + 1) * 0.05)
		//   HT_BEASTBANE — vs Brute / Insect race targets:
		//     +level * 4
		// Mob-only check on DemonBane mirrors eAthena's BL_MOB guard so
		// PvP against another player's Demon-statted character isn't
		// double-amplified.
		private static int GetRaceMasteryBonus(Character attacker, Character target)
		{
			if (attacker is not PlayerCharacter pc || pc.Skills == null)
				return 0;

			RaceType targetRace;
			ElementType targetDefenseElement;
			bool targetIsMob;

			if (target is Monster monster)
			{
				targetRace = monster.Data.Race;
				targetDefenseElement = monster.Data.Element;
				targetIsMob = true;
			}
			else
			{
				targetRace = RaceType.PlayerHuman;
				targetDefenseElement = ResolveDefenseElement(target);
				targetIsMob = false;
			}

			var bonus = 0;

			if (targetIsMob)
			{
				var demonBane = pc.Skills.GetLevel(SkillId.AL_DEMONBANE);
				if (demonBane > 0 && (targetRace == RaceType.Demon || targetDefenseElement == ElementType.Undead))
				{
					var baseLevel = pc.Parameters.BaseLevel;
					bonus += (int)(demonBane * (3 + (baseLevel + 1) * 0.05));
				}
			}

			var beastBane = pc.Skills.GetLevel(SkillId.HT_BEASTBANE);
			if (beastBane > 0 && (targetRace == RaceType.Brute || targetRace == RaceType.Insect))
				bonus += beastBane * 4;

			return bonus;
		}

		// SC_OVERTHRUST (BS_OVERTHRUST): linear +5% per level damage bonus
		// added to the skill ratio. Mirrors eAthena battle.c:1413-1416 which
		// adds val3 (= 5*level) to skillratio for any non-PA_SACRIFICE attack.
		// Applied after element / bonus modifiers but before status multipliers
		// (Sleep / Stone ×1.5) so it stacks linearly with other %-bonuses,
		// matching eAthena's placement before ATK_RATE finalisation.
		private static int ApplyOverThrust(int damage, Character attacker, AttackContext ctx)
		{
			if (attacker.StatusEffects == null)
				return damage;

			// WS_OVERTHRUSTMAX cancels and supersedes BS_OVERTHRUST when
			// active (eAthena status.c:5060-5063). Read MaxOverThrust
			// first so a parallel application (which shouldn't happen
			// after the cancel) still picks the larger bonus.
			var effect = attacker.StatusEffects.Get(StatusId.MaxOverThrust)
				?? attacker.StatusEffects.Get(StatusId.OverThrust);
			if (effect == null || effect.Val3 <= 0)
				return damage;

			// PA_SACRIFICE bypasses OverThrust in eAthena (battle.c:1413).
			// Sabine has no Sacrifice handler yet; add the guard here so
			// the parity holds when it lands.
			if (ctx.SkillId == SkillId.PA_SACRIFICE)
				return damage;

			return damage * (100 + effect.Val3) / 100;
		}

		// SP_ATK_RATE + SP_WEAPON_ATK_RATE + SP_LONG_ATK_RATE / SP_SHORT_ATK_RATE:
		// percent damage scalar applied near the end of physical damage calc.
		// eAthena battle.c:1397 adds `sd->atk_rate` via ATK_ADDRATE;
		// battle.c:885 layers `sd->weapon_atk_rate[weapontype]` per
		// right-hand weapon type; battle.c:2130 folds
		// `sd->long_attack_atk_rate` when the hit is BF_LONG (ranged or
		// flagged long-range skill). All four stack additively as a single
		// percent term so a 10% atk_rate plus a 5% bow rate plus a 15%
		// long-atk-rate yields 1.30× damage on a long-range bow shot.
		private static int ApplyAtkRate(int damage, Character attacker, bool isLongRange)
		{
			var mods = attacker.Modifiers;
			var pct = mods.BonusAtkRate
				+ mods.GetWeaponAtkRate(BattleHelpers.GetWeaponType(attacker))
				+ (isLongRange ? mods.BonusLongAtkRate : mods.BonusShortAtkRate);
			if (pct == 0)
				return damage;

			return damage * (100 + pct) / 100;
		}

		// SP_MATK_RATE + SP_WEAPON_MATK_RATE: magical counterpart of
		// ApplyAtkRate. eAthena pc.c:2125 routes SP_MATK_RATE into
		// `sd->matk_rate`; weapon_matk_rate is the symmetric per-weapon
		// accumulator. Applied after element / bonus modifiers in
		// CalcMagic, before status (Sleep / Stone) multipliers.
		private static int ApplyMatkRate(int damage, Character attacker)
		{
			var pct = attacker.Modifiers.BonusMatkRate
				+ attacker.Modifiers.GetWeaponMatkRate(BattleHelpers.GetWeaponType(attacker));
			if (pct == 0)
				return damage;

			return damage * (100 + pct) / 100;
		}

		private static bool IsRangedWeapon(Character attacker)
			=> BattleHelpers.IsRangedWeapon(attacker);

		// -----------------------------------------------------------------
		// Magic
		// -----------------------------------------------------------------

		private static AttackResult CalcMagic(AttackContext ctx)
		{
			var attacker = ctx.Attacker;
			var target = ctx.Target;
			var rnd = RandomProvider.Get();

			var attackElement = ResolveAttackElement(attacker, ctx);
			var defenseElement = ResolveDefenseElement(target);
			var defenseLevel = ResolveDefenseElementLevel(target);

			var result = new AttackResult
			{
				HitCount = Math.Max(1, ctx.HitCount),
				ActionType = ActionType.Skill,
				AttackElement = attackElement,
				TargetElement = defenseElement,
			};

			if (GroundEffectGuard.IsBlocked(ctx, isRanged: true))
			{
				result.IsMiss = true;
				return result;
			}

			if (TryAbsorbWithMagicRod(ctx))
			{
				result.IsMiss = true;
				return result;
			}

			var renewal = IsRenewal();

			// Renewal magic flee: small INT-based dodge. Pre-renewal magic
			// always lands.
			if (renewal && !ctx.AlwaysHits)
			{
				var mflee = 5 + Math.Max(0, target.Parameters.Int - attacker.Parameters.Int) / 4;
				mflee = Math.Clamp(mflee, 0, 50);
				if (rnd.Next(100) < mflee)
				{
					result.IsMiss = true;
					return result;
				}
			}

			int baseDamage;
			if (renewal)
			{
				// Renewal: WeaponMATK * skillRatio + StatusMATK.
				// WeaponMATK is 0 in v1 (weapons don't carry MATK yet).
				var weaponMatk = attacker.Parameters.WeaponMatk;
				var statusMatk = attacker.Parameters.StatusMatk;
				if (statusMatk == 0) statusMatk = attacker.Parameters.MagicAttack;
				baseDamage = (int)Math.Floor((statusMatk + weaponMatk) * ctx.SkillRatio);
			}
			else
			{
				var minMatk = attacker.Parameters.MagicAttackMin;
				var maxMatk = attacker.Parameters.MagicAttackMax;
				var rolledMatk = (minMatk > 0 || maxMatk > 0)
					? rnd.Next(Math.Min(minMatk, maxMatk), Math.Max(minMatk, maxMatk) + 1)
					: attacker.Parameters.MagicAttack;
				baseDamage = (int)Math.Floor(rolledMatk * ctx.SkillRatio);
			}

			if (baseDamage < 1) baseDamage = 1;

			var afterDef = ctx.IgnoreDefense
				? baseDamage
				: ApplyMagicDefense(baseDamage, target);

			var afterElement = ApplyElement(afterDef, attacker, attackElement, defenseElement, defenseLevel);
			var afterBonuses = ApplyBonusModifiers(afterElement, attacker, target, attackElement);
			var afterMatkRate = ApplyMatkRate(afterBonuses, attacker);
			var afterStatus = ApplyStatusMultipliers(afterMatkRate, target);
			var afterLexAeterna = ApplyLexAeterna(afterStatus, target);
			var afterKyrie = ApplyKyrieEleison(afterLexAeterna, target, isWeaponHit: false, ctx.SkillId);

			// SC_MAGICPOWER (HW_MAGICPOWER): single-shot. The +MATK%
			// rode through ApplyMatkRate above; consume the buff now
			// so the next spell casts at base power. eAthena status.c
			// dispels MAGICPOWER in skill_castend after the cast
			// resolves; we mirror that by ending the status here on
			// the first amplified hit. AoE / multi-tick spells (Storm
			// Gust, Meteor) only get the bonus on their first tick,
			// matching eAthena's "one cast, one consumption" rule.
			if (attacker.StatusEffects?.Has(StatusId.MagicPower) == true)
				attacker.StatusEffects.Stop(StatusId.MagicPower);

			result.Damage = Math.Max(1, afterKyrie);
			result.DamagePerHit = (int)Math.Ceiling(result.Damage / (float)result.HitCount);
			result.Damage = result.DamagePerHit * result.HitCount;

			return result;
		}

		// SC_MAGICROD (SA_MAGICROD): nullifies a single direct magic hit and
		// converts the skill's SP cost into SP for the rod-bearer. Mirrors
		// eAthena skill.c:1660 — gated on `src == dsrc` so ground-spawned
		// AoE timers (Storm Gust, LoV, Meteor, Heaven Drive) bypass it.
		// SP returned is `skill.SpCost * (level * 20) / 100`. Returns true
		// when the hit was absorbed.
		private static bool TryAbsorbWithMagicRod(AttackContext ctx)
		{
			if (ctx.IsAreaOfEffect)
				return false;

			var target = ctx.Target;
			if (target.StatusEffects == null || !target.StatusEffects.Has(StatusId.MagicRod))
				return false;

			if (target is not PlayerCharacter pc)
				return false;

			var effect = target.StatusEffects.Get(StatusId.MagicRod);
			if (effect == null)
				return false;

			var sp = 0;
			if (ZoneServer.Instance.Data.Skills.TryFind(ctx.SkillId, out var skillData))
				sp = skillData.GetSpCost(ctx.SkillLevel);

			sp = sp * (effect.Level * 20) / 100;
			if (sp > 0)
				pc.HealSp(sp);

			Send.ZC_NOTIFY_SKILL(target, target.Handle, SkillId.SA_MAGICROD, effect.Level, 0, 0, 0, ActionType.Skill);
			return true;
		}

		private static int ApplyMagicDefense(int damage, Character target)
		{
			var hardMdef = Math.Max(0, target.Parameters.MagicDefense);
			var softMdef = Math.Max(0, target.Parameters.MagicDefenseBonus);

			// Frozen / fully-petrified targets gain +25% hard MDEF. Mirrors
			// eAthena status.c:status_calc_mdef `mdef += 25*mdef/100`. Soft
			// MDEF is untouched. The bump applies before the renewal /
			// pre-renewal split so both formulas pick it up.
			if (IsHardCcImmobilised(target))
				hardMdef += hardMdef * 25 / 100;

			if (IsRenewal())
			{
				var denom = 1000 + hardMdef * 10;
				if (denom <= 0) denom = 1;
				var reduced = (int)Math.Floor(damage * 1000.0 / denom);
				return Math.Max(1, reduced - softMdef);
			}

			var afterHard = damage - hardMdef;
			var softPct = Math.Clamp(softMdef, 0, 100);
			return Math.Max(1, (int)Math.Floor(afterHard * (100 - softPct) / 100.0));
		}

		// -----------------------------------------------------------------
		// Element / target / bonuses
		// -----------------------------------------------------------------

		// eAthena enchant_eff lookup for SA_VOLCANO / SA_DELUGE / SA_VIOLENTGALE
		// elemental field bonuses (skill.c:198). Indexed by skill level - 1.
		// Internal so the Sage field status handlers can reuse the same
		// ratio for their per-stat bumps without duplicating the table.
		internal static readonly int[] EnchantEff = { 10, 14, 17, 19, 20 };

		private static int ApplyElement(int damage, Character attacker, ElementType atk, ElementType def, int defLevel)
		{
			var pct = ElementTable.GetModifier(atk, def, defLevel);
			pct += GetElementalFieldBonus(attacker, atk);

			if (pct == 100)
				return damage;

			return (int)Math.Floor(damage * pct / 100.0);
		}

		// SA_VOLCANO / SA_DELUGE / SA_VIOLENTGALE: while the attacker stands
		// in the matching elemental field, fire / water / wind attacks gain
		// `enchant_eff[level-1]`% extra elemental ratio. Mirrors eAthena
		// battle.c:254 — applied on top of the base element multiplier.
		private static int GetElementalFieldBonus(Character attacker, ElementType atk)
		{
			if (attacker.StatusEffects == null)
				return 0;

			StatusEffect effect = atk switch
			{
				ElementType.Fire => attacker.StatusEffects.Get(StatusId.Volcano),
				ElementType.Water => attacker.StatusEffects.Get(StatusId.Deluge),
				ElementType.Wind => attacker.StatusEffects.Get(StatusId.Whirlwind),
				_ => null,
			};

			if (effect == null)
				return 0;

			var index = Math.Clamp(effect.Level - 1, 0, EnchantEff.Length - 1);
			return EnchantEff[index];
		}

		private static int ApplyBonusModifiers(int damage, Character attacker, Character target, ElementType attackElement)
		{
			var add = 0;
			var sub = 0;

			if (target is Monster monster)
			{
				add += attacker.Modifiers.GetRaceBonus(monster.Data.Race);
				add += attacker.Modifiers.GetSizeBonus(monster.Data.Size);
				add += attacker.Modifiers.GetElementBonus(monster.Data.Element);
			}

			if (attacker is Monster atkMonster)
			{
				sub += target.Modifiers.GetRaceReduction(atkMonster.Data.Race);
				sub += target.Modifiers.GetSizeReduction(atkMonster.Data.Size);
			}
			sub += target.Modifiers.GetElementReduction(attackElement);

			var pct = 100 + add - sub;
			if (pct == 100)
				return damage;

			if (pct < 0) pct = 0;
			return (int)Math.Floor(damage * pct / 100.0);
		}

		private static ElementType ResolveAttackElement(Character attacker, AttackContext ctx)
		{
			if (ctx.AttackElement != ElementType.Weapon && ctx.AttackElement != ElementType.None)
				return ctx.AttackElement;

			if (attacker is PlayerCharacter pc)
			{
				// Ranged: ammo element overrides weapon element when
				// ammo is equipped. Otherwise use the weapon's element.
				var weaponType = pc.Inventory.RightHand?.Data.GetWeaponType() ?? WeaponType.Unknown;
				var isRanged = weaponType == WeaponType.Bow;

				if (isRanged && pc.Inventory.Ammo != null)
					return pc.Inventory.Ammo.Data.Element;

				// SC_ASPERSIO (PR_ASPERSIO) and the Sage weapon-enchants
				// (SA_FLAMELAUNCHER / FROSTWEAPON / SEISMICWEAPON /
				// LIGHTNINGLOADER) override the wearer's weapon element.
				// eAthena battle_get_atk_ele applies them after the ammo
				// override, so bow shots only swap when no ammo is loaded.
				// Skill-specified elements (handled at the top of this
				// method) are not touched. eAthena lets all of these be
				// mutually exclusive at cast time, so checking any-one-
				// matches in priority order is safe.
				if (ctx.Kind == AttackKind.Physical && attacker.StatusEffects != null)
				{
					var sc = attacker.StatusEffects;
					if (sc.Has(StatusId.Aspersio)) return ElementType.Holy;
					if (sc.Has(StatusId.EndowFire)) return ElementType.Fire;
					if (sc.Has(StatusId.EndowWater)) return ElementType.Water;
					if (sc.Has(StatusId.EndowEarth)) return ElementType.Earth;
					if (sc.Has(StatusId.EndowWind)) return ElementType.Wind;
					if (sc.Has(StatusId.EnchantPoison)) return ElementType.Poison;
				}

				return pc.Inventory.RightHand?.Data.Element ?? ElementType.Neutral;
			}

			if (attacker is Monster monster)
				return monster.Data.Element;

			return ElementType.Neutral;
		}

		private static ElementType ResolveDefenseElement(Character target)
		{
			// Frozen targets are treated as Water, fully-petrified ones as
			// Earth, regardless of equipped armor or monster element.
			// Mirrors eAthena status.c:status_calc_element. Petrifying
			// (OPT1_STONEWAIT) does NOT trigger the override — only the
			// active stone state does.
			if (target.StatusEffects != null)
			{
				if (target.StatusEffects.Has(StatusId.Freeze))
					return ElementType.Water;
				if (target.StatusEffects.Has(StatusId.Stone) && target.BodyState == BodyState.Stone)
					return ElementType.Earth;
			}

			if (target is Monster monster)
				return monster.Data.Element;

			if (target is PlayerCharacter pc)
			{
				// Scan equipped armor for a non-None DefenseElement override.
				// First match wins (chest armor is most common).
				foreach (var item in pc.Inventory.GetItems(static a => a.IsEquipped))
				{
					if (item.Data.DefenseElement != ElementType.None && item.Data.DefenseElement != ElementType.Neutral)
						return item.Data.DefenseElement;
				}
			}

			return ElementType.Neutral;
		}

		private static int ResolveDefenseElementLevel(Character target)
		{
			// Frozen / Stone-active targets always resolve at element level 1,
			// matching eAthena status.c:status_calc_element_lv. The level-1
			// row of the element table holds the harshest multipliers (Fire
			// vs Water 1 = 175% etc.).
			if (IsHardCcImmobilised(target))
				return 1;

			if (target is Monster monster)
				return monster.Data.ElementLevel <= 0 ? 1 : monster.Data.ElementLevel;

			return 1;
		}

		// True when the target is in a hard-CC state that classic eAthena
		// treats as "frozen-equivalent" for defense calc purposes: SC_FREEZE
		// or SC_STONE with opt1 == OPT1_STONE (active petrification, not
		// the OPT1_STONEWAIT countdown). Used by element override, DEF /2
		// and MDEF +25%.
		private static bool IsHardCcImmobilised(Character target)
		{
			if (target.StatusEffects == null)
				return false;
			if (target.StatusEffects.Has(StatusId.Freeze))
				return true;
			if (target.StatusEffects.Has(StatusId.Stone) && target.BodyState == BodyState.Stone)
				return true;
			return false;
		}
	}
}
