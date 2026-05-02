using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Blacksmith
{
	[SkillHandler(SkillId.BS_HAMMERFALL)]
	public class HammerfallHandler : IGroundSkillHandler
	{
		// eAthena BS_HAMMERFALL: AoE no-damage stun in a 3x3 area.
		// Stun chance = 20% + 10% per level.
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var stunChance = 20 + 10 * level;
			var rnd = RandomProvider.Get();

			foreach (var enemy in caster.Map.GetCharactersInRange(pos, 1))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				if (rnd.Next(100) < stunChance)
					enemy.StatusEffects.Start(StatusId.Stun, level, TimeSpan.FromSeconds(2 + level), caster);
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}
}
