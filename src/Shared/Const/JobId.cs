using System;

namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines all available character jobs.
	/// </summary>
	public enum JobId : int
	{
		Novice,

		// 1-1
		Swordman,
		Mage,
		Archer,
		Acolyte,
		Merchant,
		Thief,

		// 2-1
		Knight,
		Priest,
		Wizard,
		Blacksmith,
		Hunter,
		Assassin,
		Knight2,

		// 2-2
		Crusader,
		Monk,
		Sage,
		Rogue,
		Alchemist,
		Bard,
		Dancer,
		Crusader2,
		Wedding,
		SuperNovice,
		Gunslinger,
		Ninja,
		Xmas,
		Summer,
		Hanbok,
		Oktoberfest,
		Summer2,
		MaxBasic,

		NoviceHigh = 4001,
		SwordmanHigh,
		MageHigh,
		ArcherHigh,
		AcolyteHigh,
		MerchantHigh,
		ThiefHigh,
		LordKnight,
		HighPriest,
		HighWizard,
		Whitesmith,
		Sniper,
		AssassinCross,
		LordKnight2,
		Paladin,
		Champion,
		Professor,
		Stalker,
		Creator,
		Clown,
		Gypsy,
		Paladin2,

		Baby,
		BabySwordman,
		BabyMage,
		BabyArcher,
		BabyAcolyte,
		BabyMerchant,
		BabyThief,
		BabyKnight,
		BabyPriest,
		BabyWizard,
		BabyBlacksmith,
		BabyHunter,
		BabyAssassin,
		BabyKnight2,
		BabyCrusader,
		BabyMonk,
		BabySage,
		BabyRogue,
		BabyAlchemist,
		BabyBard,
		BabyDancer,
		BabyCrusader2,
		SuperBaby,

		Taekwon,
		StarGladiator,
		StarGladiator2,
		SoulLinker,

		Gangsi,
		DeathKnight,
		DarkCollector,

		RuneKnight = 4054,
		Warlock,
		Ranger,
		ArchBishop,
		Mechanic,
		GuillotineCross,

		RuneKnightT,
		WarlockT,
		RangerT,
		ArchBishopT,
		MechanicT,
		GuillotineCrossT,

		RoyalGuard,
		Sorcerer,
		Minstrel,
		Wanderer,
		Sura,
		Genetic,
		ShadowChaser,

		RoyalGuardT,
		SorcererT,
		MinstrelT,
		WandererT,
		SuraT,
		GeneticT,
		ShadowChaserT,

		RuneKnight2,
		RuneKnightT2,
		RoyalGuard2,
		RoyalGuardT2,
		Ranger2,
		RangerT2,
		Mechanic2,
		MechanicT2,

		BabyRuneKnight = 4096,
		BabyWarlock,
		BabyRanger,
		BabyArchBishop,
		BabyMechanic,
		BabyGuillotineCross,
		BabyRoyalGuard,
		BabySorcerer,
		BabyMinstrel,
		BabyWanderer,
		BabySura,
		BabyGenetic,
		BabyShadowChaser,

		BabyRuneKnight2,
		BabyRoyalGuard2,
		BabyRanger2,
		BabyMechanic2,

		SuperNoviceE = 4190,
		SuperBabyE,

		Kagerou = 4211,
		Oboro,

		Rebellion = 4215,

		Summoner = 4218,

		BabySummoner = 4220,

		BabyNinja = 4222,
		BabyKagerou,
		BabyOboro,
		BabyTaekwon,
		BabyStarGladiator,
		BabySoulLinker,
		BabyGunslinger,
		BabyRebellion,

		BabyStarGladiator2 = 4238,

		StarEmperor,
		SoulReaper,
		BabyStarEmperor,
		BabySoulReaper,
		StarEmperor2,
		BabyStarEmperor2,

		DragonKnight = 4252,
		Meister,
		ShadowCross,
		ArchMage,
		Cardinal,
		Windhawk,
		ImperialGuard,
		Biolo,
		AbyssChaser,
		ElementalMaster,
		Inquisitor,
		Troubadour,
		Trouvere,

		Windhawk2 = 4278,
		Meister2,
		DragonKnight2,
		ImperialGuard2,

		SkyEmperor = 4302,
		SoulAscetic,
		Shinkiro,
		Shiranui,
		NightWatch,
		HyperNovice,
		SpiritHandler,

		SkyEmperor2 = 4316,

		SecondJobStart = 4331,
		RuneKnight2nd,
		Mechanic2nd,
		GuillotineCross2nd,
		Warlock2nd,
		Archbishop2nd,
		Ranger2nd,
		RoyalGuard2nd,
		Genetic2nd,
		ShadowChaser2nd,
		Sorcerer2nd,
		Sura2nd,
		Minstrel2nd,
		Wanderer2nd,

		SecondJobEnd = 4350,
	}

	/// <summary>
	/// Bitmask of jobs something can apply to.
	/// </summary>
	[Flags]
	public enum JobFilter : long
	{
		Novice = 0x01,
		Swordman = 0x02,
		Mage = 0x04,
		Archer = 0x08,
		Acolyte = 0x10,
		Merchant = 0x20,
		Thief = 0x40,

		All = -1,
	}

	/// <summary>
	/// Extensions for the JobId enum.
	/// </summary>
	public static class JobIdExtensions
	{
		/// <summary>
		/// Returns true if the job matches the given filter, like when
		/// the job can use a certain item.
		/// </summary>
		/// <param name="jobId"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
		public static bool Matches(this JobId jobId, JobFilter filter)
		{
			return (filter & (JobFilter)(1L << (int)jobId)) != 0;
		}
	}
}
