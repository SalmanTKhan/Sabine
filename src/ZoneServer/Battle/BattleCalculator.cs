using System;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
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

			var renewal = IsRenewal();
			int baseDamage;

			if (renewal)
			{
				// Renewal: WeaponATK * skillRatio + StatusATK + MasteryATK.
				var weaponAtk = attacker.Parameters.WeaponAtk;
				if (weaponAtk == 0) weaponAtk = attacker.Parameters.Attack;

				var statusAtk = attacker.Parameters.StatusAtk;
				var masteryAtk = attacker.Parameters.MasteryAtk;
				baseDamage = (int)Math.Floor(weaponAtk * ctx.SkillRatio) + statusAtk + masteryAtk;
			}
			else
			{
				// Pre-renewal: roll [AttackMin..AttackMax] (already
				// includes refinement and mastery) then apply ratio.
				// A small ±5% variance roll is applied on top to mirror
				// eAthena's WATK randomisation.
				var rolledAtk = rnd.Next(attacker.Parameters.AttackMin, attacker.Parameters.AttackMax + 1);
				var variancePct = 95 + rnd.Next(11); // 95..105
				rolledAtk = rolledAtk * variancePct / 100;
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

			var afterElement = ApplyElement(afterDef, attackElement, defenseElement, defenseLevel);
			var afterBonuses = ApplyBonusModifiers(afterElement, attacker, target, attackElement);
			var afterStatus = ApplyStatusMultipliers(afterBonuses, target);

			result.Damage = Math.Max(ctx.MinDamage > 0 ? ctx.MinDamage : 1, afterStatus);
			result.DamagePerHit = (int)Math.Ceiling(result.Damage / (float)result.HitCount);
			result.Damage = result.DamagePerHit * result.HitCount;

			TryProcBleeding(ctx, result, rnd);

			return result;
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

		private static bool IsRangedWeapon(Character attacker)
		{
			if (attacker is PlayerCharacter pc)
			{
				var type = pc.Inventory.RightHand?.Data.GetWeaponType() ?? WeaponType.Unknown;
				return type == WeaponType.Bow
					|| type == WeaponType.Revolver
					|| type == WeaponType.Rifle
					|| type == WeaponType.Shotgun
					|| type == WeaponType.GatlingGun
					|| type == WeaponType.GrenadeLauncher
					|| type == WeaponType.Shuriken;
			}

			return false;
		}

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

			var afterElement = ApplyElement(afterDef, attackElement, defenseElement, defenseLevel);
			var afterBonuses = ApplyBonusModifiers(afterElement, attacker, target, attackElement);
			var afterStatus = ApplyStatusMultipliers(afterBonuses, target);

			result.Damage = Math.Max(1, afterStatus);
			result.DamagePerHit = (int)Math.Ceiling(result.Damage / (float)result.HitCount);
			result.Damage = result.DamagePerHit * result.HitCount;

			return result;
		}

		private static int ApplyMagicDefense(int damage, Character target)
		{
			var hardMdef = Math.Max(0, target.Parameters.MagicDefense);
			var softMdef = Math.Max(0, target.Parameters.MagicDefenseBonus);

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

		private static int ApplyElement(int damage, ElementType atk, ElementType def, int defLevel)
		{
			var pct = ElementTable.GetModifier(atk, def, defLevel);
			if (pct == 100)
				return damage;

			return (int)Math.Floor(damage * pct / 100.0);
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

				return pc.Inventory.RightHand?.Data.Element ?? ElementType.Neutral;
			}

			if (attacker is Monster monster)
				return monster.Data.Element;

			return ElementType.Neutral;
		}

		private static ElementType ResolveDefenseElement(Character target)
		{
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
			if (target is Monster monster)
				return monster.Data.ElementLevel <= 0 ? 1 : monster.Data.ElementLevel;

			return 1;
		}
	}
}
