using Sabine.Shared.Util;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Battlegrounds;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Battleground match-mode listener. Subscribes to
	/// <see cref="CombatEvents.PlayerKilled"/>; on a PvP kill, finds the
	/// killer's and victim's BGs and bumps the killer's score when
	/// they're paired opponents. First team to reach WinScore wins —
	/// rewards land on every member of the winning team and the match
	/// ends.
	///
	/// This is the v1 substrate that BG modes (Flavius, Tierra, KvM)
	/// plug into: each mode tunes the score target, reward, respawn,
	/// and any mode-specific extra triggers (capture-the-flag etc.) on
	/// top of the kill-count base.
	/// </summary>
	public static class BattlegroundScoreListener
	{
		public static void Subscribe()
		{
			CombatEvents.PlayerKilled += OnPlayerKilled;
			CombatEvents.MonsterKilled += OnMonsterKilled;
		}

		/// <summary>
		/// Flavius-style win condition: when a Monster designated as a
		/// BG's "crystal" (Battleground.CrystalHandle) dies, the
		/// opposing team wins outright. Other monster kills are
		/// ignored — this listener only fires the BG end on registered
		/// crystals.
		/// </summary>
		private static void OnMonsterKilled(Sabine.Zone.World.Actors.Monster monster, Character killer)
		{
			var bgs = ZoneServer.Instance.World.Battlegrounds;
			Battleground breached = null;
			foreach (var bg in bgs.All)
			{
				if (bg.CrystalHandle == monster.Handle)
				{
					breached = bg;
					break;
				}
			}
			if (breached == null) return;
			if (breached.MatchEnded) return;
			if (breached.OpponentId == null) return;

			var winner = bgs.Get(breached.OpponentId.Value);
			if (winner == null) return;

			Announce(winner, Sabine.Shared.Util.Localization.Get("Crystal destroyed! Victory."));
			Announce(breached, Sabine.Shared.Util.Localization.Get("Your crystal has fallen. Defeat."));
			EndMatch(winner, breached);
		}

		private static void OnPlayerKilled(PlayerCharacter victim, Character killer)
		{
			if (killer is not PlayerCharacter pcKiller) return;
			if (pcKiller == victim) return;

			var bgs = ZoneServer.Instance.World.Battlegrounds;
			var killerBg = bgs.GetByMember(pcKiller.Id);
			var victimBg = bgs.GetByMember(victim.Id);

			if (killerBg == null || victimBg == null) return;
			if (killerBg.Id == victimBg.Id) return; // friendly fire — don't score
			if (killerBg.OpponentId != victimBg.Id) return; // not the paired match

			if (killerBg.MatchEnded) return;

			killerBg.Score++;

			BroadcastScore(killerBg, victimBg);

			if (killerBg.Score >= killerBg.WinScore)
				EndMatch(killerBg, victimBg);
		}

		private static void BroadcastScore(Battleground winner, Battleground loser)
		{
			Announce(winner, Localization.Get("BG: {0} - {1}"), winner.Score, loser.Score);
			Announce(loser, Localization.Get("BG: {0} - {1}"), loser.Score, winner.Score);
		}

		private static void EndMatch(Battleground winner, Battleground loser)
		{
			winner.MatchEnded = true;
			loser.MatchEnded = true;

			Announce(winner, Localization.Get("Victory! +{0} zeny."), winner.RewardZeny);
			Announce(loser, Localization.Get("Defeat."));

			foreach (var charId in winner.CharacterIds)
			{
				var pc = FindOnline(charId);
				if (pc != null)
					pc.Parameters.Modify(Sabine.Shared.Const.ParameterType.Zeny, winner.RewardZeny);
			}
		}

		private static void Announce(Battleground bg, string format, params object[] args)
		{
			foreach (var charId in bg.CharacterIds)
			{
				var pc = FindOnline(charId);
				pc?.ServerMessage(format, args);
			}
		}

		private static PlayerCharacter FindOnline(int charId)
		{
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				if (map.TryGetCharacter(charId, out var c) && c is PlayerCharacter pc)
					return pc;
			}
			return null;
		}
	}
}
