using System;
using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_HOLYWATER)]
	public class AquaBenedictaHandler : ISkillHandler
	{
		// Holy Water item id (eAthena db: 523).
		private const int EmptyBottleId = 713;
		private const int HolyWaterId = 523;

		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not PlayerCharacter pc)
				return Task.CompletedTask;

			// eAthena requires the caster to stand on or adjacent to a
			// water tile. We check the caster cell plus the four
			// orthogonally adjacent cells.
			var cache = caster.Map.CacheData;
			var pos = caster.Position;
			var nearWater = cache != null && (
				cache.IsWater(pos.X, pos.Y)
				|| cache.IsWater(pos.X - 1, pos.Y)
				|| cache.IsWater(pos.X + 1, pos.Y)
				|| cache.IsWater(pos.X, pos.Y - 1)
				|| cache.IsWater(pos.X, pos.Y + 1));
			if (!nearWater)
			{
				pc.ServerMessage("You must be next to water.");
				return Task.CompletedTask;
			}

			var bottle = pc.Inventory.GetItems(static i => i.ClassId == EmptyBottleId).FirstOrDefault();
			if (bottle == null)
			{
				pc.ServerMessage("You need an Empty Bottle.");
				return Task.CompletedTask;
			}

			pc.Inventory.DecrementItem(bottle, 1);
			pc.Inventory.AddItem(new Item(HolyWaterId, 1));

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AL_CURE)]
	public class CureHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return Task.CompletedTask;

			// eAthena AL_CURE removes Silence, Blind (Darkness), Chaos,
			// and Confusion. Stone removal is technically different
			// (Statue Petrify) but kept here as a usability convenience.
			if (targetCharacter.StatusEffects.Has(StatusId.Silence))
				targetCharacter.StatusEffects.Stop(StatusId.Silence);
			if (targetCharacter.StatusEffects.Has(StatusId.Blind))
				targetCharacter.StatusEffects.Stop(StatusId.Blind);
			if (targetCharacter.StatusEffects.Has(StatusId.Chaos))
				targetCharacter.StatusEffects.Stop(StatusId.Chaos);
			if (targetCharacter.StatusEffects.Has(StatusId.Confusion))
				targetCharacter.StatusEffects.Stop(StatusId.Confusion);
			if (targetCharacter.StatusEffects.Has(StatusId.Stone))
				targetCharacter.StatusEffects.Stop(StatusId.Stone);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AL_RUWACH)]
	public class RuwachHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_SKILL_ENTRY(caster, skill.Id, caster.Position);

			// AOE radius 5 around caster. Reveals Hiding/Cloaking enemies
			// and deals a small Holy magic hit to each.
			var radius = 5;
			foreach (var other in caster.Map.GetCharactersInRange(caster.Position, radius))
			{
				if (other == caster) continue;
				if (!other.IsHostileTo(caster)) continue;

				if (other.IsHidden)
					other.StatusEffects.Stop(StatusId.Hiding);

				var ctx = new AttackContext(caster, other)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Magic,
					SkillRatio = 1.0f + 0.45f * skill.Level,
					AttackElement = ElementType.Holy,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					other.TakeDamage(result.Damage, caster);
			}

			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AL_PNEUMA)]
	public class PneumaHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var unit = new Sabine.Zone.World.Maps.SkillUnits.PneumaUnit(caster, target.Position, skill.Level);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}
}
