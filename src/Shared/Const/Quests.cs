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

	/// <summary>
	/// Quest indicator effect type shown above an NPC's head. Mirrors
	/// rAthena's <c>e_questinfo_types</c>.
	/// </summary>
	public enum QuestIconEffect : short
	{
		None = 0,
		Exclamation = 1,
		Question = 2,
	}

	/// <summary>
	/// Color of the quest indicator above an NPC's head. Mirrors
	/// rAthena's <c>e_questinfo_markcolor</c>.
	/// </summary>
	public enum QuestIconColor : short
	{
		Yellow = 0,
		Orange = 1,
		Green = 2,
		Purple = 3,
	}
}
