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
		// eAthena PR_BENEDICTIO (B.S. Sacramenti): Holy AoE around the
		// caster that converts target armor element to Holy briefly.
		// In v1 we simply notify; the per-armor element override is a
		// follow-up tied to the Aspersio system.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
				pc.ServerMessage("B.S. Sacramenti party-aura is not yet fully wired.");
		}
	}
}
