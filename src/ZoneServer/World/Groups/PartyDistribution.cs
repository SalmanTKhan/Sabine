using System.Collections.Generic;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Groups
{
	/// <summary>
	/// Static helpers that distribute monster exp and dropped loot
	/// among party members. Mirrors eAthena's <c>party_exp_share</c>
	/// and <c>party_share_loot</c> in <c>src/map/party.c</c>.
	/// </summary>
	public static class PartyDistribution
	{
		/// <summary>
		/// Distributes <paramref name="baseExp"/> and <paramref name="jobExp"/>
		/// evenly among same-map, alive party members. Returns true
		/// when the party was eligible and the share was applied;
		/// false to signal the caller should fall back to giving the
		/// full amount to the killer.
		/// </summary>
		public static bool TryShareExp(PlayerCharacter killer, int baseExp, int jobExp)
		{
			var party = killer?.Party;
			if (party == null || party.ExpShareOption != PartyExpShareOption.EvenShare)
				return false;

			var eligible = GetSameMapAliveMembers(killer, party);
			if (eligible.Count <= 1)
				return false;

			// Optional bonus per extra member: configured percentage
			// added per member after the first. eAthena's
			// `party_even_share_bonus` knob.
			var bonus = ZoneServer.Instance.Conf.World.PartyEvenShareBonus;
			var multiplier = 100 + bonus * (eligible.Count - 1);

			var basePer = (int)((long)baseExp * multiplier / 100 / eligible.Count);
			var jobPer = (int)((long)jobExp * multiplier / 100 / eligible.Count);

			foreach (var p in eligible)
			{
				if (basePer > 0)
					p.GainBaseExp(basePer);
				if (jobPer > 0)
					p.GainJobExp(jobPer);
			}

			return true;
		}

		/// <summary>
		/// Returns the player who should receive a picked-up item.
		/// For individual share, that's always the picker. For even
		/// share, picks the next member on the same map in
		/// round-robin order. Falls back to the picker when no other
		/// eligible target is available.
		/// </summary>
		public static PlayerCharacter PickLootRecipient(PlayerCharacter picker)
		{
			var party = picker?.Party;
			if (party == null || party.ItemShareOption != PartyItemShareOption.EvenShare)
				return picker;

			var eligible = GetSameMapAliveMembers(picker, party);
			if (eligible.Count <= 1)
				return picker;

			var idx = party.AdvanceLootCursor() % eligible.Count;
			return eligible[idx];
		}

		private static List<PlayerCharacter> GetSameMapAliveMembers(PlayerCharacter anchor, Party party)
		{
			var result = new List<PlayerCharacter>();
			if (anchor?.Map == null)
				return result;

			var mapId = anchor.Map.Id;
			foreach (var member in party.Members)
			{
				var p = member.Player;
				if (p == null || p.IsDead)
					continue;
				if (p.Map?.Id != mapId)
					continue;

				result.Add(p);
			}
			return result;
		}
	}
}
