using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.Sage
{
	internal static class SageFieldPlacement
	{
		public static Task SpawnField(Character caster, Character target, Skill skill, StatusId status)
		{
			if (target == null) return Task.CompletedTask;

			var center = target.Position;
			for (var dx = -2; dx <= 2; dx++)
			{
				for (var dy = -2; dy <= 2; dy++)
				{
					var pos = new Sabine.Shared.World.Position((short)(center.X + dx), (short)(center.Y + dy));
					caster.Map.AddSkillUnit(new ElementalFieldUnit(caster, pos, skill.Id, skill.Level, status));
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, center.X, center.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.SA_CASTCANCEL)]
	public class CastCancelHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is PlayerCharacter pc) pc.StopCasting();
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.SA_MAGICROD)]
	public class MagicRodHandler : ISkillHandler
	{
		// eAthena SA_MAGICROD: short window in which incoming bolt
		// magic is absorbed as SP. Window scales with skill level.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			caster.StatusEffects.Start(StatusId.MagicRod, skill.Level, TimeSpan.FromSeconds(2 + skill.Level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.SA_SPELLBREAKER)]
	public class SpellBreakerHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is PlayerCharacter pcTarget) pcTarget.StopCasting();
			Send.ZC_NOTIFY_SKILL(caster, target?.Handle ?? caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.SA_DISPELL)]
	public class DispelHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter) return Task.CompletedTask;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			foreach (var sid in new[]
			{
				StatusId.Blessing, StatusId.IncreaseAgi, StatusId.Angelus, StatusId.Concentration,
				StatusId.TwoHandQuicken, StatusId.SpearQuicken, StatusId.AdrenalineRush,
				StatusId.OverThrust, StatusId.WeaponPerfection, StatusId.PowerMaximize,
				StatusId.AutoGuard, StatusId.ReflectShield, StatusId.Defender, StatusId.Providence,
				StatusId.KyrieEleison, StatusId.Magnificat, StatusId.Gloria, StatusId.Aspersio,
				StatusId.EnchantPoison,
			})
			{
				if (targetCharacter.StatusEffects.Has(sid))
					targetCharacter.StatusEffects.Stop(sid);
			}

			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.SA_VOLCANO)]
	public class VolcanoHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SageFieldPlacement.SpawnField(caster, target, skill, StatusId.Volcano);
	}

	[SkillHandler(SkillId.SA_DELUGE)]
	public class DelugeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SageFieldPlacement.SpawnField(caster, target, skill, StatusId.Deluge);
	}

	[SkillHandler(SkillId.SA_VIOLENTGALE)]
	public class ViolentGaleHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SageFieldPlacement.SpawnField(caster, target, skill, StatusId.Whirlwind);
	}

	[SkillHandler(SkillId.SA_LANDPROTECTOR)]
	public class LandProtectorHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SageFieldPlacement.SpawnField(caster, target, skill, StatusId.MagneticEarth);
	}
}
