#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 05: Aggressive (Long Range)
	/// </summary>
	/// <remarks>
	/// Aegis: 05
	/// Athena: (Same as 04, but monster DB defines long range attack/chase)
	/// Behavior: Attacks players on sight. Will also fight back if attacked.
	/// Typically has a longer chase range and may be a ranged attacker.
	/// The AI implementation is identical to Type04; the difference is in monster data.
	/// </remarks>
	[Ai("Type05")]
	public class Type05 : Type04
	{
		// This AI is functionally identical to Type 04.
		// It inherits all behavior.
	}
}

#pragma warning restore IDE0009
