using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	public abstract class MagicBoltHandler : ISkillHandler
	{
		protected abstract ElementType Element { get; }

		/// <summary>
		/// Called after each bolt lands a non-miss hit. Default no-op;
		/// subclasses use this to roll status procs (Cold Bolt freeze, etc.).
		/// </summary>
		protected virtual void OnHit(Character caster, Character target, Skill skill, AttackResult result) { }

		public async Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return;

			// Bolt skills fire one bolt per skill level (1..5 hits).
			var hitCount = (skill.Level + 1) / 2;

			Send.ZC_USE_SKILL(caster as PlayerCharacter, skill.Id, skill.Level, target.Handle, true);

			for (var i = 0; i < hitCount; i++)
			{
				var ctx = new AttackContext(caster, targetCharacter)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Magic,
					SkillRatio = 1.0f,
					AttackElement = this.Element,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
				{
					targetCharacter.TakeDamage(result.Damage, caster);
					this.OnHit(caster, targetCharacter, skill, result);
				}

				await Task.Delay(100);
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
		protected override void OnHit(Character caster, Character target, Skill skill, AttackResult result)
		{
			if (target.StatusEffects?.Has(StatusId.Freeze) == true)
				return;

			var rnd = RandomProvider.Get();
			var freezeChance = 5 * skill.Level;
			if (rnd.Next(100) >= freezeChance)
				return;

			var duration = TimeSpan.FromSeconds(5 + 2 * skill.Level);
			target.StatusEffects.Start(StatusId.Freeze, skill.Level, duration, caster);
		}
	}

	[SkillHandler(SkillId.MG_LIGHTNINGBOLT)]
	public class LightningBoltHandler : MagicBoltHandler
	{
		protected override ElementType Element => ElementType.Wind;
	}
}
