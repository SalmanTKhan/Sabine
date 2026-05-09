using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Sniper
{
	/// <summary>
	/// SN_WINDWALK (Wind Walker) — buff on the caster's party. eAthena
	/// status.c SC_WINDWALK: flee bonus (level+1)/2 → 1/1/2/2/3/3/4/4/5/5
	/// across levels 1–10, plus a movement-speed boost. Sabine routes the
	/// flee through BonusFlee and the speed through ParameterType.Speed
	/// in <see cref="StatusEffects.Handlers.WindWalkStatusHandler"/>;
	/// the party-wide application is deferred (eAthena uses
	/// <c>party_foreachsamemap</c> at skill.c:3996), so this handler only
	/// buffs the target today. Duration: 130s + 30s/level
	/// (skill_cast_db.txt:633).
	/// </summary>
	[SkillHandler(SkillId.SN_WINDWALK)]
	public class WindWalkHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.WindWalk;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(130 + 30 * level);
	}

	/// <summary>
	/// SN_SIGHT (Falcon Eyes / True Sight) — self buff that grants a flat
	/// +1 hit per level (eAthena status.c:3582 reads val3 = 3*level, but
	/// classic skill_db caps the visible bonus). The full eAthena formula
	/// also adds +(level/3) to every primary stat and +10*level critical;
	/// only the hit leg is wired today, the rest is deferred to a follow-
	/// up that grows the status handler. Duration: 30s flat
	/// (skill_cast_db.txt:627).
	/// </summary>
	[SkillHandler(SkillId.SN_SIGHT)]
	public class FalconEyesHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.TrueSight;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30);
	}
}
