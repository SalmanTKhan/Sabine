using System.Collections;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Ais.Base;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 02: Passive Looter
	/// </summary>
	/// <remarks>
	/// Aegis: 02
	/// Athena: 0x0083 (MD_CANMOVE|MD_LOOTER|MD_CANATTACK)
	/// Behavior: wanders idly, picks up nearby drops, fights back when hit,
	/// and leashes back to spawn after losing aggro. Inherits looter and
	/// leash behavior from LooterAi/ReactiveAi.
	/// </remarks>
	[Ai("Type02")]
	public class Type02 : LooterAi
	{
		// Period-correct: Porings/Lunatics react to picking something up
		// with a music-note emote roughly 15% of the time.
		protected override IEnumerable OnBeforePickUp(int handle, Position pos)
		{
			if (Chance(15))
				yield return Emotion(EmotionId.MusicNote);
		}
	}
}

#pragma warning restore IDE0009
