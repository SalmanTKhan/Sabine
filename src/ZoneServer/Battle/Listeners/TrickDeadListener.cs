using Sabine.Shared.Const;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Novice NV_TRICKDEAD: the play-dead status breaks the moment
	/// the caster attacks or casts a skill. Movement-break is wired
	/// directly in MovementController.MoveTo; damage-break is wired
	/// in Character.TakeDamage. This listener handles the attack /
	/// skill-cast break paths via CombatEvents.
	/// </summary>
	public static class TrickDeadListener
	{
		public static void Subscribe()
		{
			CombatEvents.PreAttackResolve += OnPreAttackResolve;
			CombatEvents.SkillCast += OnSkillCast;
		}

		private static void OnPreAttackResolve(AttackContext ctx)
		{
			var attacker = ctx.Attacker;
			if (attacker?.StatusEffects?.Has(StatusId.TrickDead) == true)
				attacker.StatusEffects.Stop(StatusId.TrickDead);
		}

		private static void OnSkillCast(Sabine.Zone.World.Actors.Character caster, Sabine.Zone.Skills.Skill skill)
		{
			// Allow Trick Dead itself to toggle off via re-cast — that
			// path goes through the handler, not this listener.
			if (skill?.Id == SkillId.NV_TRICKDEAD) return;
			if (caster?.StatusEffects?.Has(StatusId.TrickDead) == true)
				caster.StatusEffects.Stop(StatusId.TrickDead);
		}
	}
}
