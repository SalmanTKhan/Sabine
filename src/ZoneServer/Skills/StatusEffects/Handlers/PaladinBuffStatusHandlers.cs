using System;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_SACRIFICE (PA_SACRIFICE) — flag handler that snapshots the
	/// hit-charge counter. Mirrors eAthena status.c:5307 (<c>val2 = 5</c>,
	/// <c>tick = -1</c>). The redirect arm hasn't shipped — the on-hit
	/// pipeline doesn't grow a "redirect to bonded ally" hook in this
	/// commit. When that hook lands, it decrements val2 and ends the
	/// buff at zero, the same way Kyrie's val3 is consumed.
	/// </summary>
	[StatusEffectHandler(StatusId.Sacrifice)]
	public class SacrificeStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = 5;
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_GOSPEL (PA_GOSPEL) — every 10s, rolls a random outcome onto
	/// party members and enemies in a 13-cell radius. eAthena status.c
	/// :5660 sets tick = 10000ms; the roll table lives in skill.c
	/// (skill_gospel). Sabine ships a representative subset of the
	/// classic-era table: party members get heal / Blessing / IncreaseAgi
	/// / +20-stats; enemies get Curse / Blind / direct damage.
	/// </summary>
	[StatusEffectHandler(StatusId.Gospel)]
	public class GospelStatusHandler : IStatusEffectHandler
	{
		private const int TickIntervalMs = 10000;
		private const int Range = 13;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val4 = 0;
		}

		public void OnEnd(Character target, StatusEffect effect) { }

		public void OnTick(Character caster, StatusEffect effect, TimeSpan elapsed)
		{
			effect.Val4 += (int)elapsed.TotalMilliseconds;
			if (effect.Val4 < TickIntervalMs)
				return;
			effect.Val4 -= TickIntervalMs;

			if (caster.Map == null) return;

			var rnd = RandomProvider.Get();
			foreach (var c in caster.Map.GetCharactersInRange(caster.Position, Range))
			{
				if (c == caster || c.IsDead) continue;

				if (caster.IsHostileTo(c))
					ApplyEnemyRoll(caster, c, rnd);
				else if (IsPartyMember(caster, c))
					ApplyAllyRoll(c, rnd);
			}
		}

		private static bool IsPartyMember(Character caster, Character other)
		{
			if (caster is not PlayerCharacter cp || other is not PlayerCharacter op) return false;
			if (cp.Party == null || op.Party == null) return false;
			return cp.Party == op.Party;
		}

		private static void ApplyAllyRoll(Character ally, Random rnd)
		{
			switch (rnd.Next(6))
			{
				case 0: // big heal
					ally.HealHp(Math.Max(50, ally.Parameters.HpMax / 4));
					break;
				case 1: // blessing
					ally.StatusEffects?.Start(StatusId.Blessing, 10, TimeSpan.FromSeconds(60), ally);
					break;
				case 2: // increase agi
					ally.StatusEffects?.Start(StatusId.IncreaseAgi, 10, TimeSpan.FromSeconds(60), ally);
					break;
				case 3: // angelus
					ally.StatusEffects?.Start(StatusId.Angelus, 10, TimeSpan.FromSeconds(60), ally);
					break;
				case 4: // concentration
					ally.StatusEffects?.Start(StatusId.Concentration, 5, TimeSpan.FromSeconds(60), ally);
					break;
				default: // no-op (some rolls are duds in eAthena)
					break;
			}
		}

		private static void ApplyEnemyRoll(Character caster, Character enemy, Random rnd)
		{
			switch (rnd.Next(5))
			{
				case 0:
					enemy.StatusEffects?.Start(StatusId.Curse, 1, TimeSpan.FromSeconds(30), caster);
					break;
				case 1:
					enemy.StatusEffects?.Start(StatusId.Blind, 1, TimeSpan.FromSeconds(30), caster);
					break;
				case 2:
					enemy.StatusEffects?.Start(StatusId.DecreaseAgi, 5, TimeSpan.FromSeconds(30), caster);
					break;
				case 3:
					{
						// Flat holy damage tick — Gospel's "smite" outcome.
						var dmg = Math.Max(50, enemy.Parameters.HpMax / 8);
						enemy.TakeDamage(dmg, caster);
					}
					break;
				default: // dud
					break;
			}
		}
	}
}
