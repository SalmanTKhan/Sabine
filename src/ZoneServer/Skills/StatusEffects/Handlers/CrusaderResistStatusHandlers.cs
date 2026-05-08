using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_TRUST (CR_TRUST / Faith) — +5%/level Holy resist. eAthena
	/// (status.c:2383) applies it as a passive, but Sabine models the
	/// skill as a long-duration buff, so the resist delta is added on
	/// cast and reverted when the buff ends or is refreshed. Snapshot
	/// in Val2 so the revert is symmetric even if level changes mid-
	/// buff.
	/// </summary>
	[StatusEffectHandler(StatusId.Trust)]
	public class TrustStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var bonus = 5 * effect.Level;
			effect.Val2 = bonus;
			target.Modifiers.SubElement[(int)ElementType.Holy] += bonus;
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Modifiers.SubElement[(int)ElementType.Holy] -= effect.Val2;
		}
	}

	/// <summary>
	/// SC_PROVIDENCE (CR_PROVIDENCE) — Val2 = 5*level, applied to both
	/// Holy element resist and Demon race resist on the target
	/// (eAthena status.c:5335 + 2415-2416). The reduction flows through
	/// <c>BattleCalculator.ApplyBonusModifiers</c> via the existing
	/// SubElement / SubRace arrays.
	/// </summary>
	[StatusEffectHandler(StatusId.Providence)]
	public class ProvidenceStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var bonus = 5 * effect.Level;
			effect.Val2 = bonus;
			target.Modifiers.SubElement[(int)ElementType.Holy] += bonus;
			target.Modifiers.SubRace[(int)RaceType.Demon] += bonus;
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 == 0)
				return;

			target.Modifiers.SubElement[(int)ElementType.Holy] -= effect.Val2;
			target.Modifiers.SubRace[(int)RaceType.Demon] -= effect.Val2;
		}
	}
}
