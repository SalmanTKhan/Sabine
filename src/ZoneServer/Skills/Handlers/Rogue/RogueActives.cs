using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Rogue
{
	[SkillHandler(SkillId.RG_STEALCOIN)]
	public class StealCoinHandler : ITargetedSkillHandler
	{
		// eAthena pc_steal_coin (pc.c): rate is out of 1000 and combines
		// skill level, base-level delta, and the rogue's Dex/Luk; amount
		// is the mob's level scaled with a small random padding. MVP mobs
		// and already-coined mobs are skipped. On success the mob aggros
		// the rogue, mirroring the mob_target call in skill.c.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, target?.Handle ?? caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (caster is not PlayerCharacter pc)
				return;

			if (target is not Monster mob || mob.IsDead)
				return;

			if (mob.StolenCoin || mob.Data.MvpExp > 0)
				return;

			var casterParams = pc.Parameters;
			var rate = level * 10
				+ (casterParams.BaseLevel - mob.Parameters.BaseLevel) * 3
				+ casterParams.Dex * 2
				+ casterParams.Luk * 2;

			var rnd = RandomProvider.Get();
			if (rate <= 0 || rnd.Next(1000) >= rate)
				return;

			var amount = mob.Parameters.BaseLevel * 10 + rnd.Next(100);
			if (amount <= 0)
				return;

			pc.Parameters.Modify(ParameterType.Zeny, amount);
			mob.StolenCoin = true;

			// Successful theft draws aggro, matching eAthena's mob_target
			// call from skill_castend_nodamage_id.
			mob.AttackerHandleTest = pc.Handle;
		}
	}

	[SkillHandler(SkillId.RG_BACKSTAP)]
	public class BackstabHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var ratio = BattleCalculator.IsRenewal()
				? 3.0f + 0.40f * level
				: 3.4f + 0.40f * level;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				WeaponRequired = true,
				IgnoreFlee = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, result.HitCount, result.ActionType);
		}
	}

	[SkillHandler(SkillId.RG_RAID)]
	public class RaidHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			caster.StatusEffects.Stop(StatusId.Hiding);

			var ratio = 1.0f + 0.40f * level;
			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, 2))
			{
				if (enemy == caster) continue;
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Physical,
					SkillRatio = ratio,
					WeaponRequired = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					enemy.TakeDamage(result.Damage, caster);
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.RG_INTIMIDATE)]
	public class IntimidateHandler : ITargetedSkillHandler
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
				SkillRatio = 1.0f + 0.30f * level,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			var dest = caster.Map.GetRandomWalkablePosition();
			caster.Warp(new Sabine.Shared.World.Location(caster.Map.Id, dest));

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	[SkillHandler(SkillId.RG_GRAFFITI)]
	public class GraffitiHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			if (parameters.Character is PlayerCharacter pc) pc.ServerMessage("Graffiti placement is not yet implemented.");
		}
	}

	[SkillHandler(SkillId.RG_FLAGGRAFFITI)]
	public class FlagGraffitiHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			if (parameters.Character is PlayerCharacter pc) pc.ServerMessage("Flag Graffiti placement is not yet implemented.");
		}
	}

	[SkillHandler(SkillId.RG_CLEANER)]
	public class RemoverHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}
}
