using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Crusader
{
	[SkillHandler(SkillId.CR_TRUST)]
	public class FaithHandler : BuffSkillHandler
	{
		// eAthena CR_TRUST (Faith): passive +HP and Holy resistance.
		// Modeled here as a long status for simplicity.
		protected override StatusId StatusId => StatusId.Trust;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromHours(24);
	}

	[SkillHandler(SkillId.CR_AUTOGUARD)]
	public class AutoGuardHandler : BuffSkillHandler
	{
		// eAthena CR_AUTOGUARD: 5% per level chance to fully block
		// physical hits while shield-equipped. 300s + 0s/level.
		protected override StatusId StatusId => StatusId.AutoGuard;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(300);
	}

	[SkillHandler(SkillId.CR_REFLECTSHIELD)]
	public class ReflectShieldHandler : BuffSkillHandler
	{
		// eAthena CR_REFLECTSHIELD: 10% + 3%*lv reflected damage.
		// 300s.
		protected override StatusId StatusId => StatusId.ReflectShield;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(300);
	}

	[SkillHandler(SkillId.CR_PROVIDENCE)]
	public class ProvidenceHandler : BuffSkillHandler
	{
		// eAthena CR_PROVIDENCE: +5% Holy / Demon resist per level
		// (status.c:5335 + 2415-2416). 90s + 30s/level.
		protected override StatusId StatusId => StatusId.Providence;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(90 + 30 * level);
	}

	[SkillHandler(SkillId.CR_DEFENDER)]
	public class DefenderHandler : BuffSkillHandler
	{
		// eAthena CR_DEFENDER: ranged-physical reduction at the cost
		// of move/ASPD. 60s + 30s/level.
		protected override StatusId StatusId => StatusId.Defender;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60 + 30 * level);
	}

	[SkillHandler(SkillId.CR_SPEARQUICKEN)]
	public class SpearQuickenHandler : BuffSkillHandler
	{
		// eAthena CR_SPEARQUICKEN: ASPD/HIT/CRIT for 30s/level when
		// wielding a spear.
		protected override StatusId StatusId => StatusId.SpearQuicken;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30 * level);
	}

	[SkillHandler(SkillId.CR_DEVOTION)]
	public class DevotionHandler : BuffSkillHandler
	{
		// eAthena CR_DEVOTION: redirects damage taken by the target
		// to the caster. 5s/level. eAthena caps concurrent links at 5
		// (status.c:5738). The actual redirection lives in the
		// DevotionListener; this handler enforces the cap and refreshes
		// the bond on refresh of an existing link.
		public const int MaxLinks = 5;

		protected override StatusId StatusId => StatusId.Devotion;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(5 + 25 * level);

		public override void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			if (target == null) return;

			// Refreshing an existing link is always allowed.
			var alreadyLinked = target.StatusEffects != null
				&& target.StatusEffects.TryGet(StatusId.Devotion, out var existing)
				&& existing.Caster == caster;

			if (!alreadyLinked && caster.DevotionLinkedTargets.Count >= MaxLinks)
				return;

			base.Handle(parameters);
		}
	}
}
