using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_QUAGMIRE (WZ_QUAGMIRE) — slows enemies standing on the unit
	/// and drains AGI/DEX. eAthena formula (status.c:5884): per-level
	/// penalty is 5*lvl on players, 10*lvl on monsters. Quagmire also
	/// blocks IncreaseAgi-class buffs from being recast — those are
	/// stripped on application.
	/// </summary>
	[StatusEffectHandler(StatusId.Quagmire)]
	public class QuagmireStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var perLevel = target is PlayerCharacter ? 5 : 10;
			var penalty = perLevel * effect.Level;
			effect.Val2 = penalty;

			// Quagmire dispels the AGI/quicken family on entry, mirroring
			// eAthena's status_change_end cascade (status.c:5036).
			if (target.StatusEffects.Has(StatusId.IncreaseAgi))
				target.StatusEffects.Stop(StatusId.IncreaseAgi);
			if (target.StatusEffects.Has(StatusId.TwoHandQuicken))
				target.StatusEffects.Stop(StatusId.TwoHandQuicken);
			if (target.StatusEffects.Has(StatusId.SpearQuicken))
				target.StatusEffects.Stop(StatusId.SpearQuicken);
			if (target.StatusEffects.Has(StatusId.Concentration))
				target.StatusEffects.Stop(StatusId.Concentration);

			if (penalty != 0)
			{
				target.Parameters.Modify(ParameterType.Agi, -penalty);
				target.Parameters.Modify(ParameterType.Dex, -penalty);
			}
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 == 0)
				return;

			target.Parameters.Modify(ParameterType.Agi, effect.Val2);
			target.Parameters.Modify(ParameterType.Dex, effect.Val2);
		}
	}
}
