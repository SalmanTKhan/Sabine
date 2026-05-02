using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.HomunculusSkills
{
	// Lif --------------------------------------------------------------

	[SkillHandler(SkillId.HLIF_AVOID)]
	public class UrgentEscapeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is Homunculus homun)
			{
				var dur = TimeSpan.FromSeconds(10 + 5 * level);
				homun.StatusEffects.Start(StatusId.IncreaseAgi, level, dur, caster);
				homun.Owner?.StatusEffects.Start(StatusId.IncreaseAgi, level, dur, caster);
			}
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.HLIF_BRAIN)]
	public class BrainSurgeryHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.HLIF_CHANGE)]
	public class MentalChargeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.Concentration, level, TimeSpan.FromSeconds(60 + 30 * level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	// Amistr ----------------------------------------------------------

	[SkillHandler(SkillId.HAMI_DEFENCE)]
	public class AmistrAdamantiumHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.Defender, level, TimeSpan.FromSeconds(30 * level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.HAMI_SKIN)]
	public class AmistrTuffSkinHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.HAMI_BLOODLUST)]
	public class AmistrBloodlustHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.TwoHandQuicken, level, TimeSpan.FromSeconds(30 + 30 * level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	// Filir ----------------------------------------------------------

	[SkillHandler(SkillId.HFLI_MOON)]
	public class FilirMoonlightHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.50f * level,
				HitCount = 3,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss) target.TakeDamage(result.Damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 3, result.ActionType);
		}
	}

	[SkillHandler(SkillId.HFLI_FLEET)]
	public class FilirFleetingMoveHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.TwoHandQuicken, level, TimeSpan.FromSeconds(30 * level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.HFLI_SPEED)]
	public class FilirOverSpeedHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.IncreaseAgi, level, TimeSpan.FromSeconds(30 * level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.HFLI_SBR44)]
	public class FilirSBR44Handler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not Homunculus homun) return;
			if (target == null) return;

			var damage = homun.State.Intimacy * 10;
			target.TakeDamage(damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 1, ActionType.Skill);

			homun.Map?.RemoveNpc(homun);
			homun.State.Entity = null;
		}
	}

	// Vanilmirth -----------------------------------------------------

	[SkillHandler(SkillId.HVAN_CHAOTIC)]
	public class ChaoticBlessingsHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not Homunculus homun) return;

			var roll = RandomProvider.Get().Next(100);
			Character recipient = roll < 30 ? homun
				: roll < 80 ? (Character)homun.Owner
				: target ?? homun;

			var heal = 50 + 100 * level;
			recipient.HealHp(heal);
			if (recipient is PlayerCharacter pc) Send.ZC_RECOVERY(pc, ParameterType.Hp, heal);
			Send.ZC_NOTIFY_SKILL(caster, recipient.Handle, skill.Id, level, heal, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.HVAN_INSTRUCT)]
	public class InstructionChangeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is Homunculus homun)
				homun.StatusEffects.Start(StatusId.Blessing, level, TimeSpan.FromSeconds(60 + 30 * level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.HVAN_EXPLOSION)]
	public class BioExplosionHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not Homunculus homun) return;

			var ratio = 5.0f + 1.0f * level;
			foreach (var enemy in homun.Map.GetCharactersInRange(homun.Position, 3))
			{
				if (!enemy.IsHostileTo(homun)) continue;
				var ctx = new AttackContext(homun, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Neutral,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss) enemy.TakeDamage(result.Damage, homun);
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			homun.Map?.RemoveNpc(homun);
			homun.State.Entity = null;
		}
	}
}
