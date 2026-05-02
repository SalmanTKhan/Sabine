using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_STEAL)]
	public class StealHandler : ITargetedSkillHandler
	{
		// eAthena classic TF_STEAL: 10 + 4*level base success chance,
		// then a per-drop roll using each drop's percent. The simple
		// model below: roll the base chance once, then pick one drop
		// using its weighted chance.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target is not Monster monster)
				return;

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (caster is not PlayerCharacter pc)
				return;

			if (monster.Data.Drops == null || monster.Data.Drops.Count == 0)
				return;

			var rnd = RandomProvider.Get();

			var baseChance = 10 + 4 * level;
			if (rnd.Next(100) >= baseChance)
				return;

			// Pick the first drop slot whose individual chance roll
			// passes and that hasn't been stolen from this mob already.
			for (var i = 0; i < monster.Data.Drops.Count; i++)
			{
				if (monster.StolenDropSlots.Contains(i))
					continue;

				var drop = monster.Data.Drops[i];
				if (drop.ItemId <= 0)
					continue;

				if (rnd.Next(1000) >= (int)(drop.Chance * 10))
					continue;

				monster.StolenDropSlots.Add(i);
				pc.Inventory.AddItem(new Item(drop.ItemId, 1));
				pc.ServerMessage($"You stole an item.");
				return;
			}
		}
	}
}
