using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Rogue
{
	internal static class StripCommon
	{
		// AM_CP_* shields the matching equip slot; eAthena skill.c
		// skill_strip_equip aborts before the strip status is applied
		// when the corresponding SC_PROTECT* is up. The frame still
		// fires so the cast SP/cooldown is consumed.
		private static StatusId GetProtectionStatus(StatusId stripStatus) => stripStatus switch
		{
			StatusId.StripWeapon => StatusId.CpWeapon,
			StatusId.StripShield => StatusId.CpShield,
			StatusId.StripArmor => StatusId.CpArmor,
			StatusId.StripHelm => StatusId.CpHelm,
			_ => StatusId.None,
		};

		public static void Apply(UseSkillParams p, StatusId status)
		{
			var caster = p.Character;
			var target = p.Target;
			var skill = p.Skill;
			var level = p.SkillLevel;

			if (target == null) return;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var protection = GetProtectionStatus(status);
			if (protection != StatusId.None && target.StatusEffects.Has(protection))
				return;

			var chance = 5 * level + (caster.Parameters.Dex - target.Parameters.Dex) / 2;
			chance = Math.Clamp(chance, 5, 95);

			if (RandomProvider.Get().Next(100) < chance)
				target.StatusEffects.Start(status, level, TimeSpan.FromSeconds(20 + 10 * level), caster);
		}
	}

	[SkillHandler(SkillId.RG_STRIPWEAPON)]
	public class StripWeaponHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => StripCommon.Apply(p, StatusId.StripWeapon);
	}

	[SkillHandler(SkillId.RG_STRIPSHIELD)]
	public class StripShieldHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => StripCommon.Apply(p, StatusId.StripShield);
	}

	[SkillHandler(SkillId.RG_STRIPARMOR)]
	public class StripArmorHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => StripCommon.Apply(p, StatusId.StripArmor);
	}

	[SkillHandler(SkillId.RG_STRIPHELM)]
	public class StripHelmHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => StripCommon.Apply(p, StatusId.StripHelm);
	}
}
