using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Stalker
{
	/// <summary>
	/// ST_CHASEWALK (Stealth) — toggle hide-with-slow that drains SP every
	/// 10s. eAthena status.c:5537 sets val2 = 10000 (drain interval),
	/// val3 = 35 - 5*level (speed reduction; less penalty at higher
	/// levels, with a -40 bonus while SL_ROGUE Spirit is up), and
	/// val4 = 10 + 2*level (SP cost per drain). Marker today — Sabine
	/// has no OPT3 hide flag for ChaseWalk, no SP-drain tick on the
	/// status pipeline, and no SC_INCSTR follow-on; all three legs are
	/// deferred. Duration: 10s minimum + 30s/level (skill_cast_db.txt:648).
	/// </summary>
	[SkillHandler(SkillId.ST_CHASEWALK)]
	public class ChaseWalkHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.ChaseWalk;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(10 + 30 * level);
	}

	/// <summary>
	/// ST_PRESERVE — locks the Stalker's Plagiarism (Intimidate) copied
	/// skill so it isn't overwritten by a new Plagiarism trigger for the
	/// duration. eAthena handles the lock at skill.c plagiarism check;
	/// Sabine doesn't have a Plagiarism handler yet, so this stays
	/// marker-only and the lock arm will land alongside that handler.
	/// Duration: 600s flat (skill_cast_db.txt:800).
	/// </summary>
	[SkillHandler(SkillId.ST_PRESERVE)]
	public class PreserveHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.Preserve;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(600);
	}

	/// <summary>
	/// ST_REJECTSWORD (Counter Instinct) — reflects 15%*level of incoming
	/// sword-class melee damage back to the attacker, up to 3 charges
	/// before the buff drops. eAthena status.c:5706: val2 = 15*val1,
	/// val3 = 3. The reflect listener lives on the on-hit pipeline; Sabine
	/// doesn't have a sword-only damage filter on its reflect bucket yet,
	/// so this is marker-only — the snapshot vals land via the status
	/// handler so the listener can read them when it ships. Duration:
	/// 300s flat (skill_cast_db.txt:650).
	/// </summary>
	[SkillHandler(SkillId.ST_REJECTSWORD)]
	public class RejectSwordHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.RejectSword;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(300);
	}
}
