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
		// Equip / card flat-stat bucket
		// -------------------------------------------------------------
		// PlayerCharacterParameters.RecalculateAll reassigns Attack /
		// AttackMin / AttackMax / Hit / Flee / HpMax / SpMax / MagicAttack
		// / MeleeDefense / MagicDefense from scratch on every equip change,
		// which means a `Parameters.Modify(Attack, +5)` from a card was
		// silently overwritten the moment another equip slot recalculated
		// stats. These fields hold the additive deltas card / equip
		// handlers contribute, and the Recalculate* methods fold them in
		// at the end so the bonuses survive subsequent recalcs.
		//
		// Status effects that mutate substats at runtime (e.g. Impositio
		// Manus) still go through Parameters.Modify; the recalc-survival
		// problem is acknowledged separately and not touched here.

		/// <summary>Flat ATK added by equipped items / cards on top of the recalc result.</summary>
		public int EquipAttack { get; set; }

		/// <summary>Flat AttackMin added on top of the recalc result.</summary>
		public int EquipAttackMin { get; set; }

		/// <summary>Flat AttackMax added on top of the recalc result.</summary>
		public int EquipAttackMax { get; set; }

		/// <summary>Flat MATK added on top of the recalc result.</summary>
		public int EquipMagicAttack { get; set; }

		/// <summary>Flat Hit added on top of the recalc result.</summary>
		public int EquipHit { get; set; }

		/// <summary>Flat Flee added on top of the recalc result.</summary>
		public int EquipFlee { get; set; }

		/// <summary>Flat MaxHP added on top of the recalc result.</summary>
		public int EquipMaxHp { get; set; }

		/// <summary>Flat MaxSP added on top of the recalc result.</summary>
		public int EquipMaxSp { get; set; }

		/// <summary>Flat hard MeleeDefense added on top of the recalc result.</summary>
		public int EquipMeleeDefense { get; set; }

		/// <summary>Flat hard MagicDefense added on top of the recalc result.</summary>
		public int EquipMagicDefense { get; set; }

		public BonusModifiers()
		{
			this.AddRace = new int[(int)RaceType.All + 1];
			this.SubRace = new int[(int)RaceType.All + 1];
			this.AddSize = new int[3];
			this.SubSize = new int[3];
			this.AddElement = new int[10];
			this.SubElement = new int[10];
		}

		/// <summary>
		/// Adds (or subtracts, with a negative <paramref name="amount"/>)
		/// a flat bonus for the given parameter to the equip-bonus bucket.
		/// Caller is responsible for triggering a recalc afterwards (cards
		/// rely on Inventory.OnEquippedItem doing it; runtime callers must
		/// invoke <c>Parameters.RecalculateAll()</c> themselves).
		/// </summary>
		public void AddEquipBonus(ParameterType type, int amount)
		{
			switch (type)
			{
				case ParameterType.Attack: this.EquipAttack += amount; break;
				case ParameterType.AttackMin: this.EquipAttackMin += amount; break;
				case ParameterType.AttackMax: this.EquipAttackMax += amount; break;
				case ParameterType.MagicAttack: this.EquipMagicAttack += amount; break;
				case ParameterType.Hit: this.EquipHit += amount; break;
				case ParameterType.Flee: this.EquipFlee += amount; break;
				case ParameterType.HpMax: this.EquipMaxHp += amount; break;
				case ParameterType.SpMax: this.EquipMaxSp += amount; break;
				case ParameterType.MeleeDefense: this.EquipMeleeDefense += amount; break;
				case ParameterType.MagicDefense: this.EquipMagicDefense += amount; break;
			}
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
