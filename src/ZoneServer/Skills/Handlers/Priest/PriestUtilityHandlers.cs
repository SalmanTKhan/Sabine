using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Priest
{
	[SkillHandler(SkillId.PR_SLOWPOISON)]
	public class SlowPoisonHandler : ITargetedSkillHandler
	{
		// eAthena PR_SLOWPOISON: pauses Poison DoT damage on the
		// target without dispelling it. Approximation: remove poison
		// if level >=4.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (level >= 4 && target.StatusEffects.Has(StatusId.Poison))
				target.StatusEffects.Stop(StatusId.Poison);
		}
	}

	[SkillHandler(SkillId.PR_STRECOVERY)]
	public class StatusRecoveryHandler : ITargetedSkillHandler
	{
		// eAthena PR_STRECOVERY (Status Recovery): cures Stone, Freeze,
		// Stun, Sleep on a friendly target. Inflicts Blind on undead.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (IsUndead(target))
			{
				target.StatusEffects.Start(StatusId.Blind, 1, TimeSpan.FromSeconds(30 * level), caster);
				return;
			}

			foreach (var sid in new[] { StatusId.Stone, StatusId.Freeze, StatusId.Stun, StatusId.Sleep })
			{
				if (target.StatusEffects.Has(sid))
					target.StatusEffects.Stop(sid);
			}
		}

		private static bool IsUndead(Character target)
		{
			if (target is not Monster monster) return false;
			return monster.Data.Element == ElementType.Undead || monster.Data.Race == RaceType.Undead;
		}
	}

	[SkillHandler(SkillId.PR_BENEDICTIO)]
	public class BenedictioHandler : ITargetedSkillHandler
	{
		// eAthena PR_BENEDICTIO (B.S. Sacramenti): applies SC_BENEDICTIO
		// to the caster and every party member within 5 cells, granting
		// Holy resist and +MaxHP for 60s. The per-armor element override
		// is the Aspersio system on the offensive side; here we apply
		// the defensive payload.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var duration = TimeSpan.FromSeconds(60);
			caster.StatusEffects.Start(StatusId.BSSacramenti, level, duration, caster);

			foreach (var c in caster.Map.GetCharactersInRange(caster.Position, 5))
			{
				if (c == caster) continue;
				if (c.IsHostileTo(caster)) continue;
				c.StatusEffects?.Start(StatusId.BSSacramenti, level, duration, caster);
			}
		}
	}
}
