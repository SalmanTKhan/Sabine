using System;
using System.Threading.Tasks;
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
	public abstract class BuffSkillHandler : ISkillHandler
	{
		protected abstract StatusId StatusId { get; }

		protected abstract TimeSpan GetDuration(int level);

		protected virtual (int v1, int v2, int v3, int v4) GetVals(int level)
			=> (level, 0, 0, 0);

		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return Task.CompletedTask;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			var duration = this.GetDuration(skill.Level);
			var (v1, v2, v3, v4) = this.GetVals(skill.Level);

			targetCharacter.StatusEffects.Start(this.StatusId, skill.Level, duration, caster, v1, v2, v3, v4);

			return Task.CompletedTask;
		}
	}
}
