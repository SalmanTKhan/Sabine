using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.Sage
{
	internal static class SageFieldPlacement
	{
		public static void SpawnField(Character caster, Sabine.Shared.World.Position center, Skill skill, int level, StatusId status)
		{
			for (var dx = -2; dx <= 2; dx++)
			{
				for (var dy = -2; dy <= 2; dy++)
				{
					var pos = new Sabine.Shared.World.Position((short)(center.X + dx), (short)(center.Y + dy));
					caster.Map.AddSkillUnit(new ElementalFieldUnit(caster, pos, skill.Id, level, status));
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);
		}
	}

	[SkillHandler(SkillId.SA_CASTCANCEL)]
	public class CastCancelHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is PlayerCharacter pc) pc.StopCasting();
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.SA_MAGICROD)]
	public class MagicRodHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			caster.StatusEffects.Start(StatusId.MagicRod, level, TimeSpan.FromSeconds(2 + level), caster);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.SA_SPELLBREAKER)]
	public class SpellBreakerHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target is PlayerCharacter pcTarget) pcTarget.StopCasting();
			Send.ZC_NOTIFY_SKILL(caster, target?.Handle ?? caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.SA_DISPELL)]
	public class DispelHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

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
				if (target.StatusEffects.Has(sid))
					target.StatusEffects.Stop(sid);
			}
		}
	}

	[SkillHandler(SkillId.SA_VOLCANO)]
	public class VolcanoHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> SageFieldPlacement.SpawnField(p.Character, p.TargetPosition, p.Skill, p.SkillLevel, StatusId.Volcano);
	}

	[SkillHandler(SkillId.SA_DELUGE)]
	public class DelugeHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> SageFieldPlacement.SpawnField(p.Character, p.TargetPosition, p.Skill, p.SkillLevel, StatusId.Deluge);
	}

	[SkillHandler(SkillId.SA_VIOLENTGALE)]
	public class ViolentGaleHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> SageFieldPlacement.SpawnField(p.Character, p.TargetPosition, p.Skill, p.SkillLevel, StatusId.Whirlwind);
	}

	[SkillHandler(SkillId.SA_LANDPROTECTOR)]
	public class LandProtectorHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams p)
			=> SageFieldPlacement.SpawnField(p.Character, p.TargetPosition, p.Skill, p.SkillLevel, StatusId.MagneticEarth);
	}
}
