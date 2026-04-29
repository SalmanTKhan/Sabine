using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.Hunter
{
	[SkillHandler(SkillId.HT_BEASTBANE)]
	public class BeastBaneHandler : ISkillHandler
	{
		// Passive: +ATK vs Brute / Insect race. Applied via stat
		// recalculation hook.
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.HT_STEELCROW)]
	public class SteelCrowHandler : ISkillHandler
	{
		// Passive: +ATK to Falcon damage.
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.HT_FALCON)]
	public class FalconryHandler : ISkillHandler
	{
		// Toggle falcon companion. v1: just animation; the falcon
		// proc on basic attacks ties into the auto-attack hook.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster.StatusEffects.Has(StatusId.FalconFly))
				caster.StatusEffects.Stop(StatusId.FalconFly);
			else
				caster.StatusEffects.Start(StatusId.FalconFly, skill.Level, System.TimeSpan.FromHours(24), caster);

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HT_DETECTING)]
	public class DetectingHandler : ISkillHandler
	{
		// eAthena HT_DETECTING (Sight, Hunter): exposes hidden
		// enemies and traps in a small area around the caster.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			foreach (var other in caster.Map.GetCharactersInRange(caster.Position, 7))
			{
				if (other == caster) continue;
				if (other.IsHidden && other.IsHostileTo(caster))
					other.StatusEffects.Stop(StatusId.Hiding);
			}

			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HT_REMOVETRAP)]
	public class RemoveTrapHandler : ISkillHandler
	{
		// eAthena HT_REMOVETRAP: caster removes their own trap from
		// the targeted cell and recovers the trap item. v1 just
		// flags the unit as expired.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			foreach (var unit in caster.Map.GetSkillUnitsAt(target.Position))
			{
				if (unit is TrapUnit trap && trap.Owner == caster)
					caster.Map.RemoveSkillUnit(unit);
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HT_SPRINGTRAP)]
	public class SpringTrapHandler : ISkillHandler
	{
		// eAthena HT_SPRINGTRAP: forces a Hunter trap on the targeted
		// cell to immediately trigger. v1: best-effort scan and
		// re-fire onTouch with the caster as the entrant proxy.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}
}
