using Sabine.Zone.Ais.Base;
using Sabine.Zone.Ais.Impl;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 01: Passive
	/// </summary>
	/// <remarks>
	/// Aegis: 01
	/// Athena: 0x0081 (MD_CANMOVE|MD_CANATTACK)
	/// Behavior: Wanders around passively, but will fight back if attacked.
	/// </remarks>
	[Ai("Type01")]
	public class Type01 : ReactiveAi
	{
		// All behavior is inherited from ReactiveAi.
	}
}

#pragma warning restore IDE0009
