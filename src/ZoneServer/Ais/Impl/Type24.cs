using System.Collections;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 24: Generic Slave
	/// </summary>
	/// <remarks>
	/// Aegis: 24
	/// Behavior: A generic slave/summon AI. Functionally identical to Type 15.
	/// It follows its master and assists in combat.
	/// </remarks>
	[Ai("Type24")]
	public class Type24 : Type15
	{
		// This AI is functionally identical to Type 15.
		// It inherits all behavior from the Alchemist Summon AI.
	}
}

#pragma warning restore IDE0009
