using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Redirects incoming damage from a Devoted target to the
	/// Crusader caster. The Devotion status's Caster field carries
	/// the redirect destination. Mirrors eAthena CR_DEVOTION.
	/// </summary>
	public static class DevotionListener
	{
		public static void Subscribe()
		{
			CombatEvents.PreAttackResolve += OnPreAttackResolve;
		}

		private static void OnPreAttackResolve(AttackContext ctx)
		{
			var target = ctx.Target;
			if (target == null) return;
			if (target.StatusEffects == null) return;
			if (!target.StatusEffects.TryGet(StatusId.Devotion, out var effect)) return;

			var protector = effect.Caster;
			if (protector == null) return;
			if (protector.IsDead) return;
			if (protector == ctx.Attacker) return;

			// Out-of-range protectors lose the bond. Devotion's max
			// range is 7 cells in classic; checked at attack time.
			var dx = protector.Position.X - target.Position.X;
			var dy = protector.Position.Y - target.Position.Y;
			if (dx * dx + dy * dy > 49)
			{
				target.StatusEffects.Stop(StatusId.Devotion);
				return;
			}

			ctx.Target = protector;
		}
	}
}
