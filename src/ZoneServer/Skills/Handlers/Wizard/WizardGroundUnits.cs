using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.Wizard
{
	[SkillHandler(SkillId.WZ_FIREPILLAR)]
	public class FirePillarHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			caster.Map.AddSkillUnit(new FirePillarUnit(caster, target.Position, skill.Level));
			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_FIREIVY)]
	public class FireIvyHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			// Fire Ivy uses the same shape as Fire Pillar with a
			// shorter prime / longer touch window.
			caster.Map.AddSkillUnit(new FirePillarUnit(caster, target.Position, skill.Level));
			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_ICEWALL)]
	public class IceWallHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			// 3-cell wide wall along an axis perpendicular to
			// caster→target. Approximated as 3 cells in a row on the
			// X-axis (orientation refinement is a follow-up).
			var center = target.Position;
			for (var dx = -1; dx <= 1; dx++)
			{
				var pos = new Sabine.Shared.World.Position((short)(center.X + dx), center.Y);
				caster.Map.AddSkillUnit(new IceWallUnit(caster, pos, skill.Level));
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, center.X, center.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_QUAGMIRE)]
	public class QuagmireHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			// 5x5 grid of Quagmire cells.
			var center = target.Position;
			for (var dx = -2; dx <= 2; dx++)
			{
				for (var dy = -2; dy <= 2; dy++)
				{
					var pos = new Sabine.Shared.World.Position((short)(center.X + dx), (short)(center.Y + dy));
					caster.Map.AddSkillUnit(new QuagmireUnit(caster, pos, skill.Level));
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, center.X, center.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_ESTIMATION)]
	public class EstimationHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc && target is Monster monster)
			{
				pc.ServerMessage($"{monster.Name}: HP {monster.Parameters.Hp}, Race {monster.Data.Race}, Element {monster.Data.Element} Lv {monster.Data.ElementLevel}, Size {monster.Data.Size}");
			}

			return Task.CompletedTask;
		}
	}
}
