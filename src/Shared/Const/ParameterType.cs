namespace Sabine.Shared.Const
{
	/// <summary>
	/// Parameter types used in packets that handle certain ranges
	/// of stats and other parameters. Based on eAthena/rAthena's "sp" values.
	/// </summary>
	public enum ParameterType : short
	{
		#region Basic Parameters (0-99)
		Speed = 0,          // SP_SPEED
		BaseExp = 1,        // SP_BASEEXP (Long)
		JobExp = 2,         // SP_JOBEXP (Long)
		Karma = 3,          // SP_KARMA
		Manner = 4,         // SP_MANNER
		Hp = 5,             // SP_HP
		HpMax = 6,          // SP_MAXHP
		Sp = 7,             // SP_SP
		SpMax = 8,          // SP_MAXSP
		StatPoints = 9,     // SP_STATUSPOINT
							// 10 is unused (SP_0a)
		BaseLevel = 11,     // SP_BASELEVEL
		SkillPoints = 12,   // SP_SKILLPOINT
		Str = 13,           // SP_STR
		Agi = 14,           // SP_AGI
		Vit = 15,           // SP_VIT
		Int = 16,           // SP_INT
		Dex = 17,           // SP_DEX
		Luk = 18,           // SP_LUK
		Class = 19,         // SP_CLASS
		Zeny = 20,          // SP_ZENY (Long)
		Sex = 21,           // SP_SEX
		BaseExpNeeded = 22,   // SP_NEXTBASEEXP (Long)
		JobExpNeeded = 23,    // SP_NEXTJOBEXP (Long)
		Weight = 24,        // SP_WEIGHT
		WeightMax = 25,     // SP_MAXWEIGHT
							// 26-31 are unused (SP_1a-SP_1f)
		AttackMin = 32,   // SP_ATKMIN
		AttackMax = 33,   // SP_ATKMAX
		Defense = 34,        // SP_DEF
		MagicAttack = 35, // SP_MATK
		
		// This is where the parameters supported by the alpha client end,
		// but it's fine to send newer ones, as they are simply ignored.

		BonusStr = 32,      // SP_USTR
		BonusAgi = 33,      // SP_UAGI
		BonusVit = 34,      // SP_UVIT
		BonusInt = 35,      // SP_UINT
		BonusDex = 36,      // SP_UDEX
		BonusLuk = 37,      // SP_ULUK
							// 38-40 are unused (SP_26, SP_27, SP_28)

		AttackPower = 41,           // SP_ATK1
		AttackPower2 = 42,          // SP_ATK2
		MagicAttackPower = 43,      // SP_MATK1
		MagicAttackPower2 = 44,     // SP_MATK2
		Defense1 = 45,               // SP_DEF1
		Defense2 = 46,              // SP_DEF2
		MagicDefense = 47,          // SP_MDEF1
		MagicDefense2 = 48,         // SP_MDEF2
		Hit = 49,                   // SP_HIT
		Flee = 50,                  // SP_FLEE1
		Flee2 = 51,                 // SP_FLEE2
		Critical = 52,              // SP_CRITICAL
		Aspd = 53,                  // SP_ASPD
									// 54 is unused (SP_36)
		JobLevel = 55,          // SP_JOBLEVEL
		Upper = 56,             // SP_UPPER
		Partner = 57,           // SP_PARTNER
		Cart = 58,              // SP_CART
		Fame = 59,              // SP_FAME
		Unbreakable = 60,       // SP_UNBREAKABLE

		CartInfo = 99,          // SP_CARTINFO
		#endregion

		#region Special Parameters (100-199)
		KilledGid = 118,        // SP_KILLEDGID
		BaseJob = 119,          // SP_BASEJOB
		BaseClass = 120,        // SP_BASECLASS
		KillerRid = 121,        // SP_KILLERRID
		KilledRid = 122,        // SP_KILLEDRID
		Sitting = 123,          // SP_SITTING
		CharMove = 124,         // SP_CHARMOVE
		CharRename = 125,       // SP_CHARRENAME
		CharFont = 126,         // SP_CHARFONT
		BankVault = 127,        // SP_BANK_VAULT
		RouletteBronze = 128,   // SP_ROULETTE_BRONZE
		RouletteSilver = 129,   // SP_ROULETTE_SILVER
		RouletteGold = 130,     // SP_ROULETTE_GOLD
		CashPoints = 131,       // SP_CASHPOINTS
		KafraPoints = 132,      // SP_KAFRAPOINTS
		PlayerDieCounter = 133, // SP_PCDIECOUNTER
		CookMastery = 134,      // SP_COOKMASTERY
		AchievementLevel = 135, // SP_ACHIEVEMENT_LEVEL

		// Mercenaries
		MercFlee = 165,
		MercKills = 189,
		MercFaith = 190,
		#endregion

		#region 4th Job Parameters (200-299)
		Pow = 219,
		Sta = 220,
		Wis = 221,
		Spl = 222,
		Con = 223,
		Crt = 224,
		Patk = 225,
		Smatk = 226,
		Res = 227,
		Mres = 228,
		Hplus = 229,
		Crate = 230,
		TraitPoint = 231,
		AP = 232,
		MaxAP = 233,

		BonusPow = 247,
		BonusSta = 248,
		BonusWis = 249,
		BonusSpl = 250,
		BonusCon = 251,
		BonusCrt = 252,
		#endregion

		#region Script Bonuses (1000+)
		AttackRange = 1000,             // SP_ATTACKRANGE
		AttackElement = 1001,           // SP_ATKELE
		DefenseElement = 1002,          // SP_DEFELE
		CastRate = 1003,                // SP_CASTRATE
		MaxHpRate = 1004,               // SP_MAXHPRATE
		MaxSpRate = 1005,               // SP_MAXSPRATE
		SpRate = 1006,                  // SP_SPRATE
		AddElement = 1007,              // SP_ADDELE
		AddRace = 1008,                 // SP_ADDRACE
		AddSize = 1009,                 // SP_ADDSIZE
		SubElement = 1010,              // SP_SUBELE
		SubRace = 1011,                 // SP_SUBRACE
		AddEff = 1012,                  // SP_ADDEFF
		ResEff = 1013,                  // SP_RESEFF
		BaseAtk = 1014,                 // SP_BASE_ATK
		AspdRate = 1015,                // SP_ASPD_RATE
		HpRecovRate = 1016,             // SP_HP_RECOV_RATE
		SpRecovRate = 1017,             // SP_SP_RECOV_RATE
		SpeedRate = 1018,               // SP_SPEED_RATE
		CriticalDef = 1019,             // SP_CRITICAL_DEF
		NearAtkDef = 1020,              // SP_NEAR_ATK_DEF
		LongAtkDef = 1021,              // SP_LONG_ATK_DEF
		DoubleRate = 1022,              // SP_DOUBLE_RATE
		DoubleAddRate = 1023,           // SP_DOUBLE_ADD_RATE
		SkillHeal = 1024,               // SP_SKILL_HEAL
		MagicAtkRate = 1025,            // SP_MATK_RATE
		IgnoreDefElement = 1026,        // SP_IGNORE_DEF_ELE
		IgnoreDefRace = 1027,           // SP_IGNORE_DEF_RACE
		AtkRate = 1028,                 // SP_ATK_RATE
		SpeedAddRate = 1029,            // SP_SPEED_ADDRATE
		SpRegenRate = 1030,             // SP_SP_REGEN_RATE
		MagicAtkDef = 1031,             // SP_MAGIC_ATK_DEF
		MiscAtkDef = 1032,              // SP_MISC_ATK_DEF
		IgnoreMdefElement = 1033,       // SP_IGNORE_MDEF_ELE
		IgnoreMdefRace = 1034,          // SP_IGNORE_MDEF_RACE
		MagicAddElement = 1035,         // SP_MAGIC_ADDELE
		MagicAddRace = 1036,            // SP_MAGIC_ADDRACE
		MagicAddSize = 1037,            // SP_MAGIC_ADDSIZE
		PerfectHitRate = 1038,          // SP_PERFECT_HIT_RATE
		PerfectHitAddRate = 1039,       // SP_PERFECT_HIT_ADD_RATE
		CriticalRate = 1040,            // SP_CRITICAL_RATE
		GetZenyNum = 1041,              // SP_GET_ZENY_NUM
		AddGetZenyNum = 1042,           // SP_ADD_GET_ZENY_NUM
		AddDamageClass = 1043,          // SP_ADD_DAMAGE_CLASS
		AddMagicDamageClass = 1044,     // SP_ADD_MAGIC_DAMAGE_CLASS
		AddDefMonster = 1045,           // SP_ADD_DEF_MONSTER
		AddMdefMonster = 1046,          // SP_ADD_MDEF_MONSTER
		AddMonsterDropItem = 1047,      // SP_ADD_MONSTER_DROP_ITEM
		DefRatioAtkElement = 1048,      // SP_DEF_RATIO_ATK_ELE
		DefRatioAtkRace = 1049,         // SP_DEF_RATIO_ATK_RACE
		UnbreakableGarment = 1050,      // SP_UNBREAKABLE_GARMENT
		HitRate = 1051,                 // SP_HIT_RATE
		FleeRate = 1052,                // SP_FLEE_RATE
		Flee2Rate = 1053,               // SP_FLEE2_RATE
		DefRate = 1054,                 // SP_DEF_RATE
		Def2Rate = 1055,                // SP_DEF2_RATE
		MdefRate = 1056,                // SP_MDEF_RATE
		Mdef2Rate = 1057,               // SP_MDEF2_RATE
		SplashRange = 1058,             // SP_SPLASH_RANGE
		SplashAddRange = 1059,          // SP_SPLASH_ADD_RANGE
		AutoSpell = 1060,               // SP_AUTOSPELL
		HpDrainRate = 1061,             // SP_HP_DRAIN_RATE
		SpDrainRate = 1062,             // SP_SP_DRAIN_RATE
		ShortWeaponDamageReturn = 1063, // SP_SHORT_WEAPON_DAMAGE_RETURN
		LongWeaponDamageReturn = 1064,  // SP_LONG_WEAPON_DAMAGE_RETURN
		WeaponComaElement = 1065,       // SP_WEAPON_COMA_ELE
		WeaponComaRace = 1066,          // SP_WEAPON_COMA_RACE
		AddEff2 = 1067,                 // SP_ADDEFF2
		BreakWeaponRate = 1068,         // SP_BREAK_WEAPON_RATE
		BreakArmorRate = 1069,          // SP_BREAK_ARMOR_RATE
		AddStealRate = 1070,            // SP_ADD_STEAL_RATE
		MagicDamageReturn = 1071,       // SP_MAGIC_DAMAGE_RETURN
		AllStats = 1073,                // SP_ALL_STATS
		AgiVit = 1074,                  // SP_AGI_VIT
		AgiDexStr = 1075,               // SP_AGI_DEX_STR
		PerfectHide = 1076,             // SP_PERFECT_HIDE
		NoKnockback = 1077,             // SP_NO_KNOCKBACK
		ClassChange = 1078,             // SP_CLASSCHANGE
		HpDrainValue = 1079,            // SP_HP_DRAIN_VALUE
		SpDrainValue = 1080,            // SP_SP_DRAIN_VALUE
		WeaponAtk = 1081,               // SP_WEAPON_ATK
		WeaponDamageRate = 1082,        // SP_WEAPON_DAMAGE_RATE
		DelayRate = 1083,               // SP_DELAYRATE
		HpDrainValueRace = 1084,        // SP_HP_DRAIN_VALUE_RACE
		SpDrainValueRace = 1085,        // SP_SP_DRAIN_VALUE_RACE
		IgnoreMdefRaceRate = 1086,      // SP_IGNORE_MDEF_RACE_RATE
		IgnoreDefRaceRate = 1087,       // SP_IGNORE_DEF_RACE_RATE
		SkillHeal2 = 1088,              // SP_SKILL_HEAL2
		AddEffOnSkill = 1089,           // SP_ADDEFF_ONSKILL
		AddHealRate = 1090,             // SP_ADD_HEAL_RATE
		AddHeal2Rate = 1091,            // SP_ADD_HEAL2_RATE
		EquipAtk = 1092,                // SP_EQUIP_ATK
		PatkRate = 1093,                // SP_PATK_RATE
		SmatkRate = 1094,               // SP_SMATK_RATE
		ResRate = 1095,                 // SP_RES_RATE
		MresRate = 1096,                // SP_MRES_RATE
		HplusRate = 1097,               // SP_HPLUS_RATE
		CrateRate = 1098,               // SP_CRATE_RATE
		AllTraitStats = 1099,           // SP_ALL_TRAIT_STATS
		MaxApRate = 1100,               // SP_MAXAPRATE
		#endregion

		#region Script Bonuses (2000+)
		RestartFullRecover = 2000,
		NoCastCancel = 2001,
		NoSizeFix = 2002,
		NoMagicDamage = 2003,
		NoWeaponDamage = 2004,
		NoGemstone = 2005,
		NoCastCancel2 = 2006,
		NoMiscDamage = 2007,
		UnbreakableWeapon = 2008,
		UnbreakableArmor = 2009,
		UnbreakableHelm = 2010,
		UnbreakableShield = 2011,
		LongAtkRate = 2012,
		CritAtkRate = 2013,
		CriticalAddRace = 2014,
		NoRegen = 2015,
		AddEffWhenHit = 2016,
		AutoSpellWhenHit = 2017,
		SkillAtk = 2018,
		Unstripable = 2019,
		AutoSpellOnSkill = 2020,
		SpGainValue = 2021,
		HpRegenRate = 2022,
		HpLossRate = 2023,
		AddRace2 = 2024,
		HpGainValue = 2025,
		SubSize = 2026,
		HpDrainValueClass = 2027,
		AddItemHealRate = 2028,
		SpDrainValueClass = 2029,
		ExpAddRace = 2030,
		SpGainRace = 2031,
		SubRace2 = 2032,
		UnbreakableShoes = 2033,
		UnstripableWeapon = 2034,
		UnstripableArmor = 2035,
		UnstripableHelm = 2036,
		UnstripableShield = 2037,
		Intravision = 2038,
		AddMonsterDropItemGroup = 2039,
		SpLossRate = 2040,
		AddSkillBlow = 2041,
		SpVanishRate = 2042,
		MagicSpGainValue = 2043,
		MagicHpGainValue = 2044,
		AddMonsterIdDropItem = 2045,
		Ematk = 2046,
		ComaClass = 2047,
		ComaRace = 2048,
		SkillUseSpRate = 2049,
		SkillCooldown = 2050,
		SkillFixedCast = 2051,
		SkillVariableCast = 2052,
		FixCastRate = 2053,
		VarCastRate = 2054,
		SkillUseSp = 2055,
		MagicAtkElement = 2056,
		AddFixedCast = 2057,
		AddVariableCast = 2058,
		SetDefRace = 2059,
		SetMdefRace = 2060,
		HpVanishRate = 2061,
		IgnoreDefClass = 2062,
		DefRatioAtkClass = 2063,
		AddClass = 2064,
		SubClass = 2065,
		MagicAddClass = 2066,
		WeaponComaClass = 2067,
		IgnoreMdefClassRate = 2068,
		ExpAddClass = 2069,
		AddClassDropItem = 2070,
		AddClassDropItemGroup = 2071,
		AddMaxWeight = 2072,
		AddItemGroupHealRate = 2073,
		HpVanishRaceRate = 2074,
		SpVanishRaceRate = 2075,
		AbsorbDmgMaxHp = 2076,
		SubSkill = 2077,
		SubDefElement = 2078,
		StateNoRecoverRace = 2079,
		CriticalRangeAtk = 2080,
		MagicAddRace2 = 2081,
		IgnoreMdefRace2Rate = 2082,
		WeaponAtkRate = 2083,
		WeaponMatkRate = 2084,
		DropAddRace = 2085,
		DropAddClass = 2086,
		NoMadoFuel = 2087,
		IgnoreDefClassRate = 2088,
		RegenPercentHp = 2089,
		RegenPercentSp = 2090,
		SkillDelay = 2091,
		NoWalkDelay = 2092,
		LongSpGainValue = 2093,
		LongHpGainValue = 2094,
		ShortAtkRate = 2095,
		MagicSubSize = 2096,
		CritDefRate = 2097,
		MagicSubDefElement = 2098,
		ReduceDamageReturn = 2099,
		AddItemSpHealRate = 2100,
		AddItemGroupSpHealRate = 2101,
		WeaponSubSize = 2102,
		AbsorbDmgMaxHp2 = 2103,
		SpIgnoreResRaceRate = 2104,
		SpIgnoreMresRaceRate = 2105,
		EmatkHidden = 2106,
		SkillRatio = 2107,
		#endregion
	}

	/// <summary>
	/// Extensions for ParameterType enum.
	/// </summary>
	public static class ParameterTypeExtensions
	{
		/// <summary>
		/// Returns true if the given paramter is a "long" parameter and
		/// is sent with an int, instead of a short.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsLong(this ParameterType type)
		{
			return
				type == ParameterType.BaseExp ||
				type == ParameterType.JobExp ||
				type == ParameterType.Zeny ||
				type == ParameterType.BaseExpNeeded ||
				type == ParameterType.JobExpNeeded;
		}
	}
}
