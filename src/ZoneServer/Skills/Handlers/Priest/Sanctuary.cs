using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Priest
{
	[SkillHandler(SkillId.PR_SANCTUARY)]
	public class SanctuaryHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var unit = new Sabine.Zone.World.Maps.SkillUnits.SanctuaryUnit(caster, pos, level);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}

	[SkillHandler(SkillId.PR_MAGNUS)]
	public class MagnusExorcismusHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var center = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			// 5x5 grid of MagnusUnits centered on the target.
			for (var dx = -2; dx <= 2; dx++)
			{
				for (var dy = -2; dy <= 2; dy++)
				{
					var pos = new Position((short)(center.X + dx), (short)(center.Y + dy));
					var unit = new Sabine.Zone.World.Maps.SkillUnits.MagnusUnit(caster, pos, level);
					caster.Map.AddSkillUnit(unit);
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);
		}
	}
}
