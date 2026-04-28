using Sabine.Shared.Const;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// Tracks the state of a player's quests. Backed by the player's
	/// permanent variable container so quest progress survives logout
	/// without needing a dedicated database table yet — when Sabine
	/// grows a real quest log this can be swapped out without changing
	/// the script-facing surface.
	/// </summary>
	public class QuestList
	{
		private const string KeyPrefix = "__quest_state_";

		/// <summary>
		/// Returns the character this quest list belongs to.
		/// </summary>
		public PlayerCharacter Character { get; }

		/// <summary>
		/// Creates a new quest list for the given character.
		/// </summary>
		/// <param name="character"></param>
		public QuestList(PlayerCharacter character)
		{
			this.Character = character;
		}

		private static string KeyFor(int questId)
			=> KeyPrefix + questId;

		/// <summary>
		/// Returns the current state of the given quest. Quests that
		/// were never started return <see cref="QuestState.Inactive"/>.
		/// </summary>
		/// <param name="questId"></param>
		public QuestState GetStatus(int questId)
		{
			var raw = this.Character.Vars.Perm.GetInt(KeyFor(questId), 0);
			return (QuestState)raw;
		}

		/// <summary>
		/// Marks the given quest as active.
		/// </summary>
		/// <param name="questId"></param>
		public void Add(int questId)
			=> this.Character.Vars.Perm.Set(KeyFor(questId), (int)QuestState.Active);

		/// <summary>
		/// Marks the given quest as complete.
		/// </summary>
		/// <param name="questId"></param>
		public void Complete(int questId)
			=> this.Character.Vars.Perm.Set(KeyFor(questId), (int)QuestState.Complete);

		/// <summary>
		/// Clears the quest's stored state. Resets back to <see cref="QuestState.Inactive"/>;
		/// callers that want to truly forget the entry can call this and rely on
		/// the GetInt default (0 == Inactive).
		/// </summary>
		/// <param name="questId"></param>
		public void Erase(int questId)
			=> this.Character.Vars.Perm.Set(KeyFor(questId), (int)QuestState.Inactive);

		/// <summary>
		/// Returns true if the quest is currently active.
		/// </summary>
		/// <param name="questId"></param>
		public bool IsActive(int questId)
			=> this.GetStatus(questId) == QuestState.Active;

		/// <summary>
		/// Returns true if the quest has been completed.
		/// </summary>
		/// <param name="questId"></param>
		public bool IsComplete(int questId)
			=> this.GetStatus(questId) == QuestState.Complete;
	}
}
