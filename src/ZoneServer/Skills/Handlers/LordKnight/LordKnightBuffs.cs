using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.LordKnight
{
	/// <summary>
	/// LK_AURABLADE — adds level*10 flat ATK to physical attacks while
	/// active. eAthena status.c SC_AURABLADE; the +ATK lives in the
	/// damage pipeline (battle.c) so the status itself is a marker
	/// today. Duration: 20s + 20s/level.
	/// </summary>
	[SkillHandler(SkillId.LK_AURABLADE)]
	public class AuraBladeHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.AuraBlade;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(20 + 20 * level);
	}

	/// <summary>
	/// LK_CONCENTRATION — +5%/level ATK and +10%/level Hit, at the
	/// cost of +5%/level damage taken. eAthena status.c
	/// SC_CONCENTRATION (LK variant). The ATK% and Hit deltas land via
	/// the status handler's OnStart; the self-DEF reduction is marker-
	/// only until the damage pipeline grows a hook for it. Duration:
	/// 20s + 5s/level.
	/// </summary>
	[SkillHandler(SkillId.LK_CONCENTRATION)]
	public class ConcentrationHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.LkConcentration;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(20 + 5 * level);
	}

	/// <summary>
	/// LK_PARRYING — chance to block physical melee damage while
	/// wielding a two-handed sword (eAthena classic: 20% + 3%*level
	/// block chance). Marker today; the block roll lives in the
	/// damage pipeline and isn't wired. Duration: 60s + 10s/level.
	/// </summary>
	[SkillHandler(SkillId.LK_PARRYING)]
	public class ParryingHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.Parrying;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60 + 10 * level);
	}

	/// <summary>
	/// LK_TENSIONRELAX — accelerated HP regen while sitting; the
	/// caster gets a "rest" buff that pushes HP back over time.
	/// Marker today: Sabine's regen tick doesn't yet read this
	/// status. Duration: until cancelled (10 minutes here as a
	/// safety cap; eAthena clears it on stand-up via a movement
	/// hook we don't model yet).
	/// </summary>
	[SkillHandler(SkillId.LK_TENSIONRELAX)]
	public class TensionRelaxHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.TensionRelax;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromMinutes(10);
	}

	/// <summary>
	/// LK_BERSERK (Frenzy) — doubles MaxHP, triples ATK, +30% move
	/// speed, locks skills, blocks heals, drains HP every 15s. eAthena
	/// status.c SC_BERSERK. Marker today: the stat / skill-lock /
	/// heal-block / drain hooks all live in different subsystems and
	/// would need a dedicated commit each. Duration: 180s.
	/// </summary>
	[SkillHandler(SkillId.LK_BERSERK)]
	public class BerserkHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.Berserk;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(180);
	}
}
