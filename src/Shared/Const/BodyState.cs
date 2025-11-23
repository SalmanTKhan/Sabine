
namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines non-stackable status changes that affect a character's body state.
	/// Corresponds to opt1 in rAthena.
	/// </summary>
	public enum BodyState : short
	{
		None = 0,
		Stone = 1,
		Freeze = 2,
		Stun = 3,
		Sleep = 4,
		Undead = 5,
		Petrifying = 6,
		Burning = 7,
		Imprison = 8,
	}
}