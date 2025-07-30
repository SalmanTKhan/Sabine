namespace Sabine.Shared.Const
{
	/// <summary>
	/// Response code for refusing connection to char server.
	/// </summary>
	public enum CharConnectError : byte
	{
		/// <summary>
		/// Shows message "Client language is different from server".
		/// </summary>
		LanguageIncorrect = 0,

		/// <summary>
		/// Your Game's EXE file is not the latest version.
		/// </summary>
		OutdatedClient = 1,

		/// <summary>
		/// The server is temporarily blocking your connection.
		/// </summary>
		TemporarilyBlocked = 99,

		/// <summary>
		/// Shows message "Access Denied".
		/// </summary>
		AccessDenied = byte.MaxValue,
	}
}
