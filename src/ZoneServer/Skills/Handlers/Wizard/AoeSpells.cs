using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Wizard
{
	[SkillHandler(SkillId.WZ_VERMILION)]
	public class LordOfVermilionHandler : ISkillHandler
	{
		// rAthena pre-renewal: 100% + 80%*lv MATK per hit, 4-7 hits
		// in a 9x9 area, 5% chance to Blind.
		// rAthena renewal: 400% + 60%*lv per hit, 20 hits over 4s.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var renewal = BattleCalculator.IsRenewal();
			var ratio = renewal
				? 4.0f + 0.60f * skill.Level
				: 1.0f + 0.80f * skill.Level;
			var hits = renewal ? 20 : (4 + skill.Level / 2);

			foreach (var enemy in caster.Map.GetCharactersInRange(target.Position, 4))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
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

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_HEAVENDRIVE)]
	public class HeavenDriveHandler : ISkillHandler
	{
		// eAthena WZ_HEAVENDRIVE: 100% + 25%*lv Earth magic over a
		// 5x5 area. Removes ground-effect units in the AoE. Renewal
		// unchanged. v1 omits the ground-removal since it'd require a
		// general unit-clear API on the map.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ratio = 1.0f + 0.25f * skill.Level;
			foreach (var enemy in caster.Map.GetCharactersInRange(target.Position, 2))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Earth,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					enemy.TakeDamage(result.Damage, caster);
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_METEOR)]
	public class MeteorStormHandler : ISkillHandler
	{
		// eAthena WZ_METEOR (Meteor Storm): random meteors over a
		// 7x7 area for ~2-3 seconds. Each meteor is a 100%+10%*lv
		// Fire MATK splash. Renewal: same shape, slightly tuned ratio.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ratio = BattleCalculator.IsRenewal()
				? 2.0f + 0.20f * skill.Level
				: 1.0f + 0.10f * skill.Level;

			foreach (var enemy in caster.Map.GetCharactersInRange(target.Position, 3))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
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

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_FROSTNOVA)]
	public class FrostNovaHandler : ISkillHandler
	{
		// eAthena WZ_FROSTNOVA: AoE around caster, 100% Water MATK,
		// chance to Freeze. Range = 5 cells in pre-renewal.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			var radius = 5;
			var ratio = 1.0f + 0.20f * skill.Level;
			var freezeChance = 20 + 5 * skill.Level;

			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, radius))
			{
				if (enemy == caster) continue;
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Water,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
				{
					enemy.TakeDamage(result.Damage, caster);
					if (Yggdrasil.Util.RandomProvider.Get().Next(100) < freezeChance)
						enemy.StatusEffects.Start(StatusId.Freeze, skill.Level, System.TimeSpan.FromSeconds(15), caster);
				}
			}

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_SIGHTRASHER)]
	public class SightRasherHandler : ISkillHandler
	{
		// eAthena WZ_SIGHTRASHER: consumes the Sight buff to deal
		// 100%+20%*lv Wind magic + knockback in a 5-cell radius.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (!caster.StatusEffects.Has(StatusId.Sight))
			{
				if (caster is PlayerCharacter pc)
					pc.ServerMessage("You need to have Sight active.");
				return Task.CompletedTask;
			}

			caster.StatusEffects.Stop(StatusId.Sight);
			var ratio = 1.0f + 0.20f * skill.Level;

			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, 5))
			{
				if (enemy == caster) continue;
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
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

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.WZ_STORMGUST)]
	public class StormGustHandler : ISkillHandler
	{
		// rAthena pre-renewal: 100% + 40%*lv MATK Water, 3-hit; the
		// 3rd hit on a victim freezes (counter-based). 9x9 area.
		// rAthena renewal: -30 + 50%*lv per hit, freeze chance
		// 65 - 5%*lv flat per hit.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var renewal = BattleCalculator.IsRenewal();
			var ratio = renewal
				? -0.30f + 0.50f * skill.Level
				: 1.0f + 0.40f * skill.Level;
			var freezeChance = renewal ? (65 - 5 * skill.Level) : 0;

			foreach (var enemy in caster.Map.GetCharactersInRange(target.Position, 4))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
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
						if (Yggdrasil.Util.RandomProvider.Get().Next(100) < freezeChance)
							enemy.StatusEffects.Start(StatusId.Freeze, skill.Level, System.TimeSpan.FromSeconds(20), caster);
					}
					else
					{
						// Pre-renewal counter: every 3rd hit freezes.
						// Approximated by guaranteed freeze on the
						// triple-hit batch.
						enemy.StatusEffects.Start(StatusId.Freeze, skill.Level, System.TimeSpan.FromSeconds(15), caster);
					}
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}
}
