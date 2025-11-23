using System;

namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines stackable status changes that affect a character's health state.
	/// Corresponds to opt2 in rAthena.
	/// </summary>
	[Flags]
	public enum HealthState : short
	{
		None = 0,
		Poison = 1 << 0,
		Curse = 1 << 1,
		Silence = 1 << 2,
		Confusion = 1 << 3,
		Blind = 1 << 4,
		// Angelus is here in rAthena, but it's a buff, so we omit it for clarity.
		Angelus = 1 << 5,
		Bleeding = 1 << 6,
		DeadlyPoison = 1 << 7, // Deadly Poison
		Fear = 1 << 8,
	}
}