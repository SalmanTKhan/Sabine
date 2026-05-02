using System;
using System.Threading.Tasks;
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

		// Bolts fire ~100ms apart so each hit lands as its own visual
		// event. Handle is sync void, so we fire-and-forget the
		// timing loop on the thread pool. The first bolt fires
		// synchronously to keep the cast feel responsive.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			// Bolt skills fire one bolt per skill level (1..5 hits).
			var hitCount = (level + 1) / 2;

			Send.ZC_USE_SKILL(caster as PlayerCharacter, skill.Id, level, target.Handle, true);

			// Fire the first bolt immediately, schedule the rest.
			this.FireBolt(caster, target, skill, level);
			if (hitCount > 1)
				_ = this.FireRemainingAsync(caster, target, skill, level, hitCount - 1);
		}

		private async Task FireRemainingAsync(Character caster, Character target, Skill skill, int level, int remaining)
		{
			for (var i = 0; i < remaining; i++)
			{
				await Task.Delay(100);
				if (target == null || target.IsDead) return;
				if (caster == null || caster.IsDead) return;
				this.FireBolt(caster, target, skill, level);
			}
		}

		private void FireBolt(Character caster, Character target, Skill skill, int level)
		{
			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f,
				AttackElement = this.Element,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				target.TakeDamage(result.Damage, caster);
				this.OnHit(caster, target, skill, level, result);
			}
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
