using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Rogue
{
	// Strip skills: roll chance, apply a Strip status that disables
	// the corresponding equipment slot. Actual unequip wiring is a
	// follow-up tied to the equipment system.

	internal static class StripCommon
	{
		public static Task Apply(Character caster, Character target, Skill skill, StatusId status)
		{
			if (target is not Character t) return Task.CompletedTask;

			var chance = 5 * skill.Level + (caster.Parameters.Dex - target.Parameters.Dex) / 2;
			chance = Math.Clamp(chance, 5, 95);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (RandomProvider.Get().Next(100) < chance)
				t.StatusEffects.Start(status, skill.Level, TimeSpan.FromSeconds(20 + 10 * skill.Level), caster);

			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.RG_STRIPWEAPON)]
	public class StripWeaponHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => StripCommon.Apply(caster, target, skill, StatusId.StripWeapon);
	}

	[SkillHandler(SkillId.RG_STRIPSHIELD)]
	public class StripShieldHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => StripCommon.Apply(caster, target, skill, StatusId.StripShield);
	}

	[SkillHandler(SkillId.RG_STRIPARMOR)]
	public class StripArmorHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => StripCommon.Apply(caster, target, skill, StatusId.StripArmor);
	}

	[SkillHandler(SkillId.RG_STRIPHELM)]
	public class StripHelmHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => StripCommon.Apply(caster, target, skill, StatusId.StripHelm);
	}
}
