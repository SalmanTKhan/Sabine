namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines how and on what a skill can be used.
	/// </summary>
	public enum SkillTargetType : short
	{
		Passive = 0,
		Enemy = 1,
		Place = 2,
		Ground = Place,
		Self = 4,
		Friend = 16,
		Ally = Friend,
		Trap = 32
	}
}
