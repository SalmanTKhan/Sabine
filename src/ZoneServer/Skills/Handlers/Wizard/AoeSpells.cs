using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Wizard
{
	[SkillHandler(SkillId.WZ_VERMILION)]
	public class LordOfVermilionHandler : IGroundSkillHandler
	{
		// rAthena pre-renewal: 100% + 80%*lv MATK per hit, 4-7 hits
		// in a 9x9 area, 5% chance to Blind.
		// rAthena renewal: 400% + 60%*lv per hit, 20 hits over 4s.
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var renewal = BattleCalculator.IsRenewal();
			var ratio = renewal
				? 4.0f + 0.60f * level
				: 1.0f + 0.80f * level;
			var hits = renewal ? 20 : (4 + level / 2);

			foreach (var enemy in caster.Map.GetCharactersInRange(pos, 4))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					HitCount = hits,
					AttackElement = ElementType.Wind,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					enemy.TakeDamage(result.Damage, caster);
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}

	[SkillHandler(SkillId.WZ_HEAVENDRIVE)]
	public class HeavenDriveHandler : IGroundSkillHandler
	{
		// eAthena WZ_HEAVENDRIVE: 100% + 25%*lv Earth magic over a
		// 5x5 area. Removes non-trap ground-effect units (FireWall,
		// SafetyWall, Pneuma, Sage element fields, etc.) in the AOE.
		// Hunter traps are exempt from the sweep — clearing them would
		// break the Hunter playstyle.
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			// Clear ground units before damage so any units the AOE would
			// otherwise tick during the sweep are gone.
			foreach (var unit in caster.Map.GetSkillUnitsInRange(pos, 2))
			{
				if (unit is Sabine.Zone.World.Maps.SkillUnits.TrapUnit) continue;
				caster.Map.RemoveSkillUnit(unit);
			}

			var ratio = 1.0f + 0.25f * level;
			foreach (var enemy in caster.Map.GetCharactersInRange(pos, 2))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Earth,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					enemy.TakeDamage(result.Damage, caster);
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}

	[SkillHandler(SkillId.WZ_METEOR)]
	public class MeteorStormHandler : IGroundSkillHandler
	{
		// eAthena WZ_METEOR: random meteors over a 7x7 area for ~2-3s.
		// Each meteor is a 100%+10%*lv Fire MATK splash.
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var ratio = BattleCalculator.IsRenewal()
				? 2.0f + 0.20f * level
				: 1.0f + 0.10f * level;

			foreach (var enemy in caster.Map.GetCharactersInRange(pos, 3))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					HitCount = 2,
					AttackElement = ElementType.Fire,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					enemy.TakeDamage(result.Damage, caster);
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}

	[SkillHandler(SkillId.WZ_FROSTNOVA)]
	public class FrostNovaHandler : ITargetedSkillHandler
	{
		// eAthena WZ_FROSTNOVA: AoE around caster, 100% Water MATK,
		// chance to Freeze. Range = 5 cells.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var radius = 5;
			var ratio = 1.0f + 0.20f * level;
			var freezeChance = 20 + 5 * level;

			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, radius))
			{
				if (enemy == caster) continue;
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Water,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
				{
					enemy.TakeDamage(result.Damage, caster);
					if (RandomProvider.Get().Next(100) < freezeChance)
						enemy.StatusEffects.Start(StatusId.Freeze, level, TimeSpan.FromSeconds(15), caster);
				}
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.WZ_SIGHTRASHER)]
	public class SightRasherHandler : ITargetedSkillHandler
	{
		// eAthena WZ_SIGHTRASHER: consumes Sight buff, deals
		// 100%+20%*lv Wind magic + knockback in a 5-cell radius.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (!caster.StatusEffects.Has(StatusId.Sight))
			{
				if (caster is PlayerCharacter pc)
					pc.ServerMessage("You need to have Sight active.");
				return;
			}

			caster.StatusEffects.Stop(StatusId.Sight);
			var ratio = 1.0f + 0.20f * level;

			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, 5))
			{
				if (enemy == caster) continue;
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Wind,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
				{
					enemy.TakeDamage(result.Damage, caster);
					enemy.Controller?.Knockback(caster.Position, 7);
				}
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.WZ_STORMGUST)]
	public class StormGustHandler : IGroundSkillHandler
	{
		// rAthena pre-renewal: 100% + 40%*lv MATK Water, 3-hit; the
		// 3rd hit on a victim freezes (counter-based). 9x9 area.
		// rAthena renewal: -30 + 50%*lv per hit, freeze chance
		// 65 - 5%*lv flat per hit.
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var renewal = BattleCalculator.IsRenewal();
			var ratio = renewal
				? -0.30f + 0.50f * level
				: 1.0f + 0.40f * level;
			var freezeChance = renewal ? (65 - 5 * level) : 0;

			foreach (var enemy in caster.Map.GetCharactersInRange(pos, 4))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					HitCount = 3,
					AttackElement = ElementType.Water,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
				{
					enemy.TakeDamage(result.Damage, caster);

					if (renewal)
					{
						if (RandomProvider.Get().Next(100) < freezeChance)
							enemy.StatusEffects.Start(StatusId.Freeze, level, TimeSpan.FromSeconds(20), caster);
					}
					else
					{
						enemy.StatusEffects.Start(StatusId.Freeze, level, TimeSpan.FromSeconds(15), caster);
					}
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}
}
