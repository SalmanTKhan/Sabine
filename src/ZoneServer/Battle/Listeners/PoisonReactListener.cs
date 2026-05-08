using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Yggdrasil.Util;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Counter-attacks the attacker with an Envenom-equivalent hit when
	/// the target has the PoisonReact buff active and the incoming hit is
	/// physical. Mirrors eAthena battle.c (SC_POISONREACT branch around
	/// line 3239): a Poison-element defender on the attacker auto-fires a
	/// counter; otherwise the counter triggers at val3% chance and
	/// decrements the val2 hit pool. The buff drops once val2 ≤ 0.
	/// </summary>
	public static class PoisonReactListener
	{
		public static void Subscribe()
		{
			CombatEvents.AttackResolved += OnAttackResolved;
		}

		private static void OnAttackResolved(AttackContext ctx, AttackResult result)
		{
			if (result.IsMiss || result.Damage <= 0) return;
			if (ctx.Kind != AttackKind.Physical) return;

			var target = ctx.Target;
			var attacker = ctx.Attacker;
			if (target == null || attacker == null || target == attacker) return;
			if (target.IsDead) return;
			if (target.StatusEffects == null) return;
			if (!target.StatusEffects.TryGet(StatusId.PoisonReact, out var effect)) return;

			// eAthena auto-trigger: when the attacker's defense element is
			// Poison the counter always fires (and consumes the rest of
			// the buff in one swing).
			var attackerDefenseElement = ResolveAttackerDefenseElement(attacker);
			var autoTrigger = attackerDefenseElement == ElementType.Poison;

			if (!autoTrigger)
			{
				if (effect.Val3 <= 0) return;
				if (RandomProvider.Get().Next(100) >= effect.Val3) return;
			}

			var counter = new AttackContext(target, attacker)
			{
				SkillId = SkillId.TF_POISON,
				SkillLevel = autoTrigger ? effect.Level : 5,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.15f * (autoTrigger ? effect.Level : 5),
				AttackElement = ElementType.Poison,
			};
			var counterResult = BattleCalculator.Calc(counter);
			var dealt = counterResult.IsMiss ? 0 : counterResult.Damage;

			if (dealt > 0)
				attacker.TakeDamage(dealt, target);

			Send.ZC_NOTIFY_SKILL(target, attacker.Handle, SkillId.TF_POISON, counter.SkillLevel, dealt, 0, 1, counterResult.ActionType);

			if (autoTrigger)
				effect.Val2 = 0;
			else
				effect.Val2 -= 1;

			if (effect.Val2 <= 0)
				target.StatusEffects.Stop(StatusId.PoisonReact);
		}

		private static ElementType ResolveAttackerDefenseElement(World.Actors.Character attacker)
		{
			if (attacker is World.Actors.Monster monster)
				return monster.Data.Element;

			if (attacker is World.Actors.PlayerCharacter pc)
			{
				foreach (var item in pc.Inventory.GetItems(static a => a.IsEquipped))
				{
					if (item.Data.DefenseElement != ElementType.None && item.Data.DefenseElement != ElementType.Neutral)
						return item.Data.DefenseElement;
				}
			}

			return ElementType.Neutral;
		}
	}
}
