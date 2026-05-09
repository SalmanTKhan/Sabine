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
		SignumCrucis = 25,

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
		Curse = 44,

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
		BladeStop = 185,

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

		// Alchemist (2-2) — Chemical Protection prevents the matching
		// Rogue / Bard strip skill from landing for the duration.
		CpWeapon = 230,
		CpShield = 231,
		CpArmor = 232,
		CpHelm = 233,

		// Sage Hocus Pocus — SA_FORTUNE outcome (caster LUK aura).
		Fortune = 234,

		// Sage Hocus Pocus — SA_GRAVITY outcome. Immobilizes the
		// target for the duration. eAthena status.c SC_GRAVITATION.
		Gravity = 235,

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

		// Reborn 2-1 — eAthena reuses the SC_CONCENTRATION name
		// across AC_CONCENTRATION (id 11 above) and LK_CONCENTRATION,
		// but the gameplay payload is different, so the LK variant
		// gets its own slot.
		AuraBlade = 240,
		LkConcentration = 241,
		MagicPower = 242,
		Parrying = 243,
		TensionRelax = 244,
		Berserk = 245,

		// Reborn 2-1 — High Priest. Marker applied to friendlies who
		// stand on the BasilicaUnit cells; the damage-block lives on
		// the unit, the status flag lets future "caster movement
		// lock" land here without changing the wire format.
		Basilica = 246,

		// Reborn 2-1 — Sniper. SC_WINDWALK / SC_TRUESIGHT in eAthena.
		// Both ride existing parameter buckets (Flee / Hit / Speed and
		// every primary stat for TrueSight) so the status handlers stay
		// thin: snapshot the bonus on Val* at start, revert on end.
		WindWalk = 247,
		TrueSight = 248,

		// Reborn 2-1 — Whitesmith. CartBoost rides the Speed bucket
		// (mirrors IncreaseAgi); MaxOverThrust shares the OverThrust
		// damage-rate path in BattleCalculator (cancels OverThrust on
		// start per eAthena status.c:5061); MeltDown is marker-only —
		// the gear-break listener it needs lives in the on-hit pipeline
		// and isn't wired yet.
		CartBoost = 249,
		MaxOverThrust = 250,
		MeltDown = 251,

		// Reborn 2-2 — Stalker. ChaseWalk is the toggle hide-with-slow
		// (eAthena status.c:5537); Preserve locks Plagiarism state;
		// RejectSword reflects sword-class melee hits with limited
		// charges (status.c:5706 — val2 = 15*val1 chance, val3 = 3
		// charges). All marker today: hide-toggle integration, the
		// Plagiarism state itself, and the on-hit reflect listener
		// each live in subsystems that haven't grown the hooks yet.
		ChaseWalk = 252,
		Preserve = 253,
		RejectSword = 254,

		// Reborn 2-2 — Paladin. Sacrifice is a finite-charge damage
		// redirect (val2 = 5 hits per eAthena status.c:5307). Gospel
		// is the toggle ground chant — marker today, the per-tick
		// random-buff roll lives in a different subsystem and would
		// balloon the commit.
		Sacrifice = 255,
		Gospel = 256,

		// Reborn 2-2 — Creator. FullProtection covers all four equip
		// slots from breakage; marker until the on-hit equip-break
		// listener exists (tied to the WS_MELTDOWN deferral).
		FullProtection = 257,

		// Reborn 2-2 — Clown / Gypsy. Marionette is a paired buff that
		// transfers stat caps between two performers; LongingFreedom
		// cancels ensemble lock; Hermode is dispel immunity.
		// All marker today — the read sites in eAthena live in
		// status.c stat-cap rebuilds, the ensemble timer, and the
		// dispel guard, none of which are wired in Sabine.
		Marionette = 258,
		LongingFreedom = 259,
		Hermode = 260,
	}
}
