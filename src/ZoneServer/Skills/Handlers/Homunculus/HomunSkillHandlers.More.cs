using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.HomunculusSkills
{
	// Lif --------------------------------------------------------------

	[SkillHandler(SkillId.HLIF_AVOID)]
	public class UrgentEscapeHandler : ISkillHandler
	{
		// eAthena HLIF_AVOID: Lif and owner gain a brief move-speed
		// burst (modeled here as IncreaseAgi for simplicity).
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is Homunculus homun)
			{
				var dur = TimeSpan.FromSeconds(10 + 5 * skill.Level);
				homun.StatusEffects.Start(StatusId.IncreaseAgi, skill.Level, dur, caster);
				homun.Owner?.StatusEffects.Start(StatusId.IncreaseAgi, skill.Level, dur, caster);
			}
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HLIF_BRAIN)]
	public class BrainSurgeryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.HLIF_CHANGE)]
	public class MentalChargeHandler : ISkillHandler
	{
		// eAthena HLIF_CHANGE: temporary INT/MATK boost for the
		// homun. v1 applies Concentration as a placeholder buff.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.Concentration, skill.Level, TimeSpan.FromSeconds(60 + 30 * skill.Level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	// Amistr ----------------------------------------------------------

	[SkillHandler(SkillId.HAMI_DEFENCE)]
	public class AmistrAdamantiumHandler : ISkillHandler
	{
		// eAthena HAMI_DEFENCE: large DEF boost, halves move-speed.
		// Modeled as Defender status (ranged-physical reduction +
		// move-speed cost) for v1.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.Defender, skill.Level, TimeSpan.FromSeconds(30 * skill.Level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HAMI_SKIN)]
	public class AmistrTuffSkinHandler : ISkillHandler
	{
		// Passive: bonus DEF / damage reduction. Stat-recalc hook.
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.HAMI_BLOODLUST)]
	public class AmistrBloodlustHandler : ISkillHandler
	{
		// eAthena HAMI_BLOODLUST: ATK/ASPD buff, lifesteal on hit.
		// v1 applies Two-Hand Quicken as ASPD placeholder.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.TwoHandQuicken, skill.Level, TimeSpan.FromSeconds(30 + 30 * skill.Level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	// Filir ----------------------------------------------------------

	[SkillHandler(SkillId.HFLI_MOON)]
	public class FilirMoonlightHandler : ISkillHandler
	{
		// eAthena HFLI_MOON: 3-hit physical, 100%+50%*lv per hit.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.50f * skill.Level,
				HitCount = 3,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss) target.TakeDamage(result.Damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 3, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HFLI_FLEET)]
	public class FilirFleetingMoveHandler : ISkillHandler
	{
		// eAthena HFLI_FLEET: ASPD buff for Filir.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.TwoHandQuicken, skill.Level, TimeSpan.FromSeconds(30 * skill.Level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HFLI_SPEED)]
	public class FilirOverSpeedHandler : ISkillHandler
	{
		// eAthena HFLI_SPEED: move-speed buff.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.IncreaseAgi, skill.Level, TimeSpan.FromSeconds(30 * skill.Level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HFLI_SBR44)]
	public class FilirSBR44Handler : ISkillHandler
	{
		// eAthena HFLI_SBR44 (S.B.R.44): Filir self-destructs,
		// dealing intimacy*10000 damage to the target. Resets
		// intimacy to 0 (homun runs away). Ratio adjusted for v1
		// since intimacy max is 1000 internally.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not Homunculus homun) return Task.CompletedTask;
			if (target == null) return Task.CompletedTask;

			var damage = homun.State.Intimacy * 10;
			target.TakeDamage(damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 1, ActionType.Skill);

			// Self-destruct: bond breaks, entity removed.
			homun.Map?.RemoveNpc(homun);
			homun.State.Entity = null;
			return Task.CompletedTask;
		}
	}

	// Vanilmirth -----------------------------------------------------

	[SkillHandler(SkillId.HVAN_CHAOTIC)]
	public class ChaoticBlessingsHandler : ISkillHandler
	{
		// eAthena HVAN_CHAOTIC: heals one of {homun, owner, random
		// enemy} chosen at random, with skill-level-scaled chances.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not Homunculus homun) return Task.CompletedTask;

			var roll = RandomProvider.Get().Next(100);
			Character recipient = roll < 30 ? homun
				: roll < 80 ? (Character)homun.Owner
				: target ?? homun;

			var heal = 50 + 100 * skill.Level;
			recipient.HealHp(heal);
			if (recipient is PlayerCharacter pc) Send.ZC_RECOVERY(pc, ParameterType.Hp, heal);
			Send.ZC_NOTIFY_SKILL(caster, recipient.Handle, skill.Id, skill.Level, heal, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HVAN_INSTRUCT)]
	public class InstructionChangeHandler : ISkillHandler
	{
		// eAthena HVAN_INSTRUCT: stat-bonus buff for homun (STR/INT
		// up). Modeled as Blessing for v1.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.Blessing, skill.Level, TimeSpan.FromSeconds(60 + 30 * skill.Level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.HVAN_EXPLOSION)]
	public class BioExplosionHandler : ISkillHandler
	{
		// eAthena HVAN_EXPLOSION: AoE Neutral magic blast around
		// Vanilmirth, then it self-destructs.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not Homunculus homun) return Task.CompletedTask;

			var ratio = 5.0f + 1.0f * skill.Level;
			foreach (var enemy in homun.Map.GetCharactersInRange(homun.Position, 3))
			{
				if (!enemy.IsHostileTo(homun)) continue;
				var ctx = new AttackContext(homun, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Neutral,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss) enemy.TakeDamage(result.Damage, homun);
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			homun.Map?.RemoveNpc(homun);
			homun.State.Entity = null;
			return Task.CompletedTask;
		}
	}
}
