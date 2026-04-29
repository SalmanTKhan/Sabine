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
		// eAthena CR_PROVIDENCE: +5% Demi-Human / Holy resist per
		// level. 90s + 30s/level.
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
		// to the caster. 5s/level. The actual redirection is wired
		// via a damage-pipeline hook; here we only apply the marker.
		protected override StatusId StatusId => StatusId.Devotion;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(5 + 25 * level);
	}
}
