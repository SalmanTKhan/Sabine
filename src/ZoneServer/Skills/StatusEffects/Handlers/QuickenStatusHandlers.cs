using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_TWOHANDQUICKEN (KN_TWOHANDQUICKEN) — flat 30% ASPD bonus while
	/// active. eAthena (status.c:5407) sets val2 = 300, which scales the
	/// aspd_rate by +30%. Mapped here onto Sabine's AspdBonus parameter,
	/// where positive values reduce attack delay.
	/// </summary>
	[StatusEffectHandler(StatusId.TwoHandQuicken)]
	public class TwoHandQuickenStatusHandler : IStatusEffectHandler
	{
		private const int AspdBonus = 30;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = AspdBonus;
			target.Parameters.Modify(ParameterType.AspdBonus, AspdBonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Parameters.Modify(ParameterType.AspdBonus, -effect.Val2);
		}
	}

	/// <summary>
	/// SC_SPEARQUICKEN (KN_SPEARQUICKEN, used by Crusader's spear path).
	/// eAthena (status.c:5417): val2 = 200 + 10*level, i.e. 20 + level%
	/// faster ASPD. Mapped onto AspdBonus.
	/// </summary>
	[StatusEffectHandler(StatusId.SpearQuicken)]
	public class SpearQuickenStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var bonus = 20 + effect.Level;
			effect.Val2 = bonus;
			target.Parameters.Modify(ParameterType.AspdBonus, bonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.Parameters.Modify(ParameterType.AspdBonus, -effect.Val2);
		}
	}

	/// <summary>
	/// SC_DEFENDER (CR_DEFENDER) — Crusader stance: long-range physical
	/// damage taken is reduced by val2 = 5 + 15*level percent (eAthena
	/// status.c:5613). The damage-reduction half is consumed inside
	/// <see cref="Sabine.Zone.Battle.BattleCalculator"/>; this handler
	/// just snapshots val2 and applies the ASPD slowdown:
	/// aspd_rate += 250 - 50*val1, i.e. (25 - 5*level)% slower attacks.
	/// At level 5 the ASPD penalty disappears entirely.
	/// </summary>
	[StatusEffectHandler(StatusId.Defender)]
	public class DefenderStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = 5 + 15 * effect.Level;
			effect.Val4 = 25 - 5 * effect.Level;

			if (effect.Val4 > 0)
				target.Parameters.Modify(ParameterType.AspdBonus, -effect.Val4);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val4 > 0)
				target.Parameters.Modify(ParameterType.AspdBonus, effect.Val4);
		}
	}
}
