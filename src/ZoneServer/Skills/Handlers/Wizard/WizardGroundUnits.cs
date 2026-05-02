using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.Wizard
{
	[SkillHandler(SkillId.WZ_FIREPILLAR)]
	public class FirePillarHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			caster.Map.AddSkillUnit(new FirePillarUnit(caster, pos, level));
			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}

	[SkillHandler(SkillId.WZ_FIREIVY)]
	public class FireIvyHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			caster.Map.AddSkillUnit(new FirePillarUnit(caster, pos, level));
			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}

	[SkillHandler(SkillId.WZ_ICEWALL)]
	public class IceWallHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var center = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			// 3-cell wide wall along an axis perpendicular to
			// caster→target. Approximated as 3 cells in a row on the
			// X-axis (orientation refinement is a follow-up).
			for (var dx = -1; dx <= 1; dx++)
			{
				var pos = new Position((short)(center.X + dx), center.Y);
				caster.Map.AddSkillUnit(new IceWallUnit(caster, pos, level));
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);
		}
	}

	[SkillHandler(SkillId.WZ_QUAGMIRE)]
	public class QuagmireHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var center = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			// 5x5 grid of Quagmire cells.
			for (var dx = -2; dx <= 2; dx++)
			{
				for (var dy = -2; dy <= 2; dy++)
				{
					var pos = new Position((short)(center.X + dx), (short)(center.Y + dy));
					caster.Map.AddSkillUnit(new QuagmireUnit(caster, pos, level));
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);
		}
	}

	[SkillHandler(SkillId.WZ_ESTIMATION)]
	public class EstimationHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc && target is Monster monster)
			{
				pc.ServerMessage($"{monster.Name}: HP {monster.Parameters.Hp}, Race {monster.Data.Race}, Element {monster.Data.Element} Lv {monster.Data.ElementLevel}, Size {monster.Data.Size}");
			}
		}
	}
}
