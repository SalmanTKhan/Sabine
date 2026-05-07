using System;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Zone.World.Actors;
using Sabine.Zone;

namespace Sabine.Zone.Skills.StatusEffects
{
	/// <summary>
	/// Adjusts status-effect durations against the target's resistances.
	/// Mirrors eAthena's <c>status_get_sc_def</c>; the <c>RenewalStatusResistance</c>
	/// feature flag selects between rAthena's classic and renewal branches.
	/// </summary>
	public static class StatusResistance
	{
		/// <summary>
		/// Returns the duration that should actually be applied for the
		/// given status on the given target. May return <see cref="TimeSpan.Zero"/>
		/// if the target fully resisted; callers should skip <c>Start</c>
		/// in that case.
		/// </summary>
		public static TimeSpan ReduceDuration(Character target, StatusId id, TimeSpan baseDuration)
		{
			if (baseDuration <= TimeSpan.Zero || target == null)
				return baseDuration;

			var tickReductionPct = GetTickReductionPercent(target, id);
			if (tickReductionPct <= 0)
				return baseDuration;

			if (tickReductionPct >= 100)
				return TimeSpan.Zero;

			var ms = baseDuration.TotalMilliseconds * (100 - tickReductionPct) / 100.0;
			return TimeSpan.FromMilliseconds(ms);
		}

		/// <summary>
		/// Computes the per-status duration reduction in percent, picking
		/// the renewal or pre-renewal formula based on the current feature
		/// flags. Returns 0 for SCs that aren't subject to stat-based
		/// resistance (buffs, songs, etc.).
		/// </summary>
		private static int GetTickReductionPercent(Character target, StatusId id)
		{
			var p = target.Parameters;
			var vit = p.Vit;
			var luk = p.Luk;
			var intel = p.Int;
			var mdef = p.MagicDefense + p.MagicDefenseBonus;

			var renewal = ZoneServer.Instance.Data.Features.IsEnabled(FeatureId.RenewalStatusResistance);

			return renewal
				? RenewalReductionPercent(id, vit, luk, intel, mdef, p.Str)
				: ClassicReductionPercent(id, vit, luk, intel, mdef, p.Str);
		}

		// Pre-renewal: eAthena status_get_sc_def. Simpler stat blends; only
		// VIT/LUK matter for most physical-style debuffs, INT for sleep,
		// MDEF for freeze. Curse fully fails when STR is 0 (eAthena
		// classic behaviour kept for compatibility with Beta1).
		private static int ClassicReductionPercent(StatusId id, int vit, int luk, int intel, int mdef, int str)
		{
			return id switch
			{
				StatusId.Stone     => vit + luk / 3,
				StatusId.Freeze    => mdef + luk / 3,
				StatusId.Stun      => vit + luk / 3,
				StatusId.Sleep     => intel + luk / 3,
				StatusId.Poison    => vit / 2,
				StatusId.Curse     => str < 1 ? 0 : vit,
				StatusId.Silence   => vit / 2,
				StatusId.Blind     => (vit + intel) / 2,
				StatusId.Confusion => (vit + intel) / 2,
				StatusId.Bleeding  => vit / 2,
				_ => 0,
			};
		}

		// Renewal: rAthena status_get_sc_def #ifdef RENEWAL branch. LUK
		// gains a small +/-10 contribution on most non-physical statuses,
		// and Curse no longer auto-fails on STR=0.
		private static int RenewalReductionPercent(StatusId id, int vit, int luk, int intel, int mdef, int str)
		{
			return id switch
			{
				StatusId.Stone     => vit + luk / 3,
				StatusId.Freeze    => mdef + luk / 3,
				StatusId.Stun      => vit + luk / 3,
				StatusId.Sleep     => intel + luk / 3,
				StatusId.Poison    => vit / 2 + luk / 5,
				StatusId.Curse     => vit + luk / 3,
				StatusId.Silence   => vit / 2 + luk / 10,
				StatusId.Blind     => (vit + intel) / 2 + luk / 10,
				StatusId.Confusion => (vit + intel) / 2 + luk / 10,
				StatusId.Bleeding  => vit / 2 + luk / 5,
				_ => 0,
			};
		}
	}
}
