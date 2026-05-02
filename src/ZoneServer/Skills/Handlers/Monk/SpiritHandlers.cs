using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Monk
{
	[SkillHandler(SkillId.MO_CALLSPIRITS)]
	public class CallSpiritsHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var current = caster.StatusEffects.TryGet(StatusId.Spirits, out var existing) ? existing.Val1 : 0;
			var balls = Math.Min(level, current + 1);
			caster.StatusEffects.Stop(StatusId.Spirits);
			caster.StatusEffects.Start(StatusId.Spirits, level, TimeSpan.FromMinutes(10), caster, balls);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.MO_ABSORBSPIRITS)]
	public class AbsorbSpiritsHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var balls = target.StatusEffects.TryGet(StatusId.Spirits, out var spirits) ? spirits.Val1 : 0;
			var sp = balls * 5;
			caster.Parameters.Modify(ParameterType.Sp, sp);
			if (balls > 0)
				target.StatusEffects.Stop(StatusId.Spirits);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, sp, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.MO_EXPLOSIONSPIRITS)]
	public class ExplosionSpiritsHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.ExplosionSpirits;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60);
	}

	[SkillHandler(SkillId.MO_STEELBODY)]
	public class SteelBodyHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.MentalStrength;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30 * level);
	}
}
