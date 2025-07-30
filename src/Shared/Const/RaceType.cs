namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines what race a monster is.
	/// </summary>
	public enum RaceType
	{
		Formless = 0,
		Undead,
		Brute, // Beast
		Plant,
		Insect,
		Fish,
		Demon,
		DemiHuman,
		Angel,
		Dragon,
		PlayerHuman,
		PlayerDoram,
		All,

		// Legacy
		Beast = Brute,
	}

	/// <summary>
	/// Defines a monster's secondary race type.
	/// </summary>
	public enum Race2Type : byte
	{
		None = 0,
		Goblin,
		Kobold,
		Orc,
		Golem,
		Guardian,
		Ninja,
		Gvg,
		Battlefield,
		Treasure,
		Biolab,
		Manuk,
		Splendide,
		Scaraba,
		OghAtkDef,
		OghHidden,
		Bio5SwordmanThief,
		Bio5AcolyteMerchant,
		Bio5MageArcher,
		Bio5Mvp,
		Clocktower,
		Thanatos,
		Faceworm,
		Hearthunter,
		Rockridge,
		WernerLab,
		TempleDemon,
		IllusionVampire,
		Malangdo,
		Ep172Alpha,
		Ep172Beta,
		Ep172Bath,
		IllusionTurtle,
		RachelSanctuary,
		IllusionLuanda,
		IllusionFrozen,
		IllusionMoonlight,
		Ep16Def,
		EddaArunafeltz,
		Lasagna,
		GlastHeimAbyss,
		DestroyedValkyrieRealm,
		EncroachedGephenia,
	}
}
