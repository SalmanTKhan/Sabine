using System.Collections.Generic;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.StatusEffects
{
	/// <summary>
	/// Categorises status effects for the dispel pipeline. Mirrors
	/// eAthena's <c>status_change_end</c> filter masks (SCB_BUFF /
	/// SCB_DEBUFF / SCB_SPECIAL).
	/// </summary>
	public enum StatusCategory
	{
		/// <summary>Positive effects strippable by SA_DISPELL and similar.</summary>
		Buff,

		/// <summary>Negative effects strippable by AL_CURE / Resurrection.</summary>
		Debuff,

		/// <summary>Effects that survive dispels (Hermode, OptionState toggles).</summary>
		Special,
	}

	/// <summary>
	/// Lookup tables that classify each <see cref="StatusId"/> for the
	/// dispel framework. Used by <c>StatusEffectComponent.DispelCategory</c>
	/// to drop the right slice in one call.
	/// </summary>
	public static class StatusCategories
	{
		// Positive buffs that SA_DISPELL, BD_RAGNAROK, BD_ETERNALCHAOS,
		// and similar dispel skills should strip. Curated to match the
		// existing SA_DISPELL list plus the new per-song handlers.
		private static readonly HashSet<StatusId> DispellableBuffs = new()
		{
			StatusId.Blessing, StatusId.IncreaseAgi, StatusId.Angelus, StatusId.Concentration,
			StatusId.TwoHandQuicken, StatusId.SpearQuicken, StatusId.AdrenalineRush,
			StatusId.OverThrust, StatusId.WeaponPerfection, StatusId.PowerMaximize,
			StatusId.AutoGuard, StatusId.ReflectShield, StatusId.Defender, StatusId.Providence,
			StatusId.KyrieEleison, StatusId.Magnificat, StatusId.Gloria, StatusId.Aspersio,
			StatusId.EnchantPoison, StatusId.Assumptio, StatusId.MagicPower, StatusId.AuraBlade,
			StatusId.Parrying, StatusId.LkConcentration, StatusId.TensionRelax, StatusId.Berserk,
			StatusId.Suffragium, StatusId.LexAeterna, StatusId.MaxOverThrust, StatusId.CartBoost,
			StatusId.Whistle, StatusId.AssassinCross, StatusId.PoemBragi, StatusId.AppleIdun,
			StatusId.Humming, StatusId.FortuneKiss, StatusId.ServiceForYou, StatusId.BardSong,
			StatusId.DancerDance, StatusId.PowerChord, StatusId.Trust, StatusId.Sight,
			StatusId.Endure, StatusId.AutoCounter, StatusId.MovingRecovery, StatusId.Fortune,
			StatusId.ExplosionSpirits, StatusId.MentalStrength, StatusId.Spirits,
		};

		// Negative effects that AL_CURE and Resurrection clear. Curated
		// from the classic-era cleanse list — status ailments only, not
		// every debuff (Quagmire / Decrease Agi survive cure).
		private static readonly HashSet<StatusId> CureableDebuffs = new()
		{
			StatusId.Stone, StatusId.Freeze, StatusId.Stun, StatusId.Sleep,
			StatusId.Curse, StatusId.Silence, StatusId.Confusion, StatusId.Blind,
			StatusId.Bleeding, StatusId.Poison,
		};

		public static IReadOnlyCollection<StatusId> GetDispellableBuffs() => DispellableBuffs;

		public static IReadOnlyCollection<StatusId> GetCureableDebuffs() => CureableDebuffs;

		public static IReadOnlyCollection<StatusId> GetByCategory(StatusCategory category) => category switch
		{
			StatusCategory.Buff => DispellableBuffs,
			StatusCategory.Debuff => CureableDebuffs,
			_ => System.Array.Empty<StatusId>(),
		};
	}
}
