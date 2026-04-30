using System;

namespace Sabine.Zone.World.Maps
{
	/// <summary>
	/// Bitflags describing per-map gameplay rules. Mirrors the subset of
	/// rAthena/eAthena map flags Sabine commands toggle at runtime.
	/// </summary>
	[Flags]
	public enum MapFlags
	{
		None = 0,
		PvP = 1 << 0,
		Gvg = 1 << 1,
		NoTeleport = 1 << 2,
		NoMemo = 1 << 3,
		NoReturn = 1 << 4,
		NoSave = 1 << 5,
		NoExp = 1 << 6,
		NoDrop = 1 << 7,
		NoVending = 1 << 8,
		NoChat = 1 << 9,
		NoSkill = 1 << 10,
		NoIcewall = 1 << 11,
		NoKnockback = 1 << 12,
		NoCommand = 1 << 13,
	}
}
