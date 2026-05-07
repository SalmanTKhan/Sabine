namespace Sabine.Shared.Const
{
	/// <summary>
	/// Identifies the gameplay era a server is configured to emulate.
	/// </summary>
	/// <remarks>
	/// The era selects which formulas, caps, and data files are used at
	/// runtime. It is resolved once at startup from the feature database
	/// and is independent of <see cref="Game.Version"/>, which describes
	/// the packet/client revision.
	///
	/// Resolution rules (highest precedence first):
	///   * Renewal feature enabled  -> <see cref="Renewal"/>
	///   * PreRenewal feature enabled -> <see cref="PreRenewal"/>
	///   * neither                  -> <see cref="Classic"/>
	/// </remarks>
	public enum Era
	{
		/// <summary>
		/// eAthena classic (pre-2010). Default.
		/// </summary>
		Classic = 0,

		/// <summary>
		/// rAthena PRE-RE branch. Classic mechanics with rAthena's
		/// modernized caps and data.
		/// </summary>
		PreRenewal = 1,

		/// <summary>
		/// rAthena RE branch. Renewal stat formulas, refine cap 20,
		/// and 3rd-job content.
		/// </summary>
		Renewal = 2,
	}
}
