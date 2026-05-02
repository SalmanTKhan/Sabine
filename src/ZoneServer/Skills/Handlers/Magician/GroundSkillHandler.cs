using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_FIREWALL)]
	public class FireWallHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			// Spawn a single Fire Wall cell at the target position. Classic
			// Fire Wall is a 3-cell-wide wall facing the caster, but the
			// orientation math (perpendicular to caster→target) is its
			// own follow-up; v1 ships a 1-cell version.
			var unit = new Sabine.Zone.World.Maps.SkillUnits.FireWallUnit(caster, pos, level);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}

	[SkillHandler(SkillId.MG_SAFETYWALL)]
	public class SafetyWallHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var unit = new Sabine.Zone.World.Maps.SkillUnits.SafetyWallUnit(caster, pos, level);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}

	[SkillHandler(SkillId.MG_THUNDERSTORM)]
	public class ThunderStormHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var center = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			// Spawn a 3x3 cross of Wind cells centred on the target. Each
			// cell hits enemies that step onto it. The cross gives most
			// of Thunder Storm's footprint without the full 5x5 cost.
			var offsets = new (int dx, int dy)[]
			{
				(  0,  0 ),
				( -1,  0 ), (  1,  0 ),
				(  0, -1 ), (  0,  1 ),
				( -1, -1 ), (  1, -1 ), ( -1,  1 ), (  1,  1 ),
			};
			foreach (var (dx, dy) in offsets)
			{
				var pos = new Position(center.X + dx, center.Y + dy);
				var unit = new Sabine.Zone.World.Maps.SkillUnits.ThunderStormUnit(caster, pos, level);
				caster.Map.AddSkillUnit(unit);
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);
		}
	}
}
