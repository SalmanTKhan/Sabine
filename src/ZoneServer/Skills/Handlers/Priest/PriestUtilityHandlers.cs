using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Priest
{
	[SkillHandler(SkillId.PR_SLOWPOISON)]
	public class SlowPoisonHandler : ISkillHandler
	{
		// eAthena PR_SLOWPOISON: pauses Poison DoT damage on the
		// target without dispelling it. Implemented as a duration
		// shorten of the existing Poison status by 1/(level+1).
		// Approximation in v1: simply remove poison if level >=4.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter) return Task.CompletedTask;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (skill.Level >= 4 && targetCharacter.StatusEffects.Has(StatusId.Poison))
				targetCharacter.StatusEffects.Stop(StatusId.Poison);

			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.PR_STRECOVERY)]
	public class StatusRecoveryHandler : ISkillHandler
	{
		// eAthena PR_STRECOVERY (Status Recovery): cures Stone, Freeze,
		// Stun, Sleep on a friendly target. Inflicts Blind on undead.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter) return Task.CompletedTask;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (IsUndead(targetCharacter))
			{
				targetCharacter.StatusEffects.Start(StatusId.Blind, 1, TimeSpan.FromSeconds(30 * skill.Level), caster);
				return Task.CompletedTask;
			}

			foreach (var sid in new[] { StatusId.Stone, StatusId.Freeze, StatusId.Stun, StatusId.Sleep })
			{
				if (targetCharacter.StatusEffects.Has(sid))
					targetCharacter.StatusEffects.Stop(sid);
			}

			return Task.CompletedTask;
		}

		private static bool IsUndead(Character target)
		{
			if (target is not Monster monster) return false;
			return monster.Data.Element == ElementType.Undead || monster.Data.Race == RaceType.Undead;
		}
	}

	[SkillHandler(SkillId.PR_BENEDICTIO)]
	public class BenedictioHandler : ISkillHandler
	{
		// eAthena PR_BENEDICTIO (B.S. Sacramenti): Holy AoE around the
		// caster that converts target armor element to Holy briefly.
		// In v1 we simply notify; the per-armor element override is a
		// follow-up tied to the Aspersio system.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
				pc.ServerMessage("B.S. Sacramenti party-aura is not yet fully wired.");

			return Task.CompletedTask;
		}
	}
}
