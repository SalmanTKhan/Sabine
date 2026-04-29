namespace Sabine.Shared.Const
{
	/// <summary>
	/// Identifies a status effect (buff or debuff) on a character.
	/// </summary>
	/// <remarks>
	/// Values are arbitrary for now. When a status_db is introduced these
	/// will be aligned with rAthena's SC_* ids.
	/// </remarks>
	public enum StatusId : int
	{
		None = 0,

		IncreaseAgi = 1,
		DecreaseAgi = 2,
		Blessing = 3,
		Angelus = 4,

		// Buffs
		Endure = 10,
		Concentration = 11,
		LoudExclamation = 12,
		Sight = 13,
		Hiding = 14,
		EnergyCoat = 15,
		AutoBerserk = 16,
		MovingRecovery = 17,
		TrickDead = 18,

		// Debuffs
		Provoke = 20,
		Stone = 21,
		Freeze = 22,
		Stun = 23,
		Sleep = 24,

		// DOT
		Poison = 30,
		Bleeding = 31,

		// Ailments — flag-only in v1; no skill applies them yet but
		// future cards / mob skills can. Cure dispels Silence, Blind,
		// Confusion, and Chaos.
		Silence = 40,
		Blind = 41,
		Confusion = 42,
		Chaos = 43,

		// Knight (2-1)
		TwoHandQuicken = 100,
		AutoCounter = 101,
		Riding = 102,

		// Priest (2-1)
		Aspersio = 110,
		Suffragium = 111,
		ImpositioManus = 112,
		LexAeterna = 113,
		LexDivina = 114,
		Magnificat = 115,
		Gloria = 116,
		KyrieEleison = 117,
		Sanctuary = 118,
		Assumptio = 119,
		BSSacramenti = 120,

		// Wizard (2-1)
		Quagmire = 130,
		FrostNova = 131,
		StormGustCounter = 132,

		// Hunter (2-1)
		FalconFly = 140,
		AnkleSnare = 141,

		// Blacksmith (2-1)
		AdrenalineRush = 150,
		WeaponPerfection = 151,
		PowerMaximize = 152,
		OverThrust = 153,

		// Assassin (2-1)
		Cloaking = 160,
		EnchantPoison = 161,
		PoisonReact = 162,
		VenomDust = 163,
		VenomSplasher = 164,

		// Crusader (2-2)
		Trust = 170,
		AutoGuard = 171,
		ReflectShield = 172,
		Devotion = 173,
		Providence = 174,
		Defender = 175,
		SpearQuicken = 176,

		// Monk (2-2)
		Spirits = 180,
		ExplosionSpirits = 181,
		MentalStrength = 182,
		ChainCombo = 183,
		ComboFinish = 184,

		// Sage (2-2)
		FreeCast = 190,
		AutoSpell = 191,
		EndowFire = 192,
		EndowWater = 193,
		EndowWind = 194,
		EndowEarth = 195,
		MindBreaker = 196,
		MagicRod = 197,
		Volcano = 198,
		Deluge = 199,
		Whirlwind = 200,
		MagneticEarth = 201,
		Dragonology = 202,

		// Rogue (2-2)
		TunnelDrive = 210,
		StripWeapon = 211,
		StripShield = 212,
		StripArmor = 213,
		StripHelm = 214,
		Plagiarism = 215,

		// Bard / Dancer (2-2) — songs/dances apply per-tick auras
		// while standing in the unit; the flag here is the in-aura
		// marker so handlers can check membership.
		BardSong = 220,
		DancerDance = 221,
		PowerChord = 222,
		Lullaby = 223,
		MentalSensing = 224,
		ServiceForYou = 225,
		FortuneKiss = 226,
	}
}
