using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Assassin
{
	[SkillHandler(SkillId.AS_CLOAKING)]
	public class CloakingHandler : ISkillHandler
	{
		// eAthena AS_CLOAKING: enhanced Hide that allows movement
		// when adjacent to a wall. Toggles on recast. Wall-adjacency
		// gating happens via the StatusEffect's update tick (a TODO
		// for the cloak status handler); v1 simply applies the flag.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster.StatusEffects.Has(StatusId.Cloaking))
			{
				caster.StatusEffects.Stop(StatusId.Cloaking);
				return Task.CompletedTask;
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			caster.StatusEffects.Start(StatusId.Cloaking, skill.Level, TimeSpan.FromMinutes(30), caster);
			Send.ZC_NOTIFY_VANISH(caster, DisappearType.Vanish);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AS_ENCHANTPOISON)]
	public class EnchantPoisonHandler : BuffSkillHandler
	{
		// eAthena AS_ENCHANTPOISON: weapon element → Poison for
		// 60s + 30s/level.
		protected override StatusId StatusId => StatusId.EnchantPoison;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60 + 30 * level);
	}

	[SkillHandler(SkillId.AS_POISONREACT)]
	public class PoisonReactHandler : BuffSkillHandler
	{
		// eAthena AS_POISONREACT: counter-attacks Poison-element
		// hits with Envenom. 30s + 30s/level.
		protected override StatusId StatusId => StatusId.PoisonReact;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30 + 30 * level);
	}
}
