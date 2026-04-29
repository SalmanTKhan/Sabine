using System;
using Sabine.Shared.Const;
using Yggdrasil.Util;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// Represents a character's parameters (stats, sub-stats, and
	/// anything related).
	/// </summary>
	public abstract class Parameters
	{
		#region Base Stats
		/// <summary>
		/// Gets or sets the character's STR stat.
		/// </summary>
		public int Str
		{
			get => _str;
			set { _str = Math.Max(1, value); }
		}
		private int _str = 1;

		/// <summary>
		/// Gets or sets the character's AGI stat.
		/// </summary>
		public int Agi
		{
			get => _agi;
			set { _agi = Math.Max(1, value); }
		}
		private int _agi = 1;

		/// <summary>
		/// Gets or sets the character's VIT stat.
		/// </summary>
		public int Vit
		{
			get => _vit;
			set { _vit = Math.Max(1, value); }
		}
		private int _vit = 1;

		/// <summary>
		/// Gets or sets the character's INT stat.
		/// </summary>
		public int Int
		{
			get => _int;
			set { _int = Math.Max(1, value); }
		}
		private int _int = 1;

		/// <summary>
		/// Gets or sets the character's DEX stat.
		/// </summary>
		public int Dex
		{
			get => _dex;
			set { _dex = Math.Max(1, value); }
		}
		private int _dex = 1;

		/// <summary>
		/// Gets or sets the character's LUK stat.
		/// </summary>
		public int Luk
		{
			get => _luk;
			set { _luk = Math.Max(1, value); }
		}
		private int _luk = 1;

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the STR stat by one.
		/// </summary>
		public int StrNeeded => (1 + (this.Str + 9) / 10);

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the AGI stat by one.
		/// </summary>
		public int AgiNeeded => (1 + (this.Agi + 9) / 10);

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the VIT stat by one.
		/// </summary>
		public int VitNeeded => (1 + (this.Vit + 9) / 10);

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the INT stat by one.
		/// </summary>
		public int IntNeeded => (1 + (this.Int + 9) / 10);

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the DEX stat by one.
		/// </summary>
		public int DexNeeded => (1 + (this.Dex + 9) / 10);

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the LUK stat by one.
		/// </summary>
		public int LukNeeded => (1 + (this.Luk + 9) / 10);

		/// <summary>
		/// Gets or sets the character's stat points.
		/// </summary>
		public int StatPoints
		{
			get => _statPoints;
			set { _statPoints = Math.Max(0, value); }
		}
		private int _statPoints = 0;

		/// <summary>
		/// Returns the number of skill points the character has to assign.
		/// </summary>
		public int SkillPoints
		{
			get => _skillPoints;
			set { _skillPoints = Math.Max(0, value); }
		}
		private int _skillPoints = 0;
		#endregion

		#region Vitals and Capacity
		/// <summary>
		/// Returns the character's current weight.
		/// </summary>
		public int Weight { get; set; } = 10;

		/// <summary>
		/// Returns the character's current max weight.
		/// </summary>
		public int WeightMax { get; set; } = 20000;

		/// <summary>
		/// Gets or sets the character's speed.
		/// </summary>
		public int Speed { get; set; } = 200;

		/// <summary>
		/// Gets or sets how many HP the character currently has,
		/// capped between 0 and the max HP.
		/// </summary>
		public int Hp
		{
			get => _hp;
			set { _hp = Math2.Clamp(0, this.HpMax, value); }
		}
		private int _hp = 40;

		/// <summary>
		/// Gets or sets the character's maximum amount of HP.
		/// </summary>
		public int HpMax
		{
			get => _hpMax;
			set { _hpMax = Math.Max(1, value); }
		}
		private int _hpMax = 40;

		/// <summary>
		/// Gets or sets how many SP the character currently has,
		/// capped between 0 and the max SP.
		/// </summary>
		public int Sp
		{
			get => _sp;
			set { _sp = Math2.Clamp(0, this.SpMax, value); }
		}
		private int _sp = 10;

		/// <summary>
		/// Gets or sets the character's maximum amount of SP.
		/// </summary>
		public int SpMax
		{
			get => _spMax;
			set { _spMax = Math.Max(1, value); }
		}
		private int _spMax = 10;
		#endregion

		#region Combat Stats
		/// <summary>
		/// Returns the character's current min attack.
		/// </summary>
		/// <remarks>
		/// A minimum attack parameter, as used by the alpha client.
		/// </remarks>
		public int AttackMin { get; set; } = 1;

		/// <summary>
		/// Returns the character's current max attack.
		/// </summary>
		/// <remarks>
		/// A maximum attack parameter, as used by the alpha client.
		/// </remarks>
		public int AttackMax { get; set; } = 1;

		/// <summary>
		/// Returns the character's current attack value.
		/// </summary>
		/// <remarks>
		/// A singular attack parameter, as used by clients beyond the
		/// alpha.
		/// </remarks>
		public int Attack { get; set; } = 1;

		/// <summary>
		/// Gets or sets the character's attack bonus, displays as "+ X"
		/// on most clients.
		/// </summary>
		public int AttackBonus { get; set; }

		/// <summary>
		/// Returns the character's current singular magic attack value.
		/// </summary>
		/// <remarks>
		/// A singular magic attack parameter, as displayed by the alpha
		/// client, even though there were no skills.
		/// </remarks>
		public int MagicAttack { get; set; }

		/// <summary>
		/// Returns the character's current min magic attack.
		/// </summary>
		/// <remarks>
		/// A minimum magic attack parameter, as used by clients beyond
		/// the alpha.
		/// </remarks>
		public int MagicAttackMin { get; set; }

		/// <summary>
		/// Returns the character's current max magic attack.
		/// </summary>
		/// <remarks>
		/// A maximum magic attack parameter, as used by clients beyond
		/// the alpha.
		/// </remarks>
		public int MagicAttackMax { get; set; }

		/// <summary>
		/// Returns the character's current combined defense value.
		/// </summary>
		/// <remarks>
		/// A singular defense parameter, as used by the alpha client.
		/// There was no visible magic defense.
		/// </remarks>
		public int Defense { get; set; }

		/// <summary>
		/// Returns the character's current melee defense.
		/// </summary>
		/// <remarks>
		/// The hard defense parameter used by clients beyond the alpha.
		/// </remarks>
		public int MeleeDefense { get; set; }

		/// <summary>
		/// Returns the character's current melee defense bonus.
		/// </summary>
		/// <remarks>
		/// The soft defense parameter used by clients beyond the alpha,
		/// displayed as "+ X".
		/// </remarks>
		public int MeleeDefenseBonus { get; set; }

		/// <summary>
		/// Gets or sets the character's hit value, which determines how
		/// likely they are to hit or miss an enemy.
		/// </summary>
		public int Hit { get; set; } = 2;

		/// <summary>
		/// Gets or sets the character's flee value, which determines how
		/// likely they are to evade an enemy attack.
		/// </summary>
		public int Flee { get; set; } = 2;

		/// <summary>
		/// Gets or sets the character's flee bonus.
		/// </summary>
		/// <remarks>
		/// Doesn't appear to exist in the alpha client, displayed as "+
		/// X" on later ones.
		/// </remarks>
		public int FleeBonus { get; set; }

		/// <summary>
		/// Returns the delay between attacks for the character.
		/// </summary>
		public int AttackDelay { get; set; } = 800;

		/// <summary>
		/// Returns the duration of the character's attack motion.
		/// </summary>
		public int AttackMotionDelay { get; set; } = 800;

		/// <summary>
		/// Returns the time it takes to play the character's taking
		/// damage motion.
		/// </summary>
		public int DamageMotionDelay { get; set; } = 1000;

		/// <summary>
		/// Gets or sets the character's attack speed value.
		/// </summary>
		public int Aspd { get; set; } = 150;
		#endregion

		#region Levels and Experience
		/// <summary>
		/// Gets or sets the character's base experience points.
		/// </summary>
		public int BaseExp { get; set; }

		/// <summary>
		/// Gets or sets the character's job experience points.
		/// </summary>
		public int JobExp { get; set; }

		/// <summary>
		/// Returns the amount of experience points necessary to reach
		/// the next base level.
		/// </summary>
		public int BaseExpNeeded { get; set; } = 9;

		/// <summary>
		/// Returns the amount of experience points necessary to reach
		/// the next job level.
		/// </summary>
		public int JobExpNeeded { get; set; }

		/// <summary>
		/// Gets or sets the character's current base level.
		/// </summary>
		public int BaseLevel { get; set; } = 1;

		/// <summary>
		/// Gets or sets the character's current base level.
		/// </summary>
		public int JobLevel { get; set; } = 1;
		#endregion

		#region Currencies and Identity
		/// <summary>
		/// Gets or sets how many Zeny the character has.
		/// </summary>
		public int Zeny
		{
			get => _zeny;
			set { _zeny = Math.Max(0, value); }
		}
		private int _zeny = 0;

		/// <summary>
		/// Gets or sets the character's karma value.
		/// </summary>
		public int Karma { get; set; }

		/// <summary>
		/// Gets or sets the character's manner value.
		/// </summary>
		public int Manner { get; set; }

		/// <summary>
		/// Gets or sets the character's class ID.
		/// </summary>
		public int Class { get; set; }

		/// <summary>
		/// Gets or sets the character's sex.
		/// </summary>
		public int Sex { get; set; }
		#endregion

		#region Bonus Stats and Sub-Stats
		/// <summary>Gets or sets the bonus STR from equipment and buffs.</summary>
		public int BonusStr { get; set; }
		/// <summary>Gets or sets the bonus AGI from equipment and buffs.</summary>
		public int BonusAgi { get; set; }
		/// <summary>Gets or sets the bonus VIT from equipment and buffs.</summary>
		public int BonusVit { get; set; }
		/// <summary>Gets or sets the bonus INT from equipment and buffs.</summary>
		public int BonusInt { get; set; }
		/// <summary>Gets or sets the bonus DEX from equipment and buffs.</summary>
		public int BonusDex { get; set; }
		/// <summary>Gets or sets the bonus LUK from equipment and buffs.</summary>
		public int BonusLuk { get; set; }

		/// <summary>Gets or sets the character's status attack power.</summary>
		public int AttackPower { get; set; }
		/// <summary>Gets or sets the character's weapon attack power.</summary>
		public int AttackPower2 { get; set; }
		/// <summary>Gets or sets the character's status magic attack power.</summary>
		public int MagicAttackPower { get; set; }
		/// <summary>Gets or sets the character's weapon magic attack power.</summary>
		public int MagicAttackPower2 { get; set; }
		/// <summary>Gets or sets the character's soft defense.</summary>
		public int Defense1 { get; set; }
		/// <summary>Gets or sets the character's hard defense.</summary>
		public int Defense2 { get; set; }

		/// <summary>
		/// Returns the character's current magic defense.
		/// </summary>
		/// <remarks>
		/// The hard magic defense parameter used by clients beyond the
		/// alpha.
		/// </remarks>
		public int MagicDefense { get; set; }

		/// <summary>
		/// Returns the character's current magic defense bonus.
		/// </summary>
		/// <remarks>
		/// The soft magic defense parameter used by clients beyond the
		/// alpha, displayed as "+ X".
		/// </remarks>
		public int MagicDefenseBonus { get; set; }

		/// <summary>Gets or sets the character's perfect dodge chance.</summary>
		public int Flee2 { get; set; }

		/// <summary>
		/// Gets or sets the character's crit value, determining how
		/// likely they are to crit.
		/// </summary>
		public int Critical { get; set; }
		#endregion

		#region Misc Basic Parameters
		/// <summary>Gets or sets the character's 'upper' state.</summary>
		public int Upper { get; set; }
		/// <summary>Gets or sets the character's partner ID.</summary>
		public int Partner { get; set; }
		/// <summary>Gets or sets the character's cart state.</summary>
		public int Cart { get; set; }
		/// <summary>Gets or sets the character's fame points.</summary>
		public int Fame
		{
			get => _fame;
			set { _fame = Math.Max(0, value); }
		}
		private int _fame = 0;
		/// <summary>Gets or sets the character's unbreakable equipment flags.</summary>
		public int Unbreakable { get; set; }
		/// <summary>Gets or sets the character's cart info flags.</summary>
		public int CartInfo { get; set; }
		#endregion

		#region Special Parameters
		/// <summary>Gets or sets whether the character is sitting.</summary>
		public int Sitting { get; set; }
		/// <summary>Gets or sets the amount of zeny in the bank vault.</summary>
		public int BankVault
		{
			get => _bankVault;
			set { _bankVault = Math.Max(0, value); }
		}
		private int _bankVault = 0;
		/// <summary>Gets or sets the number of bronze roulette credits.</summary>
		public int RouletteBronze { get; set; }
		/// <summary>Gets or sets the number of silver roulette credits.</summary>
		public int RouletteSilver { get; set; }
		/// <summary>Gets or sets the number of gold roulette credits.</summary>
		public int RouletteGold { get; set; }
		/// <summary>Gets or sets the character's cash points.</summary>
		public int CashPoints
		{
			get => _cashPoints;
			set { _cashPoints = Math.Max(0, value); }
		}
		private int _cashPoints = 0;
		/// <summary>Gets or sets the character's kafra points.</summary>
		public int KafraPoints
		{
			get => _kafraPoints;
			set { _kafraPoints = Math.Max(0, value); }
		}
		private int _kafraPoints = 0;
		/// <summary>Gets or sets the character's death counter.</summary>
		public int PlayerDieCounter { get; set; }
		/// <summary>Gets or sets the character's cooking mastery level.</summary>
		public int CookMastery { get; set; }
		/// <summary>Gets or sets the character's achievement level.</summary>
		public int AchievementLevel { get; set; }
		#endregion

		#region Mercenary Parameters
		/// <summary>Gets or sets the mercenary's flee stat.</summary>
		public int MercFlee { get; set; }
		/// <summary>Gets or sets the mercenary's kill count.</summary>
		public int MercKills { get; set; }
		/// <summary>Gets or sets the mercenary's faith/loyalty.</summary>
		public int MercFaith { get; set; }
		#endregion

		#region 4th Job Parameters
		/// <summary>Gets or sets the character's POW trait stat.</summary>
		public int Pow { get => _pow; set { _pow = Math.Max(1, value); } }
		private int _pow = 1;
		/// <summary>Gets or sets the character's STA trait stat.</summary>
		public int Sta { get => _sta; set { _sta = Math.Max(1, value); } }
		private int _sta = 1;
		/// <summary>Gets or sets the character's WIS trait stat.</summary>
		public int Wis { get => _wis; set { _wis = Math.Max(1, value); } }
		private int _wis = 1;
		/// <summary>Gets or sets the character's SPL trait stat.</summary>
		public int Spl { get => _spl; set { _spl = Math.Max(1, value); } }
		private int _spl = 1;
		/// <summary>Gets or sets the character's CON trait stat.</summary>
		public int Con { get => _con; set { _con = Math.Max(1, value); } }
		private int _con = 1;
		/// <summary>Gets or sets the character's CRT trait stat.</summary>
		public int Crt { get => _crt; set { _crt = Math.Max(1, value); } }
		private int _crt = 1;

		/// <summary>Gets or sets the character's P.ATK sub-stat.</summary>
		public int Patk { get; set; }
		/// <summary>Gets or sets the character's S.MATK sub-stat.</summary>
		public int Smatk { get; set; }
		/// <summary>Gets or sets the character's RES sub-stat.</summary>
		public int Res { get; set; }
		/// <summary>Gets or sets the character's MRES sub-stat.</summary>
		public int Mres { get; set; }
		/// <summary>Gets or sets the character's H.PLUS sub-stat.</summary>
		public int Hplus { get; set; }
		/// <summary>Gets or sets the character's C.RATE sub-stat.</summary>
		public int Crate { get; set; }

		/// <summary>Gets or sets the character's available trait points.</summary>
		public int TraitPoint { get => _traitPoint; set { _traitPoint = Math.Max(0, value); } }
		private int _traitPoint = 0;

		/// <summary>Gets or sets the character's current AP.</summary>
		public int AP { get => _ap; set { _ap = Math2.Clamp(0, this.MaxAP, value); } }
		private int _ap = 0;
		/// <summary>Gets or sets the character's maximum AP.</summary>
		public int MaxAP { get => _maxAP; set { _maxAP = Math.Max(1, value); } }
		private int _maxAP = 1;

		/// <summary>Gets or sets the bonus POW from equipment and buffs.</summary>
		public int BonusPow { get; set; }
		/// <summary>Gets or sets the bonus STA from equipment and buffs.</summary>
		public int BonusSta { get; set; }
		/// <summary>Gets or sets the bonus WIS from equipment and buffs.</summary>
		public int BonusWis { get; set; }
		/// <summary>Gets or sets the bonus SPL from equipment and buffs.</summary>
		public int BonusSpl { get; set; }
		/// <summary>Gets or sets the bonus CON from equipment and buffs.</summary>
		public int BonusCon { get; set; }
		/// <summary>Gets or sets the bonus CRT from equipment and buffs.</summary>
		public int BonusCrt { get; set; }
		#endregion

		/// <summary>
		/// Gets or sets the character's attack speed bonus, displayed as
		/// "+ X" on most clients.
		/// </summary>
		public int AspdBonus { get; set; }

		/// <summary>
		/// Gets or sets the weapon-derived portion of the character's
		/// physical attack (renewal-only split). In pre-renewal mode
		/// <see cref="Attack"/> is the unified value and this is unused.
		/// </summary>
		public int WeaponAtk { get; set; }

		/// <summary>
		/// Gets or sets the stat-derived portion of the character's
		/// physical attack (renewal-only split — STR/DEX/LUK/Level).
		/// </summary>
		public int StatusAtk { get; set; }

		/// <summary>
		/// Gets or sets the weapon-derived portion of the character's
		/// magic attack (renewal-only split). Weapons don't carry a
		/// MATK field in Sabine yet, so this stays 0.
		/// </summary>
		public int WeaponMatk { get; set; }

		/// <summary>
		/// Gets or sets the stat-derived portion of the character's
		/// magic attack (renewal-only split — INT-driven).
		/// </summary>
		public int StatusMatk { get; set; }

		/// <summary>
		/// Gets or sets the mastery ATK bonus from passive weapon-mastery
		/// skills (Sword Mastery, Vulture's Eye, etc.). In pre-renewal
		/// this is also baked into <see cref="Attack"/> so the status
		/// window reflects it; in renewal the calculator uses it as a
		/// separate addend.
		/// </summary>
		public int MasteryAtk { get; set; }

		/// <summary>
		/// Returns the value for the given parameter.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public int Get(ParameterType type)
		{
			switch (type)
			{
				case ParameterType.Speed: return this.Speed;
				case ParameterType.BaseExp: return this.BaseExp;
				case ParameterType.JobExp: return this.JobExp;
				case ParameterType.Karma: return this.Karma;
				case ParameterType.Manner: return this.Manner;
				case ParameterType.Hp: return this.Hp;
				case ParameterType.HpMax: return this.HpMax;
				case ParameterType.Sp: return this.Sp;
				case ParameterType.SpMax: return this.SpMax;
				case ParameterType.StatPoints: return this.StatPoints;
				case ParameterType.BaseLevel: return this.BaseLevel;
				case ParameterType.SkillPoints: return this.SkillPoints;
				case ParameterType.Str: return this.Str;
				case ParameterType.Agi: return this.Agi;
				case ParameterType.Vit: return this.Vit;
				case ParameterType.Int: return this.Int;
				case ParameterType.Dex: return this.Dex;
				case ParameterType.Luk: return this.Luk;
				case ParameterType.Class: return this.Class;
				case ParameterType.Zeny: return this.Zeny;
				case ParameterType.Sex: return this.Sex;
				case ParameterType.BaseExpNeeded: return this.BaseExpNeeded;
				case ParameterType.JobExpNeeded: return this.JobExpNeeded;
				case ParameterType.Weight: return this.Weight;
				case ParameterType.WeightMax: return this.WeightMax;
				case ParameterType.AttackMin: return this.AttackMin;
				case ParameterType.AttackMax: return this.AttackMax;
				case ParameterType.Defense: return this.Defense;
				case ParameterType.MagicAttack: return this.MagicAttack;
				// NOTE: The following BonusStat enums have values that conflict with the parameters above.
				// They are omitted from this switch to prevent compile errors.
				// case ParameterType.BonusStr:
				// case ParameterType.BonusAgi:
				// case ParameterType.BonusVit:
				// case ParameterType.BonusInt:
				case ParameterType.BonusDex: return this.BonusDex;
				case ParameterType.BonusLuk: return this.BonusLuk;
				case ParameterType.Attack: return this.AttackPower;
				case ParameterType.AttackBonus: return this.AttackPower2;
				case ParameterType.MagicAttackPower: return this.MagicAttackPower;
				case ParameterType.MagicAttackPower2: return this.MagicAttackPower2;
				case ParameterType.MeleeDefense: return this.Defense1;
				case ParameterType.MeleeDefenseBonus: return this.Defense2;
				case ParameterType.MagicDefense: return this.MagicDefense;
				case ParameterType.MagicDefenseBonus: return this.MagicDefenseBonus;
				case ParameterType.Hit: return this.Hit;
				case ParameterType.Flee: return this.Flee;
				case ParameterType.FleeBonus: return this.Flee2;
				case ParameterType.Critical: return this.Critical;
				case ParameterType.Aspd: return this.Aspd;
				case ParameterType.AspdBonus: return this.AspdBonus;
				case ParameterType.JobLevel: return this.JobLevel;
				case ParameterType.Upper: return this.Upper;
				case ParameterType.Partner: return this.Partner;
				case ParameterType.Cart: return this.Cart;
				case ParameterType.Fame: return this.Fame;
				case ParameterType.Unbreakable: return this.Unbreakable;
				case ParameterType.CartInfo: return this.CartInfo;
				case ParameterType.Sitting: return this.Sitting;
				case ParameterType.BankVault: return this.BankVault;
				case ParameterType.RouletteBronze: return this.RouletteBronze;
				case ParameterType.RouletteSilver: return this.RouletteSilver;
				case ParameterType.RouletteGold: return this.RouletteGold;
				case ParameterType.CashPoints: return this.CashPoints;
				case ParameterType.KafraPoints: return this.KafraPoints;
				case ParameterType.PlayerDieCounter: return this.PlayerDieCounter;
				case ParameterType.CookMastery: return this.CookMastery;
				case ParameterType.AchievementLevel: return this.AchievementLevel;
				case ParameterType.MercFlee: return this.MercFlee;
				case ParameterType.MercKills: return this.MercKills;
				case ParameterType.MercFaith: return this.MercFaith;
				case ParameterType.Pow: return this.Pow;
				case ParameterType.Sta: return this.Sta;
				case ParameterType.Wis: return this.Wis;
				case ParameterType.Spl: return this.Spl;
				case ParameterType.Con: return this.Con;
				case ParameterType.Crt: return this.Crt;
				case ParameterType.Patk: return this.Patk;
				case ParameterType.Smatk: return this.Smatk;
				case ParameterType.Res: return this.Res;
				case ParameterType.Mres: return this.Mres;
				case ParameterType.Hplus: return this.Hplus;
				case ParameterType.Crate: return this.Crate;
				case ParameterType.TraitPoint: return this.TraitPoint;
				case ParameterType.AP: return this.AP;
				case ParameterType.MaxAP: return this.MaxAP;
				case ParameterType.BonusPow: return this.BonusPow;
				case ParameterType.BonusSta: return this.BonusSta;
				case ParameterType.BonusWis: return this.BonusWis;
				case ParameterType.BonusSpl: return this.BonusSpl;
				case ParameterType.BonusCon: return this.BonusCon;
				case ParameterType.BonusCrt: return this.BonusCrt;

				default:
					throw new ArgumentException($"Invalid parameter type '{type}'.");
			}
		}

		/// <summary>
		/// Sets the value for the given stat.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public int Set(ParameterType type, int value)
		{
			var before = this.Get(type);

			switch (type)
			{
				case ParameterType.Speed: this.Speed = value; break;
				case ParameterType.BaseExp: this.BaseExp = value; break;
				case ParameterType.JobExp: this.JobExp = value; break;
				case ParameterType.Karma: this.Karma = value; break;
				case ParameterType.Manner: this.Manner = value; break;
				case ParameterType.Hp: this.Hp = value; break;
				case ParameterType.HpMax: this.HpMax = value; break;
				case ParameterType.Sp: this.Sp = value; break;
				case ParameterType.SpMax: this.SpMax = value; break;
				case ParameterType.StatPoints: this.StatPoints = value; break;
				case ParameterType.BaseLevel: this.BaseLevel = value; break;
				case ParameterType.SkillPoints: this.SkillPoints = value; break;
				case ParameterType.Str: this.Str = value; break;
				case ParameterType.Agi: this.Agi = value; break;
				case ParameterType.Vit: this.Vit = value; break;
				case ParameterType.Int: this.Int = value; break;
				case ParameterType.Dex: this.Dex = value; break;
				case ParameterType.Luk: this.Luk = value; break;
				case ParameterType.Class: this.Class = value; break;
				case ParameterType.Zeny: this.Zeny = value; break;
				case ParameterType.Sex: this.Sex = value; break;
				case ParameterType.BaseExpNeeded: this.BaseExpNeeded = value; break;
				case ParameterType.JobExpNeeded: this.JobExpNeeded = value; break;
				case ParameterType.Weight: this.Weight = value; break;
				case ParameterType.WeightMax: this.WeightMax = value; break;
				case ParameterType.AttackMin: this.AttackMin = value; break;
				case ParameterType.AttackMax: this.AttackMax = value; break;
				case ParameterType.Defense: this.Defense = value; break;
				case ParameterType.MagicAttack: this.MagicAttack = value; break;
				case ParameterType.BonusDex: this.BonusDex = value; break;
				case ParameterType.BonusLuk: this.BonusLuk = value; break;
				case ParameterType.Attack: this.AttackPower = value; break;
				case ParameterType.AttackBonus: this.AttackPower2 = value; break;
				case ParameterType.MagicAttackPower: this.MagicAttackPower = value; break;
				case ParameterType.MagicAttackPower2: this.MagicAttackPower2 = value; break;
				case ParameterType.MeleeDefense: this.Defense1 = value; break;
				case ParameterType.MeleeDefenseBonus: this.Defense2 = value; break;
				case ParameterType.MagicDefense: this.MagicDefense = value; break;
				case ParameterType.MagicDefenseBonus: this.MagicDefenseBonus = value; break;
				case ParameterType.Hit: this.Hit = value; break;
				case ParameterType.Flee: this.Flee = value; break;
				case ParameterType.FleeBonus: this.Flee2 = value; break;
				case ParameterType.Critical: this.Critical = value; break;
				case ParameterType.Aspd: this.Aspd = value; break;
				case ParameterType.AspdBonus: this.AspdBonus = value; break;
				case ParameterType.JobLevel: this.JobLevel = value; break;
				case ParameterType.Upper: this.Upper = value; break;
				case ParameterType.Partner: this.Partner = value; break;
				case ParameterType.Cart: this.Cart = value; break;
				case ParameterType.Fame: this.Fame = value; break;
				case ParameterType.Unbreakable: this.Unbreakable = value; break;
				case ParameterType.CartInfo: this.CartInfo = value; break;
				case ParameterType.Sitting: this.Sitting = value; break;
				case ParameterType.BankVault: this.BankVault = value; break;
				case ParameterType.RouletteBronze: this.RouletteBronze = value; break;
				case ParameterType.RouletteSilver: this.RouletteSilver = value; break;
				case ParameterType.RouletteGold: this.RouletteGold = value; break;
				case ParameterType.CashPoints: this.CashPoints = value; break;
				case ParameterType.KafraPoints: this.KafraPoints = value; break;
				case ParameterType.PlayerDieCounter: this.PlayerDieCounter = value; break;
				case ParameterType.CookMastery: this.CookMastery = value; break;
				case ParameterType.AchievementLevel: this.AchievementLevel = value; break;
				case ParameterType.MercFlee: this.MercFlee = value; break;
				case ParameterType.MercKills: this.MercKills = value; break;
				case ParameterType.MercFaith: this.MercFaith = value; break;
				case ParameterType.Pow: this.Pow = value; break;
				case ParameterType.Sta: this.Sta = value; break;
				case ParameterType.Wis: this.Wis = value; break;
				case ParameterType.Spl: this.Spl = value; break;
				case ParameterType.Con: this.Con = value; break;
				case ParameterType.Crt: this.Crt = value; break;
				case ParameterType.Patk: this.Patk = value; break;
				case ParameterType.Smatk: this.Smatk = value; break;
				case ParameterType.Res: this.Res = value; break;
				case ParameterType.Mres: this.Mres = value; break;
				case ParameterType.Hplus: this.Hplus = value; break;
				case ParameterType.Crate: this.Crate = value; break;
				case ParameterType.TraitPoint: this.TraitPoint = value; break;
				case ParameterType.AP: this.AP = value; break;
				case ParameterType.MaxAP: this.MaxAP = value; break;
				case ParameterType.BonusPow: this.BonusPow = value; break;
				case ParameterType.BonusSta: this.BonusSta = value; break;
				case ParameterType.BonusWis: this.BonusWis = value; break;
				case ParameterType.BonusSpl: this.BonusSpl = value; break;
				case ParameterType.BonusCon: this.BonusCon = value; break;
				case ParameterType.BonusCrt: this.BonusCrt = value; break;

				default:
					throw new ArgumentException($"Invalid parameter type '{type}'.");
			}

			var after = this.Get(type);

			this.OnModified(type, before, after);

			return after;
		}

		/// <summary>
		/// Modifies the given parameter and updates the client. Returns the
		/// parameter's new value after the modification.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="modifier"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public int Modify(ParameterType type, int modifier)
		{
			var before = this.Get(type);

			switch (type)
			{
				case ParameterType.Speed: this.Speed += modifier; break;
				case ParameterType.BaseExp: this.BaseExp += modifier; break;
				case ParameterType.JobExp: this.JobExp += modifier; break;
				case ParameterType.Karma: this.Karma += modifier; break;
				case ParameterType.Manner: this.Manner += modifier; break;
				case ParameterType.Hp: this.Hp += modifier; break;
				case ParameterType.HpMax: this.HpMax += modifier; break;
				case ParameterType.Sp: this.Sp += modifier; break;
				case ParameterType.SpMax: this.SpMax += modifier; break;
				case ParameterType.StatPoints: this.StatPoints += modifier; break;
				case ParameterType.BaseLevel: this.BaseLevel += modifier; break;
				case ParameterType.SkillPoints: this.SkillPoints += modifier; break;
				case ParameterType.Str: this.Str += modifier; break;
				case ParameterType.Agi: this.Agi += modifier; break;
				case ParameterType.Vit: this.Vit += modifier; break;
				case ParameterType.Int: this.Int += modifier; break;
				case ParameterType.Dex: this.Dex += modifier; break;
				case ParameterType.Luk: this.Luk += modifier; break;
				case ParameterType.Class: this.Class += modifier; break;
				case ParameterType.Zeny: this.Zeny = Math2.AddChecked(this.Zeny, modifier); break;
				case ParameterType.Sex: this.Sex += modifier; break;
				case ParameterType.BaseExpNeeded: this.BaseExpNeeded += modifier; break;
				case ParameterType.JobExpNeeded: this.JobExpNeeded += modifier; break;
				case ParameterType.Weight: this.Weight += modifier; break;
				case ParameterType.WeightMax: this.WeightMax += modifier; break;
				case ParameterType.AttackMin: this.AttackMin += modifier; break;
				case ParameterType.AttackMax: this.AttackMax += modifier; break;
				case ParameterType.Defense: this.Defense += modifier; break;
				case ParameterType.MagicAttack: this.MagicAttack += modifier; break;
				case ParameterType.BonusDex: this.BonusDex += modifier; break;
				case ParameterType.BonusLuk: this.BonusLuk += modifier; break;
				case ParameterType.Attack: this.AttackPower += modifier; break;
				case ParameterType.AttackBonus: this.AttackPower2 += modifier; break;
				case ParameterType.MagicAttackPower: this.MagicAttackPower += modifier; break;
				case ParameterType.MagicAttackPower2: this.MagicAttackPower2 += modifier; break;
				case ParameterType.MeleeDefense: this.Defense1 += modifier; break;
				case ParameterType.MeleeDefenseBonus: this.Defense2 += modifier; break;
				case ParameterType.MagicDefense: this.MagicDefense += modifier; break;
				case ParameterType.MagicDefenseBonus: this.MagicDefenseBonus += modifier; break;
				case ParameterType.Hit: this.Hit += modifier; break;
				case ParameterType.Flee: this.Flee += modifier; break;
				case ParameterType.FleeBonus: this.Flee2 += modifier; break;
				case ParameterType.Critical: this.Critical += modifier; break;
				case ParameterType.Aspd: this.Aspd += modifier; break;
				case ParameterType.JobLevel: this.JobLevel += modifier; break;
				case ParameterType.Upper: this.Upper += modifier; break;
				case ParameterType.Partner: this.Partner += modifier; break;
				case ParameterType.Cart: this.Cart += modifier; break;
				case ParameterType.Fame: this.Fame += modifier; break;
				case ParameterType.Unbreakable: this.Unbreakable += modifier; break;
				case ParameterType.CartInfo: this.CartInfo += modifier; break;
				case ParameterType.Sitting: this.Sitting += modifier; break;
				case ParameterType.BankVault: this.BankVault += modifier; break;
				case ParameterType.RouletteBronze: this.RouletteBronze += modifier; break;
				case ParameterType.RouletteSilver: this.RouletteSilver += modifier; break;
				case ParameterType.RouletteGold: this.RouletteGold += modifier; break;
				case ParameterType.CashPoints: this.CashPoints += modifier; break;
				case ParameterType.KafraPoints: this.KafraPoints += modifier; break;
				case ParameterType.PlayerDieCounter: this.PlayerDieCounter += modifier; break;
				case ParameterType.CookMastery: this.CookMastery += modifier; break;
				case ParameterType.AchievementLevel: this.AchievementLevel += modifier; break;
				case ParameterType.MercFlee: this.MercFlee += modifier; break;
				case ParameterType.MercKills: this.MercKills += modifier; break;
				case ParameterType.MercFaith: this.MercFaith += modifier; break;
				case ParameterType.Pow: this.Pow += modifier; break;
				case ParameterType.Sta: this.Sta += modifier; break;
				case ParameterType.Wis: this.Wis += modifier; break;
				case ParameterType.Spl: this.Spl += modifier; break;
				case ParameterType.Con: this.Con += modifier; break;
				case ParameterType.Crt: this.Crt += modifier; break;
				case ParameterType.Patk: this.Patk += modifier; break;
				case ParameterType.Smatk: this.Smatk += modifier; break;
				case ParameterType.Res: this.Res += modifier; break;
				case ParameterType.Mres: this.Mres += modifier; break;
				case ParameterType.Hplus: this.Hplus += modifier; break;
				case ParameterType.Crate: this.Crate += modifier; break;
				case ParameterType.TraitPoint: this.TraitPoint += modifier; break;
				case ParameterType.AP: this.AP += modifier; break;
				case ParameterType.MaxAP: this.MaxAP += modifier; break;
				case ParameterType.BonusPow: this.BonusPow += modifier; break;
				case ParameterType.BonusSta: this.BonusSta += modifier; break;
				case ParameterType.BonusWis: this.BonusWis += modifier; break;
				case ParameterType.BonusSpl: this.BonusSpl += modifier; break;
				case ParameterType.BonusCon: this.BonusCon += modifier; break;
				case ParameterType.BonusCrt: this.BonusCrt += modifier; break;

				default:
					throw new ArgumentException($"Unsupported parameter type '{type}'.");
			}

			// Get new value after it was properly assigned and potentially
			// capped in the property.
			var after = this.Get(type);

			this.OnModified(type, before, after);

			return after;
		}

		/// <summary>
		/// Called when a parameter was modified via Modify.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="before"></param>
		/// <param name="after"></param>
		protected virtual void OnModified(ParameterType type, int before, int after)
		{
		}

		/// <summary>
		/// Returns the amount of stat points needed to increase the
		/// given stat by one.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public int GetStatPointsNeeded(ParameterType type)
		{
			switch (type)
			{
				case ParameterType.Str: return this.StrNeeded;
				case ParameterType.Agi: return this.AgiNeeded;
				case ParameterType.Vit: return this.VitNeeded;
				case ParameterType.Int: return this.IntNeeded;
				case ParameterType.Dex: return this.DexNeeded;
				case ParameterType.Luk: return this.LukNeeded;

				default:
					throw new ArgumentException($"Invalid stat type '{type}'.");
			}
		}

		/// <summary>
		/// Recalculates all sub-stats.
		/// </summary>
		public virtual void RecalculateAll()
		{
		}
	}

	/// <summary>
	/// Represents a character's parameters (stats, sub-stats, and
	/// anything related).
	/// </summary>
	public class Parameters<TCharacter> : Parameters where TCharacter : Character
	{
		/// <summary>
		/// Returns the character these parameters belong to.
		/// </summary>
		public TCharacter Character { get; }

		/// <summary>
		/// Creates new instance.
		/// </summary>
		/// <param name="character"></param>
		public Parameters(TCharacter character)
		{
			this.Character = character;
		}
	}
}
