using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Priest
{
	[SkillHandler(SkillId.PR_SANCTUARY)]
	public class SanctuaryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var unit = new Sabine.Zone.World.Maps.SkillUnits.SanctuaryUnit(caster, target.Position, skill.Level);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.PR_MAGNUS)]
	public class MagnusExorcismusHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			// 5x5 grid of MagnusUnits centered on the target.
			var center = target.Position;
			for (var dx = -2; dx <= 2; dx++)
			{
				for (var dy = -2; dy <= 2; dy++)
				{
					var pos = new Sabine.Shared.World.Position((short)(center.X + dx), (short)(center.Y + dy));
					var unit = new Sabine.Zone.World.Maps.SkillUnits.MagnusUnit(caster, pos, skill.Level);
					caster.Map.AddSkillUnit(unit);
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, center.X, center.Y, 0);
			return Task.CompletedTask;
		}
	}
}
