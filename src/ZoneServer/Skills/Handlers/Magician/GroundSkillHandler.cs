using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	/// <summary>
	/// Generic ground-skill stub used by skills that don't yet have a
	/// SkillUnit implementation. Specialised handlers (FireWall,
	/// SafetyWall, Pneuma) spawn real units; the rest just play the
	/// visual.
	/// </summary>
	public abstract class GroundSkillHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);

			if (caster is PlayerCharacter pc)
				pc.ServerMessage($"{skill.Id} is not fully implemented yet.");

			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MG_FIREWALL)]
	public class FireWallHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			// Spawn a single Fire Wall cell at the target position. Classic
			// Fire Wall is a 3-cell-wide wall facing the caster, but the
			// orientation math (perpendicular to caster→target) is its
			// own follow-up; v1 ships a 1-cell version.
			var unit = new Sabine.Zone.World.Maps.SkillUnits.FireWallUnit(caster, target.Position, skill.Level);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MG_SAFETYWALL)]
	public class SafetyWallHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var unit = new Sabine.Zone.World.Maps.SkillUnits.SafetyWallUnit(caster, target.Position, skill.Level);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MG_THUNDERSTORM)]
	public class ThunderStormHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			// Spawn a 3x3 cross of Wind cells centred on the target. Each
			// cell hits enemies that step onto it. The cross gives most
			// of Thunder Storm's footprint without the full 5x5 cost.
			var center = target.Position;
			var offsets = new (int dx, int dy)[]
			{
				(  0,  0 ),
				( -1,  0 ), (  1,  0 ),
				(  0, -1 ), (  0,  1 ),
				( -1, -1 ), (  1, -1 ), ( -1,  1 ), (  1,  1 ),
			};
			foreach (var (dx, dy) in offsets)
			{
				var pos = new Sabine.Shared.World.Position(center.X + dx, center.Y + dy);
				var unit = new Sabine.Zone.World.Maps.SkillUnits.ThunderStormUnit(caster, pos, skill.Level);
				caster.Map.AddSkillUnit(unit);
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}
}
