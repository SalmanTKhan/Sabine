using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	public abstract class MagicBoltHandler : ITargetedSkillHandler
	{
		protected abstract ElementType Element { get; }

		/// <summary>
		/// Called after each bolt lands a non-miss hit. Default no-op;
		/// subclasses use this to roll status procs (Cold Bolt freeze, etc.).
		/// </summary>
		protected virtual void OnHit(Character caster, Character target, Skill skill, int level, AttackResult result) { }

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			// Classic bolts are one multi-hit magic attack with one hit per
			// skill level. ZC_NOTIFY_SKILL carries the total damage and hit
			// count; clients use count to stagger each bolt by 200 ms.
			var ctx = CreateAttackContext(caster, target, skill.Id, level, this.Element);
			var result = BattleCalculator.Calc(ctx);
			var damage = result.IsMiss ? 0 : result.Damage;
			var impactDelayMs = ResolveImpactDelayMs(caster);

			// Match rAthena's battle_delay_damage ordering: broadcast the skill
			// packet immediately so clients can animate toward the target, then
			// mutate HP and run on-hit procs when attackMT actually lands.
			Send.ZC_NOTIFY_SKILL(
				caster,
				target.Handle,
				skill.Id,
				level,
				damage,
				impactDelayMs,
				target.Parameters.DamageMotionDelay,
				result.ActionType,
				result.HitCount);

			if (result.IsMiss)
				return;

			if (impactDelayMs <= 0)
			{
				this.ApplyImpact(caster, target, skill, level, result, damage);
				return;
			}

			ZoneServer.Instance.World.Scheduler.Schedule(
				TimeSpan.FromMilliseconds(impactDelayMs),
				_ => this.ApplyImpact(caster, target, skill, level, result, damage),
				null);
		}

		internal static int ResolveImpactDelayMs(Character caster)
		{
			return Math.Max(0, caster?.Parameters?.AttackMotionDelay ?? 0);
		}

		private void ApplyImpact(Character caster, Character target, Skill skill, int level, AttackResult result, int damage)
		{
			// A delayed projectile does not follow a target through a warp. This
			// mirrors rAthena's delayed-damage same-map validity check.
			if (target == null || target.IsDead || caster?.Map == null || target.Map != caster.Map)
				return;

			target.TakeDamage(damage, caster);
			for (var i = 0; i < result.HitCount; ++i)
				this.OnHit(caster, target, skill, level, result);
		}

		internal static AttackContext CreateAttackContext(
			Character caster,
			Character target,
			SkillId skillId,
			int level,
			ElementType element)
		{
			var hitCount = Math.Clamp(level, 1, 10);
			return new AttackContext(caster, target)
			{
				SkillId = skillId,
				SkillLevel = level,
				Kind = AttackKind.Magic,
				// BattleCalculator treats SkillRatio as total cast damage and
				// divides that total over HitCount for the wire/display result.
				SkillRatio = hitCount,
				HitCount = hitCount,
				AttackElement = element,
			};
		}
	}

	[SkillHandler(SkillId.MG_FIREBOLT)]
	public class FireBoltHandler : MagicBoltHandler
	{
		protected override ElementType Element => ElementType.Fire;
	}

	[SkillHandler(SkillId.MG_COLDBOLT)]
	public class ColdBoltHandler : MagicBoltHandler
	{
		protected override ElementType Element => ElementType.Water;

		// eAthena classic Cold Bolt: 5% per skill level chance per hit
		// to freeze, 5+2*level second duration. Mirrors Frost Diver's
		// proc shape but smaller chance because Cold Bolt fires multiple
		// times per cast.
		protected override void OnHit(Character caster, Character target, Skill skill, int level, AttackResult result)
		{
			if (target.StatusEffects?.Has(StatusId.Freeze) == true)
				return;

			var rnd = RandomProvider.Get();
			var freezeChance = 5 * level;
			if (rnd.Next(100) >= freezeChance)
				return;

			var duration = TimeSpan.FromSeconds(5 + 2 * level);
			target.StatusEffects.Start(StatusId.Freeze, level, duration, caster);
		}
	}

	[SkillHandler(SkillId.MG_LIGHTNINGBOLT)]
	public class LightningBoltHandler : MagicBoltHandler
	{
		protected override ElementType Element => ElementType.Wind;
	}
}
