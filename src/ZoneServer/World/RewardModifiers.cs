using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World
{
	/// <summary>
	/// Cross-cutting reward multiplier. Called at the EXP / zeny / drop
	/// award sites so per-character buffs (BD_RICHMANKIM and future event
	/// statuses) and server-wide rate knobs can amplify gains without
	/// each award path duplicating the logic.
	/// </summary>
	public static class RewardModifiers
	{
		/// <summary>
		/// Returns the multiplier (1.0 = unchanged) to apply to the EXP
		/// amount granted to <paramref name="recipient"/>.
		/// </summary>
		public static float GetExpMultiplier(Character recipient)
		{
			if (recipient == null) return 1.0f;
			var multiplier = 1.0f;

			// BD_RICHMANKIM ("Mr. Kim a Rich Man"): +25+25*level% EXP
			// gain while standing in the ensemble aura.
			if (recipient.StatusEffects != null
				&& recipient.StatusEffects.TryGet(StatusId.RichmanKim, out var richman))
			{
				var pct = 25 + 25 * richman.Level;
				multiplier *= 1.0f + pct / 100f;
			}

			return multiplier;
		}

		/// <summary>
		/// Returns the multiplier to apply to the drop chance of items
		/// rolled when <paramref name="killer"/> kills a mob. Stacks
		/// multiplicatively on top of <c>WorldConf.ItemDropRate</c>.
		/// </summary>
		public static float GetDropMultiplier(Character killer)
		{
			// No buff currently amplifies drops; this hook exists for
			// future event statuses (Yggdrasil Berry hour, GM-toggled
			// rate boosts).
			return 1.0f;
		}

		/// <summary>
		/// Returns the multiplier to apply to a zeny gain delta on
		/// <paramref name="recipient"/>. Only positive deltas should be
		/// scaled — losses (purchases, fees) stay 1.0.
		/// </summary>
		public static float GetZenyMultiplier(Character recipient)
		{
			return 1.0f;
		}
	}
}
