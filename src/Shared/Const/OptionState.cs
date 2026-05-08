using System;

namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines visual / mount option flags carried in <c>opt0</c> on
	/// classic-era packets (alpha / beta1 / pre-renewal). Mirrors eAthena
	/// status.h <c>OPTION_*</c> values one-to-one so packet bits land at
	/// the offsets the client already understands. The newer
	/// <see cref="EffectState"/> (opt3) buff-aura field lives in a
	/// separate slot and is unrelated to this one.
	/// </summary>
	[Flags]
	public enum OptionState : int
	{
		None = 0,
		Sight = 0x00000001,
		Hide = 0x00000002,
		Cloak = 0x00000004,
		Cart1 = 0x00000008,
		Falcon = 0x00000010,
		Riding = 0x00000020,
		Invisible = 0x00000040,
		Cart2 = 0x00000080,
		Cart3 = 0x00000100,
		Cart4 = 0x00000200,
		Cart5 = 0x00000400,
		Orcish = 0x00000800,
		Wedding = 0x00001000,
		Ruwach = 0x00002000,
		ChaseWalk = 0x00004000,
		Flying = 0x00008000,

		Cart = Cart1 | Cart2 | Cart3 | Cart4 | Cart5,
	}
}
