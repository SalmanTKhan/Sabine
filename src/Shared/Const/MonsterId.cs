namespace Sabine.Shared.Const
{
	/// <summary>
	/// Friendly aliases for monster ids commonly referenced from
	/// engine code. Mirrors entries in <see cref="IdentityId"/> (which
	/// is the canonical sprite/identity enum); this class exists so
	/// consumers like the WoE finishers can spell intent in domain
	/// terms (<c>MonsterId.EMPERIUM</c>) instead of versioned client
	/// constants (<c>IdentityId.JT_EMPELIUM</c>).
	/// </summary>
	public static class MonsterId
	{
		/// <summary>
		/// War of Emperium objective monster. eAthena id 1288.
		/// </summary>
		public const int EMPERIUM = 1288;

		/// <summary>Castle archer guardian. eAthena id 1285.</summary>
		public const int ARCHER_GUARDIAN = 1285;

		/// <summary>Castle knight guardian. eAthena id 1286.</summary>
		public const int KNIGHT_GUARDIAN = 1286;

		/// <summary>Castle soldier guardian. eAthena id 1287.</summary>
		public const int SOLDIER_GUARDIAN = 1287;
	}
}
