using Sabine.Shared.Const;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Per-character damage modifier accumulator. Mirrors the bucket of
	/// <c>add_race</c> / <c>sub_race</c> / <c>add_size</c> /
	/// <c>add_ele</c> bonuses that eAthena and rAthena maintain on
	/// <c>map_session_data</c>.
	/// </summary>
	/// <remarks>
	/// In v1 nothing populates these arrays — they're a hook for the
	/// future card-script and equip-bonus engine. The
	/// <see cref="BattleCalculator"/> still consults them, so once the
	/// card engine writes into the arrays the multipliers go live with
	/// no further wiring.
	/// </remarks>
	public class BonusModifiers
	{
		/// <summary>Percent damage bonus dealt to a target of the indexed race.</summary>
		public int[] AddRace { get; }

		/// <summary>Percent damage reduction taken from an attacker of the indexed race.</summary>
		public int[] SubRace { get; }

		/// <summary>Percent damage bonus dealt to a target of the indexed size (S/M/L).</summary>
		public int[] AddSize { get; }

		/// <summary>Percent damage reduction taken from an attacker of the indexed size.</summary>
		public int[] SubSize { get; }

		/// <summary>Percent damage bonus dealt to a target of the indexed defense element.</summary>
		public int[] AddElement { get; }

		/// <summary>Percent damage reduction taken from an attacker of the indexed attack element.</summary>
		public int[] SubElement { get; }

		// -------------------------------------------------------------
		// Flat substat bonus bucket (cards + status buffs)
		// -------------------------------------------------------------
		// PlayerCharacterParameters.RecalculateAll reassigns Attack /
		// AttackMin / AttackMax / Hit / Flee / HpMax / SpMax / MagicAttack
		// / MeleeDefense / MeleeDefenseBonus / MagicDefense /
		// MagicDefenseBonus from scratch on every equip change, so any
		// `Parameters.Modify(Attack, +5)` was silently overwritten the
		// moment another equip slot recalculated stats. Cards (equip-time)
		// and status effects (runtime) both park their flat deltas in this
		// bucket; the Recalculate* methods fold the bucket value in as the
		// final additive term so bonuses survive subsequent recalcs.
		//
		// Status handlers go through <see cref="Sabine.Zone.World.Actors.Character.AddBonusStat"/>,
		// which writes here for player targets and falls back to direct
		// Parameters.Modify on monsters (whose RecalculateAll is a no-op,
		// so the bucket would never be applied to them).

		/// <summary>Flat ATK added on top of the recalc result.</summary>
		public int BonusAttack { get; set; }

		/// <summary>Flat AttackMin added on top of the recalc result.</summary>
		public int BonusAttackMin { get; set; }

		/// <summary>Flat AttackMax added on top of the recalc result.</summary>
		public int BonusAttackMax { get; set; }

		/// <summary>Flat MATK added on top of the recalc result.</summary>
		public int BonusMagicAttack { get; set; }

		/// <summary>Flat Hit added on top of the recalc result.</summary>
		public int BonusHit { get; set; }

		/// <summary>Flat Flee added on top of the recalc result.</summary>
		public int BonusFlee { get; set; }

		/// <summary>Flat MaxHP added on top of the recalc result.</summary>
		public int BonusMaxHp { get; set; }

		/// <summary>
		/// Percent MaxHP scalar applied during recalc before the flat
		/// <see cref="BonusMaxHp"/> term. Mirrors eAthena's
		/// <c>status_calc_maxhp</c> rate-then-flat ordering. Read by
		/// <c>PlayerCharacterParameters.RecalculateHp</c>; written by
		/// SC_BERSERK (+100), SC_DELUGE (+enchant_eff%), and any
		/// <c>bMaxHPrate</c> card script.
		/// </summary>
		public int BonusMaxHpRate { get; set; }

		/// <summary>Flat MaxSP added on top of the recalc result.</summary>
		public int BonusMaxSp { get; set; }

		/// <summary>Flat hard MeleeDefense added on top of the recalc result.</summary>
		public int BonusMeleeDefense { get; set; }

		/// <summary>Flat soft MeleeDefenseBonus (def2) added on top of the recalc result.</summary>
		public int BonusMeleeDefenseBonus { get; set; }

		/// <summary>Flat hard MagicDefense added on top of the recalc result.</summary>
		public int BonusMagicDefense { get; set; }

		/// <summary>Flat soft MagicDefenseBonus (mdef2) added on top of the recalc result.</summary>
		public int BonusMagicDefenseBonus { get; set; }

		/// <summary>Percent HP regen rate bonus (eAthena bHPrecovRate). Read by the regen tick.</summary>
		public int BonusHpRecovRate { get; set; }

		/// <summary>Percent SP regen rate bonus (eAthena bSPrecovRate). Read by the regen tick.</summary>
		public int BonusSpRecovRate { get; set; }

		// -------------------------------------------------------------
		// Damage rate bucket (cards + status buffs that scale damage)
		// -------------------------------------------------------------
		// eAthena keeps these as flat percent accumulators on
		// <c>map_session_data</c>: <c>sd->atk_rate</c> /
		// <c>sd->matk_rate</c> / <c>sd->weapon_atk_rate[wt]</c> /
		// <c>sd->weapon_matk_rate[wt]</c>. The calculator folds them in as
		// the final damage multiplier (battle.c:1397, 875, pc.c:2125).
		// Provoke writes here too — its eAthena equivalent flips the same
		// atk_rate accumulator on the target via status_calc_str/atk.

		/// <summary>Percent ATK bonus applied to outgoing physical damage (eAthena bAtkRate / SP_ATK_RATE).</summary>
		public int BonusAtkRate { get; set; }

		/// <summary>Percent MATK bonus applied to outgoing magic damage (eAthena bMatkRate / SP_MATK_RATE).</summary>
		public int BonusMatkRate { get; set; }

		/// <summary>Per-weapon-type percent ATK bonus (eAthena bWeaponAtkRate / SP_WEAPON_ATK_RATE).</summary>
		public int[] WeaponAtkRate { get; }

		/// <summary>Per-weapon-type percent MATK bonus (eAthena bWeaponMatkRate / SP_WEAPON_MATK_RATE).</summary>
		public int[] WeaponMatkRate { get; }

		/// <summary>Percent damage bonus on BF_LONG hits (eAthena bLongAtkRate / SP_LONG_ATK_RATE).</summary>
		public int BonusLongAtkRate { get; set; }

		/// <summary>Percent damage bonus on BF_SHORT (melee) hits (eAthena bShortAtkRate / SP_SHORT_ATK_RATE).</summary>
		public int BonusShortAtkRate { get; set; }

		// -------------------------------------------------------------
		// HP / SP drain bucket (lifesteal on physical hit)
		// -------------------------------------------------------------
		// eAthena battle.c:battle_drain rolls drain on every player weapon
		// hit: a flat <c>value</c> is added unconditionally, plus a
		// <c>damage * percent / 100</c> term when a per-1000 <c>rate</c>
		// roll succeeds (battle.c:691, battle_calc_drain). Sabine keeps
		// the four scalars separate; card handlers set rate+percent
		// together to mirror eAthena's <c>bonus2 bHpDrainRate, rate,
		// per</c> syntax.

		/// <summary>Flat HP healed on every basic-attack hit (eAthena bHpDrainValue).</summary>
		public int BonusHpDrainValue { get; set; }

		/// <summary>Per-1000 chance to roll the HP drain percent (eAthena bHpDrainRate first arg).</summary>
		public int BonusHpDrainRate { get; set; }

		/// <summary>Percent of damage healed when the HP drain rate roll succeeds (eAthena bHpDrainRate second arg).</summary>
		public int BonusHpDrainPercent { get; set; }

		/// <summary>Flat SP recovered on every basic-attack hit (eAthena bSpDrainValue).</summary>
		public int BonusSpDrainValue { get; set; }

		/// <summary>Per-1000 chance to roll the SP drain percent (eAthena bSpDrainRate first arg).</summary>
		public int BonusSpDrainRate { get; set; }

		/// <summary>Percent of damage recovered when the SP drain rate roll succeeds (eAthena bSpDrainRate second arg).</summary>
		public int BonusSpDrainPercent { get; set; }

		public BonusModifiers()
		{
			this.AddRace = new int[(int)RaceType.All + 1];
			this.SubRace = new int[(int)RaceType.All + 1];
			this.AddSize = new int[3];
			this.SubSize = new int[3];
			this.AddElement = new int[10];
			this.SubElement = new int[10];
			this.WeaponAtkRate = new int[(int)WeaponType.Shuriken + 1];
			this.WeaponMatkRate = new int[(int)WeaponType.Shuriken + 1];
		}

		/// <summary>
		/// Adds (or subtracts, with a negative <paramref name="amount"/>)
		/// a flat bonus for the given parameter to the bucket. Caller is
		/// responsible for triggering a recalc afterwards (cards rely on
		/// Inventory.OnEquippedItem doing it; status handlers should call
		/// <see cref="Sabine.Zone.World.Actors.Character.AddBonusStat"/>
		/// instead, which handles the recalc and the monster fallback).
		/// </summary>
		public void AddBonus(ParameterType type, int amount)
		{
			switch (type)
			{
				case ParameterType.Attack: this.BonusAttack += amount; break;
				case ParameterType.AttackMin: this.BonusAttackMin += amount; break;
				case ParameterType.AttackMax: this.BonusAttackMax += amount; break;
				case ParameterType.MagicAttack: this.BonusMagicAttack += amount; break;
				case ParameterType.Hit: this.BonusHit += amount; break;
				case ParameterType.Flee: this.BonusFlee += amount; break;
				case ParameterType.HpMax: this.BonusMaxHp += amount; break;
				case ParameterType.SpMax: this.BonusMaxSp += amount; break;
				case ParameterType.MeleeDefense: this.BonusMeleeDefense += amount; break;
				case ParameterType.MeleeDefenseBonus: this.BonusMeleeDefenseBonus += amount; break;
				case ParameterType.MagicDefense: this.BonusMagicDefense += amount; break;
				case ParameterType.MagicDefenseBonus: this.BonusMagicDefenseBonus += amount; break;
				case ParameterType.HpRecovRate: this.BonusHpRecovRate += amount; break;
				case ParameterType.SpRecovRate: this.BonusSpRecovRate += amount; break;
				case ParameterType.MaxHpRate: this.BonusMaxHpRate += amount; break;
				case ParameterType.AtkRate: this.BonusAtkRate += amount; break;
				case ParameterType.MagicAtkRate: this.BonusMatkRate += amount; break;
				case ParameterType.LongAtkRate: this.BonusLongAtkRate += amount; break;
				case ParameterType.ShortAtkRate: this.BonusShortAtkRate += amount; break;
				case ParameterType.HpDrainValue: this.BonusHpDrainValue += amount; break;
				case ParameterType.SpDrainValue: this.BonusSpDrainValue += amount; break;
			}
		}

		/// <summary>
		/// Adds an HP-drain rate (per-1000 chance) and percent-of-damage
		/// pair, mirroring eAthena's two-arg <c>bonus2 bHpDrainRate,
		/// rate, per</c>. Caller passes negative values on unequip to
		/// revert. Combining rate+percent in one helper keeps card
		/// handlers symmetrical and prevents a desynced revert when only
		/// one half is touched.
		/// </summary>
		public void AddHpDrain(int rate, int percent)
		{
			this.BonusHpDrainRate += rate;
			this.BonusHpDrainPercent += percent;
		}

		/// <summary>SP counterpart of <see cref="AddHpDrain"/>.</summary>
		public void AddSpDrain(int rate, int percent)
		{
			this.BonusSpDrainRate += rate;
			this.BonusSpDrainPercent += percent;
		}

		/// <summary>
		/// Adds <paramref name="delta"/> to <see cref="SubElement"/> for
		/// every elemental defense element except Neutral — the "blanket
		/// elemental resist" applied by skills like BD_SIEGFRIED. Pass a
		/// negative <paramref name="delta"/> to revert on effect end.
		/// </summary>
		public void AddSubElementAllExceptNeutral(int delta)
		{
			// Indices 1..9 in the SubElement[] array correspond to
			// ElementType.Water..ElementType.Undead per ElementType.cs.
			// Neutral (0) is intentionally skipped.
			for (var i = (int)ElementType.Water; i <= (int)ElementType.Undead; i++)
				this.SubElement[i] += delta;
		}

		/// <summary>
		/// Returns the per-weapon-type ATK rate bonus the attacker has
		/// accumulated for the given weapon (eAthena
		/// <c>sd->weapon_atk_rate[wt]</c>). Out-of-range types map to 0.
		/// </summary>
		public int GetWeaponAtkRate(WeaponType weaponType)
		{
			var idx = (int)weaponType;
			return idx >= 0 && idx < this.WeaponAtkRate.Length ? this.WeaponAtkRate[idx] : 0;
		}

		/// <summary>
		/// Returns the per-weapon-type MATK rate bonus (eAthena
		/// <c>sd->weapon_matk_rate[wt]</c>).
		/// </summary>
		public int GetWeaponMatkRate(WeaponType weaponType)
		{
			var idx = (int)weaponType;
			return idx >= 0 && idx < this.WeaponMatkRate.Length ? this.WeaponMatkRate[idx] : 0;
		}

		/// <summary>
		/// Returns the sum bonus the attacker grants when hitting a target
		/// of the given race, expressed as a percent (e.g. 30 = +30%).
		/// </summary>
		public int GetRaceBonus(RaceType race)
		{
			var idx = (int)race;
			return idx >= 0 && idx < this.AddRace.Length ? this.AddRace[idx] : 0;
		}

		/// <summary>
		/// Returns the target's reduction against attacks from the given
		/// race, expressed as a percent (e.g. 20 = -20% taken).
		/// </summary>
		public int GetRaceReduction(RaceType race)
		{
			var idx = (int)race;
			return idx >= 0 && idx < this.SubRace.Length ? this.SubRace[idx] : 0;
		}

		public int GetSizeBonus(SizeType size)
		{
			var idx = (int)size;
			return idx >= 0 && idx < this.AddSize.Length ? this.AddSize[idx] : 0;
		}

		public int GetSizeReduction(SizeType size)
		{
			var idx = (int)size;
			return idx >= 0 && idx < this.SubSize.Length ? this.SubSize[idx] : 0;
		}

		public int GetElementBonus(ElementType element)
		{
			var idx = (int)element;
			return idx >= 0 && idx < this.AddElement.Length ? this.AddElement[idx] : 0;
		}

		public int GetElementReduction(ElementType element)
		{
			var idx = (int)element;
			return idx >= 0 && idx < this.SubElement.Length ? this.SubElement[idx] : 0;
		}
	}
}
