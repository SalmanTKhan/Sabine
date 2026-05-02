using System;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Rogue RG_PLAGIARISM: when a Rogue is hit by a copyable
	/// targeted skill, the skill id is recorded into a Plagiarism
	/// status (Val1 = copied SkillId). The copied skill can later be
	/// invoked by the Rogue through their hotbar — that part is a
	/// follow-up; this listener captures the skill id and refreshes
	/// the duration on every fresh capture.
	/// </summary>
	public static class PlagiarismListener
	{
		// eAthena: copy duration is 5s + 5s/level. Cap at 60s for v1.
		private static TimeSpan GetDuration(int plagLevel)
			=> TimeSpan.FromSeconds(Math.Min(5 + 5 * plagLevel, 60));

		public static void Subscribe()
		{
			CombatEvents.AttackResolved += OnAttackResolved;
		}

		private static void OnAttackResolved(AttackContext ctx, AttackResult result)
		{
			if (result.IsMiss) return;
			if (ctx.SkillId == SkillId.None) return;

			// Plagiarism only triggers on Rogues (or Stalkers in the
			// classic chain). Sabine has a single PLAGIARISM passive.
			var target = ctx.Target;
			if (target is not PlayerCharacter pc) return;

			var plagLevel = pc.Skills.GetLevel(SkillId.RG_PLAGIARISM);
			if (plagLevel < 1) return;

			// Don't copy your own skills, and don't copy skills the
			// Rogue already knows directly.
			if (ctx.Attacker == pc) return;
			if (pc.Skills.GetLevel(ctx.SkillId) > 0) return;

			// Capture: store the SkillId in Val1, refresh duration.
			pc.StatusEffects.Stop(StatusId.Plagiarism);
			pc.StatusEffects.Start(StatusId.Plagiarism, plagLevel, GetDuration(plagLevel), pc, (int)ctx.SkillId);

			pc.ServerMessage($"You have plagiarized: {ctx.SkillId}.");
		}
	}
}
