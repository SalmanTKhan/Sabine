using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_ENERGYCOAT)]
	public class EnergyCoatHandler : ITargetedSkillHandler
	{
		// eAthena MG_ENERGYCOAT: while active, incoming physical damage
		// is reduced based on current SP%, at the cost of additional SP
		// per hit. Toggle skill — recasting cancels.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster.StatusEffects.Has(StatusId.EnergyCoat))
			{
				caster.StatusEffects.Stop(StatusId.EnergyCoat);
				return;
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			caster.StatusEffects.Start(StatusId.EnergyCoat, level, TimeSpan.FromMinutes(30), caster);
		}
	}
}
