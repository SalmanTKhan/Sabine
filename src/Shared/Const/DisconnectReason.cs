namespace Sabine.Shared.Const
{
	/// <summary>
	/// Specifies why a disconnect was requested.
	/// </summary>
	public enum DisconnectReason : byte
	{
		/// <summary>
		/// Doesn't display a message and simply closes the connection.
		/// </summary>
		NoReason = 0,

		/// <summary>
		/// Displays the message "Server Off" before closing the connection.
		/// </summary>
		ServerOff = 1,

		/// <summary>
		/// Displays the message "Double login probibited" before closing
		/// the connection.
		/// </summary>
		DoubleLoginProbibited = 2,

		/// <summary>
		/// Server is shutting down. (Also used for server off)
		/// </summary>
		ServerShutdown = 1,

		/// <summary>
		/// The server has been shut down.
		/// </summary>
		ServerClosed = 2,

		/// <summary>
		/// Another user has logged into this account.
		/// </summary>
		DoubleLogin = 3,

		/// <summary>
		/// Server is temporarily blocking your connection.
		/// </summary>
		TemporarilyBlocked = 4,

		/// <summary>
		/// You have been disconnected by a GM.
		/// </summary>
		KickedByGm = 5,

		/// <summary>
		/// You have been disconnected.
		/// </summary>
		GenericDisconnect = 8,

		/// <summary>
		/// Your Game's EXE file is not the latest version.
		/// </summary>
		OutdatedClient = 9,
	}
}
