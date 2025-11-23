namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines what a character wants to do.
	/// </summary>
	public enum ActionType : byte
	{
		/// <summary>
		/// Attack once.
		/// </summary>
		Attack = 0,
		// 1 - Pick up item.
		/// <summary>
		/// Sit down.
		/// </summary>
		SitDown = 2,
		/// <summary>
		/// Stand up.
		/// </summary>
		StandUp = 3,
		// 4 - Damage
		// 5 - Double Attack
		// 6 - Endure
		/// <summary>
		/// Attack continuously.
		/// </summary>
		AutoAttack = 7,
		// 8 - Use skill
		Skill = 8,
		// ...and many more server-side actions.
	}
}
