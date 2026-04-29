using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Monk
{
	[SkillHandler(SkillId.MO_CALLSPIRITS)]
	public class CallSpiritsHandler : ISkillHandler
	{
		// eAthena MO_CALLSPIRITS: charges spirit balls up to skill
		// level (max 5). v1 stores the count in the Spirits status'
		// val1; consumers (Finger Offensive, Asura, Explosion) read
		// it back.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			var current = caster.StatusEffects.TryGet(StatusId.Spirits, out var existing) ? existing.Val1 : 0;
			var balls = Math.Min(skill.Level, current + 1);
			caster.StatusEffects.Stop(StatusId.Spirits);
			caster.StatusEffects.Start(StatusId.Spirits, skill.Level, TimeSpan.FromMinutes(10), caster, balls);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MO_ABSORBSPIRITS)]
	public class AbsorbSpiritsHandler : ISkillHandler
	{
		// eAthena MO_ABSORBSPIRITS: absorb the target's spirit balls
		// (or coins) and convert into SP for the caster.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var balls = target.StatusEffects.TryGet(StatusId.Spirits, out var spirits) ? spirits.Val1 : 0;
			var sp = balls * 5;
			caster.Parameters.Modify(ParameterType.Sp, sp);
			if (balls > 0)
				target.StatusEffects.Stop(StatusId.Spirits);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, sp, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MO_EXPLOSIONSPIRITS)]
	public class ExplosionSpiritsHandler : BuffSkillHandler
	{
		// eAthena MO_EXPLOSIONSPIRITS: ATK/CRIT buff for 60s. Caps
		// spirit balls at 5; pre-Asura buff.
		protected override StatusId StatusId => StatusId.ExplosionSpirits;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60);
	}

	[SkillHandler(SkillId.MO_STEELBODY)]
	public class SteelBodyHandler : BuffSkillHandler
	{
		// eAthena MO_STEELBODY: greatly reduces incoming damage but
		// halves move/atk speed. 30s/level.
		protected override StatusId StatusId => StatusId.MentalStrength;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30 * level);
	}
}
