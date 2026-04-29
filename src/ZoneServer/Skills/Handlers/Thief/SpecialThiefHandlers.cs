using System;
using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_SPRINKLESAND)]
	public class SprinkleSandHandler : ISkillHandler
	{
		// eAthena TF_SPRINKLESAND: 130% physical hit with a 20% chance
		// to inflict Blind. Single level.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
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

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, result.HitCount, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.TF_BACKSLIDING)]
	public class BackSlidingHandler : ISkillHandler
	{
		// eAthena TF_BACKSLIDING: short backwards teleport (5 cells)
		// in the direction opposite the caster's facing. Falls back
		// to no-op if the destination is blocked.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			var (dx, dy) = DirectionDelta(caster.Direction);
			var distance = 5;
			var dest = new Position(
				(short)(caster.Position.X - dx * distance),
				(short)(caster.Position.Y - dy * distance));

			if (caster.Map.CacheData != null && caster.Map.CacheData.IsPassable(dest.X, dest.Y))
				caster.Warp(new Location(caster.Map.Id, dest));

			return Task.CompletedTask;
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
	public class PickStoneHandler : ISkillHandler
	{
		// eAthena TF_PICKSTONE: adds one Stone (item id 7049) to the
		// caster's inventory. Used as ammo by TF_THROWSTONE.
		private const int StoneItemId = 7049;

		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not PlayerCharacter pc) return Task.CompletedTask;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			pc.Inventory.AddItem(new Item(StoneItemId, 1));
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.TF_THROWSTONE)]
	public class ThrowStoneHandler : ISkillHandler
	{
		// eAthena TF_THROWSTONE: ranged 50-damage attack with a 5%
		// chance to Stun and 5% chance to Blind. Consumes one Stone.
		private const int StoneItemId = 7049;

		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			if (caster is not PlayerCharacter pc) return Task.CompletedTask;

			var stone = pc.Inventory.GetItems(static i => i.ClassId == StoneItemId).FirstOrDefault();
			if (stone == null)
			{
				pc.ServerMessage("You need a Stone.");
				return Task.CompletedTask;
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

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 1, ActionType.Skill);
			return Task.CompletedTask;
		}
	}
}
