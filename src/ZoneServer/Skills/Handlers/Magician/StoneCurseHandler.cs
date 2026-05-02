using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_STONECURSE)]
	public class StoneCurseHandler : ITargetedSkillHandler
	{
		// Red Gemstone item id (eAthena db: 716).
		private const int RedGemstoneId = 716;

		// eAthena classic MG_STONECURSE: chance = 24 + 2*level (%);
		// duration = 5 + 5*level seconds. Red Gemstone is consumed
		// regardless of resist (eAthena behavior).
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			if (caster is PlayerCharacter pc)
			{
				var gem = System.Linq.Enumerable.FirstOrDefault(pc.Inventory.GetItems(static i => i.ClassId == RedGemstoneId));
				if (gem == null)
				{
					pc.ServerMessage("You need a Red Gemstone.");
					return;
				}
				pc.Inventory.DecrementItem(gem, 1);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var rnd = RandomProvider.Get();
			var chance = 24 + 2 * level;
			if (rnd.Next(100) < chance)
			{
				var duration = TimeSpan.FromSeconds(5 + 5 * level);
				target.StatusEffects.Start(StatusId.Stone, level, duration, caster);
			}
		}
	}
}
