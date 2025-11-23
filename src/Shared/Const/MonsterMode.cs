using System;

namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines the behavior and properties of a monster.
	/// </summary>
	[Flags]
	public enum MonsterMode : uint
	{
		None = 0x0000000,
		CanMove = 0x0000001,
		Looter = 0x0000002,
		Aggressive = 0x0000004,
		Assist = 0x0000008,
		CastSensorIdle = 0x0000010,
		NoRandomWalk = 0x0000020,
		NoCast = 0x0000040,
		CanAttack = 0x0000080,
		CastSensorChase = 0x0000200,
		ChangeChase = 0x0000400,
		Angry = 0x0000800,
		ChangeTargetMelee = 0x0001000,
		ChangeTargetChase = 0x0002000,
		TargetWeak = 0x0004000,
		RandomTarget = 0x0008000,
		IgnoreMelee = 0x0010000,
		IgnoreMagic = 0x0020000,
		IgnoreRanged = 0x0040000,
		Mvp = 0x0080000,
		IgnoreMisc = 0x0100000,
		KnockbackImmune = 0x0200000,
		TeleportBlock = 0x0400000,
		FixedItemDrop = 0x1000000,
		Detector = 0x2000000,
		StatusImmune = 0x4000000,
		SkillImmune = 0x8000000,
	}
}
