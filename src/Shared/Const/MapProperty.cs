namespace Sabine.Shared.Const
{
	/// <summary>
	/// Map property type sent in <c>ZC_NOTIFY_MAPPROPERTY</c>. Drives
	/// client-side UI cues (PvP icon, no-experience banner, etc.).
	/// Mirrors rAthena's <c>enum map_property</c>.
	/// </summary>
	public enum MapProperty : short
	{
		Nothing = 0,
		FreePvpZone = 1,
		EventPvpZone = 2,
		AgitZone = 3,
		PkServer = 4,
		PvpServer = 5,
		DenyDisconnect = 6,
	}

	/// <summary>
	/// ZC_SHOWDIGIT countdown style — drives how the client renders the
	/// number overlay. Mirrors rAthena's <c>clif_showdigit</c> type byte.
	/// </summary>
	public enum ShowDigitType : byte
	{
		/// <summary>Displays the value for 5 seconds and disappears.</summary>
		Once = 0,

		/// <summary>Counts up from <c>-value</c>.</summary>
		IncrementFromNegative = 1,

		/// <summary>Counts down from <c>-value</c>; overflows past 0.</summary>
		DecrementFromNegative = 2,

		/// <summary>Counts down from <c>value</c>; stops at 0 (max 2 digits).</summary>
		DecrementStop = 3,
	}

	/// <summary>
	/// Map type sent in <c>ZC_NOTIFY_MAPINFO</c>. Drives the client's
	/// BGM choice and zone iconography. Mirrors rAthena's
	/// <c>enum e_map_type</c>.
	/// </summary>
	public enum MapInfoType : short
	{
		Village = 0,
		VillageIn = 1,
		Field = 2,
		Dungeon = 3,
		Arena = 4,
		PenaltyFreePvpZone = 5,
		NoPenaltyFreePvpZone = 6,
		EventGuildWar = 7,
		Agit = 8,
		Dungeon2 = 9,
		Dungeon3 = 10,
		PkServer = 11,
		PvpServer = 12,
		DenySkill = 13,
		TurboTrack = 14,
		Jail = 15,
		MonsterTrack = 16,
		PoringBattle = 17,
		AgitSiegeV15 = 18,
		Battlefield = 19,
		PvpTournament = 20,
	}
}
