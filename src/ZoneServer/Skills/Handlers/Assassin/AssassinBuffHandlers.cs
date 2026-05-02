using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Assassin
{
	[SkillHandler(SkillId.AS_CLOAKING)]
	public class CloakingHandler : ITargetedSkillHandler
	{
		// eAthena AS_CLOAKING: enhanced Hide that allows movement
		// when adjacent to a wall. Toggles on recast.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster.StatusEffects.Has(StatusId.Cloaking))
			{
				caster.StatusEffects.Stop(StatusId.Cloaking);
				return;
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			caster.StatusEffects.Start(StatusId.Cloaking, level, TimeSpan.FromMinutes(30), caster);
			Send.ZC_NOTIFY_VANISH(caster, DisappearType.Vanish);
		}
	}

	[SkillHandler(SkillId.AS_ENCHANTPOISON)]
	public class EnchantPoisonHandler : BuffSkillHandler
	{
		// eAthena AS_ENCHANTPOISON: weapon element -> Poison for 60s + 30s/level.
		protected override StatusId StatusId => StatusId.EnchantPoison;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60 + 30 * level);
	}

	[SkillHandler(SkillId.AS_POISONREACT)]
	public class PoisonReactHandler : BuffSkillHandler
	{
		// eAthena AS_POISONREACT: counter-attacks Poison-element hits with Envenom. 30s + 30s/level.
		protected override StatusId StatusId => StatusId.PoisonReact;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30 + 30 * level);
	}
}
