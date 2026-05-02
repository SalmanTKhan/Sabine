using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.Skills.StatusEffects;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers
{
	/// <summary>
	/// Base class for skills that primarily apply a single status effect
	/// to the target. Subclasses provide the StatusId, per-level duration,
	/// and any val arguments. Per-level durations follow the eAthena
	/// classic skill_cast_db.txt arrays.
	/// </summary>
	public abstract class BuffSkillHandler : ITargetedSkillHandler
	{
		protected abstract StatusId StatusId { get; }

		protected abstract TimeSpan GetDuration(int level);

		protected virtual (int v1, int v2, int v3, int v4) GetVals(int level)
			=> (level, 0, 0, 0);

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var duration = this.GetDuration(level);
			var (v1, v2, v3, v4) = this.GetVals(level);

			target.StatusEffects.Start(this.StatusId, level, duration, caster, v1, v2, v3, v4);
		}
	}
}
