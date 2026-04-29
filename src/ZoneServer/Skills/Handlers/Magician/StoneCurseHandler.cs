using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_STONECURSE)]
	public class StoneCurseHandler : ISkillHandler
	{
		// Red Gemstone item id (eAthena db: 716).
		private const int RedGemstoneId = 716;

		// eAthena classic MG_STONECURSE: chance = 24 + 2*level (%);
		// duration = 5 + 5*level seconds. Red Gemstone is consumed
		// regardless of resist (eAthena behavior).
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return Task.CompletedTask;

			if (caster is PlayerCharacter pc)
			{
				var gem = System.Linq.Enumerable.FirstOrDefault(pc.Inventory.GetItems(static i => i.ClassId == RedGemstoneId));
				if (gem == null)
				{
					pc.ServerMessage("You need a Red Gemstone.");
					return Task.CompletedTask;
				}
				pc.Inventory.DecrementItem(gem, 1);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			var rnd = RandomProvider.Get();
			var chance = 24 + 2 * skill.Level;
			if (rnd.Next(100) < chance)
			{
				var duration = TimeSpan.FromSeconds(5 + 5 * skill.Level);
				targetCharacter.StatusEffects.Start(StatusId.Stone, skill.Level, duration, caster);
			}

			return Task.CompletedTask;
		}
	}
}
