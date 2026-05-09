using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_MARIONETTE — the caster (donor) side of the Marionette pair.
	/// Halves the donor's primary stats (val3 / val4 carry the
	/// snapshot, packed three-stats-per-int) and ends the partner's
	/// SC_MARIONETTE2 when the donor's status drops. eAthena
	/// status.c:5668 stores the same packing.
	/// </summary>
	[StatusEffectHandler(StatusId.Marionette)]
	public class MarionetteStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			ApplyDelta(target, effect, sign: -1);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			ApplyDelta(target, effect, sign: +1);
			MarionettePair.EndPartner(target, effect, StatusId.Marionette2);
		}

		private static void ApplyDelta(Character target, StatusEffect effect, int sign)
		{
			var (str, agi, vit, intl, dex, luk) = MarionettePair.Unpack(effect);
			target.Parameters.Modify(ParameterType.Str, sign * str);
			target.Parameters.Modify(ParameterType.Agi, sign * agi);
			target.Parameters.Modify(ParameterType.Vit, sign * vit);
			target.Parameters.Modify(ParameterType.Int, sign * intl);
			target.Parameters.Modify(ParameterType.Dex, sign * dex);
			target.Parameters.Modify(ParameterType.Luk, sign * luk);
		}
	}

	/// <summary>
	/// SC_MARIONETTE2 — the partner (receiver) side. Reads the same
	/// packed snapshot from val3 / val4 and adds it to the partner's
	/// stats; when either side ends, the other tears down with it via
	/// <see cref="MarionettePair.EndPartner"/>.
	/// </summary>
	[StatusEffectHandler(StatusId.Marionette2)]
	public class Marionette2StatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var (str, agi, vit, intl, dex, luk) = MarionettePair.Unpack(effect);
			target.Parameters.Modify(ParameterType.Str, +str);
			target.Parameters.Modify(ParameterType.Agi, +agi);
			target.Parameters.Modify(ParameterType.Vit, +vit);
			target.Parameters.Modify(ParameterType.Int, +intl);
			target.Parameters.Modify(ParameterType.Dex, +dex);
			target.Parameters.Modify(ParameterType.Luk, +luk);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			var (str, agi, vit, intl, dex, luk) = MarionettePair.Unpack(effect);
			target.Parameters.Modify(ParameterType.Str, -str);
			target.Parameters.Modify(ParameterType.Agi, -agi);
			target.Parameters.Modify(ParameterType.Vit, -vit);
			target.Parameters.Modify(ParameterType.Int, -intl);
			target.Parameters.Modify(ParameterType.Dex, -dex);
			target.Parameters.Modify(ParameterType.Luk, -luk);

			MarionettePair.EndPartner(target, effect, StatusId.Marionette);
		}
	}

	/// <summary>
	/// Helpers shared by the donor and receiver Marionette handlers:
	/// stat-snapshot packing and cross-end propagation.
	/// </summary>
	internal static class MarionettePair
	{
		public static (int str, int agi, int vit, int intl, int dex, int luk) Unpack(StatusEffect effect)
		{
			var v3 = effect.Val3;
			var v4 = effect.Val4;
			var str = (v3 >> 16) & 0xFF;
			var agi = (v3 >> 8) & 0xFF;
			var vit = v3 & 0xFF;
			var intl = (v4 >> 16) & 0xFF;
			var dex = (v4 >> 8) & 0xFF;
			var luk = v4 & 0xFF;
			return (str, agi, vit, intl, dex, luk);
		}

		/// <summary>
		/// Ends the paired Marionette status on the partner identified
		/// by Val1 (character id). Mirrors eAthena status.c:6716 — when
		/// one side drops, the other has to follow so the stat math
		/// doesn't desync. Re-entrant safe: stopping a status that
		/// isn't active is a no-op on Sabine's status pipeline.
		/// </summary>
		public static void EndPartner(Character side, StatusEffect effect, StatusId partnerStatus)
		{
			var partnerId = effect.Val1;
			if (partnerId == 0) return;
			if (side?.Map == null) return;

			if (!side.Map.TryGetCharacter(partnerId, out var partner)) return;
			if (partner?.StatusEffects == null) return;
			if (!partner.StatusEffects.Has(partnerStatus)) return;

			partner.StatusEffects.Stop(partnerStatus);
		}
	}

	/// <summary>
	/// SC_LONGING (CG_LONGINGFREEDOM) — flag that cancels the ensemble
	/// move lock. Pure flag; Sabine doesn't model the move lock yet.
	/// </summary>
	[StatusEffectHandler(StatusId.LongingFreedom)]
	public class LongingFreedomStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_HERMODE (CG_HERMODE) — flag for dispel immunity. Status-removal
	/// logic doesn't read it yet; pure flag.
	/// </summary>
	[StatusEffectHandler(StatusId.Hermode)]
	public class HermodeStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
