using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>BD_DRUMBATTLEFIELD ("Drum on the Battlefield"): +25*level
	/// ATK and +5*level Hit for party members in the aura. eAthena
	/// status.c:5664 stores val1=ATK, val2=Hit deltas.</summary>
	[StatusEffectHandler(StatusId.DrumBattle)]
	public class DrumBattleStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var atk = 25 * effect.Level;
			var hit = 5 * effect.Level;
			effect.Val2 = atk;
			effect.Val3 = hit;
			target.AddBonusStat(ParameterType.Attack, atk);
			target.AddBonusStat(ParameterType.Hit, hit);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Attack, -effect.Val2);
			if (effect.Val3 != 0)
				target.AddBonusStat(ParameterType.Hit, -effect.Val3);
		}
	}

	/// <summary>BD_ETERNALCHAOS ("Eternal Chaos"): enemy debuff. eAthena
	/// status.c:5670 zeroes the target's DEF outright by stamping
	/// SCB_DEF on the recalc; here we approximate with a deep DEF cut
	/// (-15*level) that lasts only while the enemy is in the aura.</summary>
	[StatusEffectHandler(StatusId.EternalChaos)]
	public class EternalChaosStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var defCut = 15 * effect.Level;
			effect.Val2 = defCut;
			target.AddBonusStat(ParameterType.MeleeDefenseBonus, -defCut);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.MeleeDefenseBonus, effect.Val2);
		}
	}

	/// <summary>BD_RINGNIBELUNGEN ("The Ring of the Nibelungen"): +50*level
	/// weapon ATK while wielding a two-handed weapon. eAthena status.c:5666
	/// gates on weapon type; Sabine skips the gate today since the bonus
	/// rides through the AddBonusStat pipeline either way.</summary>
	[StatusEffectHandler(StatusId.RingNibelungen)]
	public class RingNibelungenStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var atk = 50 * effect.Level;
			effect.Val2 = atk;
			target.AddBonusStat(ParameterType.Attack, atk);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Attack, -effect.Val2);
		}
	}

	/// <summary>BD_INTOABYSS ("Into the Abyss"): -50% SP cost on skill
	/// casts for party members in the aura. The actual SP-cost gate
	/// lives in <c>PacketHandler.TryDeductSkillCost</c>; this handler
	/// is a marker so the status icon broadcasts.</summary>
	[StatusEffectHandler(StatusId.IntoAbyss)]
	public class IntoAbyssStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = 50; // percent SP-cost reduction
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>BD_SIEGFRIED ("Invulnerable Siegfried"): grants element
	/// resistances to elemental damage. eAthena classic adds +20+5*level%
	/// resist to every non-Neutral element for party members in the aura.</summary>
	[StatusEffectHandler(StatusId.Siegfried)]
	public class SiegfriedStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var resist = 20 + 5 * effect.Level;
			effect.Val2 = resist;
			target.Modifiers.AddSubElementAllExceptNeutral(resist);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 == 0) return;
			target.Modifiers.AddSubElementAllExceptNeutral(-effect.Val2);
		}
	}
}
