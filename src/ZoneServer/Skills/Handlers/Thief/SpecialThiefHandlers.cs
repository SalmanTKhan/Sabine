using System;
using System.Linq;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_SPRINKLESAND)]
	public class SprinkleSandHandler : ITargetedSkillHandler
	{
		// eAthena TF_SPRINKLESAND: 130% physical hit with a 20% chance
		// to inflict Blind. Single level.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.3f,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				target.TakeDamage(result.Damage, caster);

				if (RandomProvider.Get().Next(100) < 20)
					target.StatusEffects.Start(StatusId.Blind, 1, TimeSpan.FromSeconds(30), caster);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, result.HitCount, result.ActionType);
		}
	}

	[SkillHandler(SkillId.TF_BACKSLIDING)]
	public class BackSlidingHandler : ITargetedSkillHandler
	{
		// eAthena TF_BACKSLIDING: short backwards teleport (5 cells)
		// in the direction opposite the caster's facing. Falls back
		// to no-op if the destination is blocked.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var (dx, dy) = DirectionDelta(caster.Direction);
			var distance = 5;
			var dest = new Position(
				(short)(caster.Position.X - dx * distance),
				(short)(caster.Position.Y - dy * distance));

			if (caster.Map.CacheData != null && caster.Map.CacheData.IsPassable(dest.X, dest.Y))
				caster.Warp(new Location(caster.Map.Id, dest));
		}

		private static (int dx, int dy) DirectionDelta(Direction d) => d switch
		{
			Direction.North => (0, 1),
			Direction.NorthEast => (1, 1),
			Direction.East => (1, 0),
			Direction.SouthEast => (1, -1),
			Direction.South => (0, -1),
			Direction.SouthWest => (-1, -1),
			Direction.West => (-1, 0),
			Direction.NorthWest => (-1, 1),
			_ => (0, 1),
		};
	}

	[SkillHandler(SkillId.TF_PICKSTONE)]
	public class PickStoneHandler : ITargetedSkillHandler
	{
		// eAthena TF_PICKSTONE: adds one Stone (item id 7049) to the
		// caster's inventory. Used as ammo by TF_THROWSTONE.
		private const int StoneItemId = 7049;

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not PlayerCharacter pc) return;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			pc.Inventory.AddItem(new Item(StoneItemId, 1));
		}
	}

	[SkillHandler(SkillId.TF_THROWSTONE)]
	public class ThrowStoneHandler : ITargetedSkillHandler
	{
		// eAthena TF_THROWSTONE: ranged 50-damage attack with a 5%
		// chance to Stun and 5% chance to Blind. Consumes one Stone.
		private const int StoneItemId = 7049;

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;
			if (caster is not PlayerCharacter pc) return;

			var stone = pc.Inventory.GetItems(static i => i.ClassId == StoneItemId).FirstOrDefault();
			if (stone == null)
			{
				pc.ServerMessage("You need a Stone.");
				return;
			}
			pc.Inventory.DecrementItem(stone, 1);

			// Fixed 50 damage, treated as long-range neutral.
			var damage = 50;
			target.TakeDamage(damage, caster);

			var rnd = RandomProvider.Get();
			if (rnd.Next(100) < 5)
				target.StatusEffects.Start(StatusId.Stun, 1, TimeSpan.FromSeconds(3), caster);
			if (rnd.Next(100) < 5)
				target.StatusEffects.Start(StatusId.Blind, 1, TimeSpan.FromSeconds(20), caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 1, ActionType.Skill);
		}
	}
}
