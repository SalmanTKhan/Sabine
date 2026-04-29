using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_ENERGYCOAT)]
	public class EnergyCoatHandler : ISkillHandler
	{
		// eAthena MG_ENERGYCOAT: while active, incoming physical damage
		// is reduced based on current SP%, at the cost of additional SP
		// per hit. Toggle skill — recasting cancels.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster.StatusEffects.Has(StatusId.EnergyCoat))
			{
				caster.StatusEffects.Stop(StatusId.EnergyCoat);
				return Task.CompletedTask;
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			caster.StatusEffects.Start(StatusId.EnergyCoat, skill.Level, TimeSpan.FromMinutes(30), caster);
			return Task.CompletedTask;
		}
	}
}
