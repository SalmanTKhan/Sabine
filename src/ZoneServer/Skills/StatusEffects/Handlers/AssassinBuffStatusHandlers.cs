using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_ENCPOISON (AS_ENCHANTPOISON) — overrides the wearer's weapon
	/// element to Poison for the duration. Pure flag handler; the swap is
	/// applied by
	/// <see cref="Sabine.Zone.Battle.BattleCalculator.ResolveAttackElement"/>
	/// alongside the other Endow / Aspersio overrides. eAthena status.c
	/// SC_ENCPOISON additionally carries a poison-on-hit chance (val2 =
	/// 250 + 50*level / 10000), which is left for a follow-up once the
	/// on-hit-effect engine is in place.
	/// </summary>
	[StatusEffectHandler(StatusId.EnchantPoison)]
	public class EnchantPoisonStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_POISONREACT (AS_POISONREACT) — counters incoming attacks with
	/// Envenom (TF_POISON) at <see cref="StatusEffect.Val3"/>% chance, or
	/// auto-counters with the dedicated AS_POISONREACT hit when the
	/// attacker's defense element is Poison. Mirrors eAthena status.c
	/// SC_POISONREACT init: val2 = (val1+1)/2 + val1/10 counters,
	/// val3 = 50 chance%. The counter logic itself lives in
	/// <see cref="Sabine.Zone.Battle.Listeners.PoisonReactListener"/>;
	/// this handler just snapshots the counters and chance so refresh
	/// casts re-roll them cleanly.
	/// </summary>
	[StatusEffectHandler(StatusId.PoisonReact)]
	public class PoisonReactStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = (effect.Level + 1) / 2 + effect.Level / 10;
			effect.Val3 = 50;
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
