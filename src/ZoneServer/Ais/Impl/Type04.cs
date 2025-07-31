using Sabine.Zone.Ais.Base;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 04: Aggressive
	/// </summary>
	/// <remarks>
	/// Aegis: 04
	/// Athena: 0x0281 (MD_CANMOVE|MD_CANATTACK|MD_AGGRESSIVE)
	/// Behavior: Attacks players on sight. Will also fight back if attacked.
	/// Typically has a shorter chase range.
	/// </remarks>
	[Ai("Type04")]
	public class Type04 : AggressiveAi
	{
		// All behavior is inherited from AggressiveAi.
	}
}

#pragma warning restore IDE0009
