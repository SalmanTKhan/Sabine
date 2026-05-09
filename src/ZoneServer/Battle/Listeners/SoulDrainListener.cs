using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// HW_SOULDRAIN — refunds SP to the caster on every magic-skill kill.
	/// eAthena status.c grants <c>(target.MaxSp * 1%) * (level * 2)</c>
	/// SP back per kill when the killing blow was a damaging skill, so
	/// it tops the caster off after a heavy spell rotation. Sabine
	/// approximates with a simpler MaxSP-based formula tied to the
	/// killer's own pool — the alpha-era profile lacks per-attack skill
	/// metadata on the kill event, so we don't gate on "magic kill"
	/// here yet; SoulDrain refunds on any kill and the gating tightens
	/// when AttackKind threads through to the kill event.
	///
	/// Refund amount: <c>2% per level of the killer's MaxSP</c>, capped
	/// to current MaxSP. Subscribed once at server startup.
	/// </summary>
	public static class SoulDrainListener
	{
		public static void Subscribe()
		{
			CombatEvents.MonsterKilled += OnMonsterKilled;
		}

		private static void OnMonsterKilled(Monster monster, Character killer)
		{
			if (killer == null) return;

			var level = killer.Skills?.GetLevel(SkillId.HW_SOULDRAIN) ?? 0;
			if (level <= 0) return;

			var refund = killer.Parameters.SpMax * 2 * level / 100;
			if (refund <= 0) return;

			killer.Parameters.Modify(ParameterType.Sp, refund);
		}
	}
}
