namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines the state of a player's quest.
	/// </summary>
	public enum QuestState : byte
	{
		/// <summary>
		/// Inactive quest (the user can toggle between active and inactive quests).
		/// </summary>
		Inactive = 0,

		/// <summary>
		/// Active quest.
		/// </summary>
		Active = 1,

		/// <summary>
		/// Completed quest.
		/// </summary>
		Complete = 2,
	}
}
