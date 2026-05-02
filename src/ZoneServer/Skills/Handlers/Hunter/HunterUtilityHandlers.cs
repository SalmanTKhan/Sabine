using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.Hunter
{
	[SkillHandler(SkillId.HT_BEASTBANE)]
	public class BeastBaneHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}

	[SkillHandler(SkillId.HT_STEELCROW)]
	public class SteelCrowHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}

	[SkillHandler(SkillId.HT_FALCON)]
	public class FalconryHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster.StatusEffects.Has(StatusId.FalconFly))
				caster.StatusEffects.Stop(StatusId.FalconFly);
			else
				caster.StatusEffects.Start(StatusId.FalconFly, level, TimeSpan.FromHours(24), caster);

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.HT_DETECTING)]
	public class DetectingHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			foreach (var other in caster.Map.GetCharactersInRange(caster.Position, 7))
			{
				if (other == caster) continue;
				if (other.IsHidden && other.IsHostileTo(caster))
					other.StatusEffects.Stop(StatusId.Hiding);
			}
		}
	}

	[SkillHandler(SkillId.HT_REMOVETRAP)]
	public class RemoveTrapHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			foreach (var unit in caster.Map.GetSkillUnitsAt(pos))
			{
				if (unit is TrapUnit trap && trap.Owner == caster)
					caster.Map.RemoveSkillUnit(unit);
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.HT_SPRINGTRAP)]
	public class SpringTrapHandler : IGroundSkillHandler
	{
		// eAthena HT_SPRINGTRAP: forces a Hunter trap on the targeted
		// cell to immediately trigger. The trap picks the nearest
		// hostile in range as its victim; the trap is consumed
		// regardless. Friendly fire and dud cells are no-ops.
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			foreach (var unit in caster.Map.GetSkillUnitsAt(pos))
			{
				if (unit is TrapUnit trap)
				{
					trap.Spring();
					caster.Map.RemoveSkillUnit(unit);
				}
			}
		}
	}
}
