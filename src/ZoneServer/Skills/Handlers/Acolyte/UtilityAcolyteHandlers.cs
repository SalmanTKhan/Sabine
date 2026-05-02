using System.Linq;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_HOLYWATER)]
	public class AquaBenedictaHandler : ITargetedSkillHandler
	{
		// Holy Water item id (eAthena db: 523).
		private const int EmptyBottleId = 713;
		private const int HolyWaterId = 523;

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not PlayerCharacter pc)
				return;

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
				return;
			}

			var bottle = pc.Inventory.GetItems(static i => i.ClassId == EmptyBottleId).FirstOrDefault();
			if (bottle == null)
			{
				pc.ServerMessage("You need an Empty Bottle.");
				return;
			}

			pc.Inventory.DecrementItem(bottle, 1);
			pc.Inventory.AddItem(new Item(HolyWaterId, 1));

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.AL_CURE)]
	public class CureHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			// eAthena AL_CURE removes Silence, Blind (Darkness), Chaos,
			// and Confusion. Stone removal is technically different
			// (Statue Petrify) but kept here as a usability convenience.
			if (target.StatusEffects.Has(StatusId.Silence))
				target.StatusEffects.Stop(StatusId.Silence);
			if (target.StatusEffects.Has(StatusId.Blind))
				target.StatusEffects.Stop(StatusId.Blind);
			if (target.StatusEffects.Has(StatusId.Chaos))
				target.StatusEffects.Stop(StatusId.Chaos);
			if (target.StatusEffects.Has(StatusId.Confusion))
				target.StatusEffects.Stop(StatusId.Confusion);
			if (target.StatusEffects.Has(StatusId.Stone))
				target.StatusEffects.Stop(StatusId.Stone);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.AL_RUWACH)]
	public class RuwachHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

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
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = 1.0f + 0.45f * level,
					AttackElement = ElementType.Holy,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					other.TakeDamage(result.Damage, caster);
			}
		}
	}

	[SkillHandler(SkillId.AL_PNEUMA)]
	public class PneumaHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var unit = new Sabine.Zone.World.Maps.SkillUnits.PneumaUnit(caster, pos, level);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}
}
