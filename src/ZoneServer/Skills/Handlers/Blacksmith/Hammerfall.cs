using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Blacksmith
{
	[SkillHandler(SkillId.BS_HAMMERFALL)]
	public class HammerfallHandler : ISkillHandler
	{
		// eAthena BS_HAMMERFALL: AoE no-damage stun in a 3x3 area.
		// Stun chance = 20% + 10% per level.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var stunChance = 20 + 10 * skill.Level;
			var rnd = RandomProvider.Get();

			foreach (var enemy in caster.Map.GetCharactersInRange(target.Position, 1))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				if (rnd.Next(100) < stunChance)
					enemy.StatusEffects.Start(StatusId.Stun, skill.Level, TimeSpan.FromSeconds(2 + skill.Level), caster);
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}
}
