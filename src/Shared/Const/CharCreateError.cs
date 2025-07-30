namespace Sabine.Shared.Const
{
	/// <summary>
	/// Specifies why a character couldn't be created.
	/// </summary>
	public enum CharCreateError : byte
	{
		/// <summary>
		/// The character named already existed.
		/// </summary>
		NameExistsAlready = 0,

		/// <summary>
		/// User is underage (??).
		/// </summary>
		Underaged = 1,

		/// <summary>
		/// Character creation is denied.
		/// </summary>
		CreationFailed = 2,

		/// <summary>
		/// Your stat values are incorrect.
		/// </summary>
		InvalidStats = 3,

		/// <summary>
		/// The name is not valid.
		/// </summary>
		InvalidName = 4,

		/// <summary>
		/// Name is already in use by another account.
		/// </summary>
		NameInUse = 5,

		/// <summary>
		/// You are not able to create a character at this time.
		/// </summary>
		NotAllowed = 6,

		/// <summary>
		/// General error.
		/// </summary>
		Denied = 0xFF,
	}
}
