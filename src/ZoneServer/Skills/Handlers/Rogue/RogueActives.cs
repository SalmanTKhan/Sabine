using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Rogue
{
	[SkillHandler(SkillId.RG_STEALCOIN)]
	public class StealCoinHandler : ISkillHandler
	{
		// eAthena RG_STEALCOIN: 50% + level*1.5% chance to steal Zeny
		// from a monster. Stub: animate only; zeny model can be wired
		// with the existing Steal handler.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, target?.Handle ?? caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.RG_BACKSTAP)]
	public class BackstabHandler : ISkillHandler
	{
		// eAthena RG_BACKSTAP: 340% + 40%*lv physical when struck
		// from behind. v1 doesn't enforce facing — applies the bonus
		// flat. Renewal tunes the ratio slightly.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ratio = BattleCalculator.IsRenewal()
				? 3.0f + 0.40f * skill.Level
				: 3.4f + 0.40f * skill.Level;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				WeaponRequired = true,
				IgnoreFlee = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, result.HitCount, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.RG_RAID)]
	public class RaidHandler : ISkillHandler
	{
		// eAthena RG_RAID (Sightless Raid): 100% + 40%*lv physical
		// AoE around the caster, breaks Hide. Renewal: same shape.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			caster.StatusEffects.Stop(StatusId.Hiding);

			var ratio = 1.0f + 0.40f * skill.Level;
			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, 2))
			{
				if (enemy == caster) continue;
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Physical,
					SkillRatio = ratio,
					WeaponRequired = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					enemy.TakeDamage(result.Damage, caster);
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.RG_INTIMIDATE)]
	public class IntimidateHandler : ISkillHandler
	{
		// eAthena RG_INTIMIDATE (Snatch): grabs the target and warps
		// both caster and target to a random map cell. v1 deals a
		// small hit and warps the caster only.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.30f * skill.Level,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			var dest = caster.Map.GetRandomWalkablePosition();
			caster.Warp(new Sabine.Shared.World.Location(caster.Map.Id, dest));

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 1, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.RG_GRAFFITI)]
	public class GraffitiHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is PlayerCharacter pc) pc.ServerMessage("Graffiti placement is not yet implemented.");
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.RG_FLAGGRAFFITI)]
	public class FlagGraffitiHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is PlayerCharacter pc) pc.ServerMessage("Flag Graffiti placement is not yet implemented.");
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.RG_CLEANER)]
	public class RemoverHandler : ISkillHandler
	{
		// eAthena RG_CLEANER: removes graffiti / wall paint in a
		// small radius. v1 stub.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}
}
