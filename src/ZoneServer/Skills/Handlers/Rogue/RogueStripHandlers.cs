using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Rogue
{
	internal static class StripCommon
	{
		public static void Apply(UseSkillParams p, StatusId status)
		{
			var caster = p.Character;
			var target = p.Target;
			var skill = p.Skill;
			var level = p.SkillLevel;

			if (target == null) return;

			var chance = 5 * level + (caster.Parameters.Dex - target.Parameters.Dex) / 2;
			chance = Math.Clamp(chance, 5, 95);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

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
