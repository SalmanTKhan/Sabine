namespace Sabine.Shared.Const
{
	/// <summary>
	/// Response code for refusing connection to login server.
	/// </summary>
	public enum LoginConnectError : byte
	{
		/// <summary>
		/// Shows message "Incorrect UserID".
		/// </summary>
		UserNotFound = 0,

		/// <summary>
		/// Shows message "Incorrect Password".
		/// </summary>
		PasswordIncorrect = 1,

		/// <summary>
		/// Shows message "ID expired".
		/// </summary>
		IdExpired = 2,

		/// <summary>
		/// Rejected from server.
		/// </summary>
		RejectedFromServer = 3,

		/// <summary>
		/// You have been blocked by the GM Team.
		/// </summary>
		BlockedByGm = 4,

		/// <summary>
		/// Your Game's EXE file is not the latest version.
		/// </summary>
		OutdatedClient = 5,

		/// <summary>
		/// You are prohibited to log in until %s.
		/// </summary>
		ProhibitedLogin = 6,

		/// <summary>
		/// Server is jammed due to over-population.
		/// </summary>
		ServerOverpopulated = 7,

		/// <summary>
		/// No more accounts may be connected from this IP.
		/// </summary>
		IpLimitExceeded = 8,

		/// <summary>
		/// This account has been BANNED!
		/// </summary>
		AccountBanned = 9,

		/// <summary>
		/// Server still recognizes your last connection.
		/// </-summary>
		StillLoggedIn = 11,

		/// <summary>
		/// Shows message "Access Denied".
		/// </summary>
		AccessDenied = byte.MaxValue,
	}
}
