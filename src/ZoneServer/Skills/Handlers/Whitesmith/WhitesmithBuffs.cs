using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Whitesmith
{
	/// <summary>
	/// WS_CARTBOOST (Cart Boost) — self movement-speed buff that requires
	/// a cart (MC_PUSHCART). The cart-equipped check is gated upstream at
	/// cast time in eAthena (skill.c:6001 SkillCheckPC); we don't model
	/// the cart toggle yet, so the buff applies unconditionally and
	/// becomes a thin speed bump. SC_DECREASEAGI is cancelled with no
	/// further effect when present (status.c:5098-5103) — that arm is
	/// deferred until the Whitesmith handler grows a pre-apply hook.
	/// Duration: 60s flat (skill_cast_db.txt:642).
	/// </summary>
	[SkillHandler(SkillId.WS_CARTBOOST)]
	public class CartBoostHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.CartBoost;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60);
	}

	/// <summary>
	/// WS_OVERTHRUSTMAX (Maximum Overthrust) — +20% physical damage per
	/// skill level. eAthena status.c:5840 sets val2 = 20*level; the buff
	/// cancels SC_OVERTHRUST on start (status.c:5060-5063) so the larger
	/// bonus replaces the smaller. Sabine routes the bonus through the
	/// existing <c>ApplyOverThrust</c> path in BattleCalculator; the
	/// status handler stores the same <c>val3</c> the OverThrust handler
	/// uses so the read site stays uniform. Duration: 180s flat
	/// (skill_cast_db.txt:819).
	/// </summary>
	[SkillHandler(SkillId.WS_OVERTHRUSTMAX)]
	public class MaxOverThrustHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.MaxOverThrust;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(180);
	}

	/// <summary>
	/// WS_MELTDOWN (Melt Down) — chance to break the target's weapon
	/// (10%/level) or armor (7%/level) on hit. eAthena status.c:5867
	/// stores val2 = 100*level (weapon break per-1000) and val3 = 70*level
	/// (armor break per-1000); battle.c:1740-1750 reads them on every
	/// physical-attack hit and rolls the breaks via skill_break_equip.
	/// Marker today: Sabine has no equip-break listener on the on-hit
	/// pipeline, so the bonus snapshot is wired but the rolls aren't
	/// fired. Duration: skill_cast_db.txt:639 — 15s + 5s/level.
	/// </summary>
	[SkillHandler(SkillId.WS_MELTDOWN)]
	public class MeltDownHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.MeltDown;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(15 + 5 * level);
	}
}
