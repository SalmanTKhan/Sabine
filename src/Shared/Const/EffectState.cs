﻿using System;

namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines visual effects for buffs and auras.
	/// Corresponds to opt3 in rAthena.
	/// </summary>
	[Flags]
	public enum EffectState : int
	{
		None = 0,
		Quicken = 1 << 0,
		Overthrust = 1 << 1,
		Energycoat = 1 << 2,
		Explosionspirits = 1 << 3,
		Steelbody = 1 << 4,
		Bladestop = 1 << 5,
		Aurablade = 1 << 6,
		Berserk = 1 << 7,
		Lightblade = 1 << 8, // Not used
		Moonlit = 1 << 9,
		Marionette = 1 << 10,
		Assumptio = 1 << 11,
		Warm = 1 << 12, // SG_WARM
		Kaite = 1 << 13,
		Bunsin = 1 << 14,
		Soullink = 1 << 15,
		Undead = 1 << 16, // Property Change
		Contract = 1 << 17, // Not used
	}
}