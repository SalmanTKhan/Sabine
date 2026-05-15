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
		// eAthena status.c:2376: SC_FAITH grants +200*level max HP on
		// top of the Holy resist. Snapshot in Val3 so the revert is
		// symmetric with the resist delta in Val2.
		public void OnStart(Character target, StatusEffect effect)
		{
			var resistBonus = 5 * effect.Level;
			effect.Val2 = resistBonus;
			target.Modifiers.SubElement[(int)ElementType.Holy] += resistBonus;

			var hpBonus = 200 * effect.Level;
			effect.Val3 = hpBonus;
			target.AddBonusStat(ParameterType.HpMax, hpBonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Modifiers.SubElement[(int)ElementType.Holy] -= effect.Val2;
			if (effect.Val3 != 0)
				target.AddBonusStat(ParameterType.HpMax, -effect.Val3);
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

	/// <summary>
	/// SC_AUTOGUARD (CR_AUTOGUARD) — marker registration. The block
	/// roll lives in <see cref="Sabine.Zone.Battle.Listeners.AutoGuardListener"/>,
	/// which reads the status's <c>Level</c> directly off the
	/// <c>StatusEffects</c> bag, so no OnStart/OnEnd state work is
	/// needed. Having the explicit handler closes the dispatch table
	/// and gives a hook for future shield-icon / cooldown work.
	/// </summary>
	[StatusEffectHandler(StatusId.AutoGuard)]
	public class AutoGuardStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_DEVOTION — maintains the caster's linked-target list so the
	/// 5-link cap can be enforced and so death/disconnect can clear the
	/// bonds. The redirect itself lives in DevotionListener.
	/// </summary>
	[StatusEffectHandler(StatusId.Devotion)]
	public class DevotionStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var caster = effect.Caster;
			if (caster == null) return;
			if (!caster.DevotionLinkedTargets.Contains(target))
				caster.DevotionLinkedTargets.Add(target);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			effect.Caster?.DevotionLinkedTargets.Remove(target);
		}
	}
}
