using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.World.Groups
{
	public class PartyManager
	{
		public static PartyManager Instance { get; } = new PartyManager();

		private readonly Dictionary<int, Party> _parties = new();
		private readonly Dictionary<string, Party> _partiesByName = new();

		public Party CreateParty(PlayerCharacter leader, string name)
		{
			if (leader.Party != null || _partiesByName.ContainsKey(name.ToLower()))
			{
				return null;
			}

			var party = new Party(leader, name);
			_parties.Add(party.Id, party);
			_partiesByName.Add(name.ToLower(), party);

			party.UpdateAllClients();
			return party;
		}

		public Party GetParty(int partyId)
		{
			_parties.TryGetValue(partyId, out var party);
			return party;
		}

		public void RemoveParty(int partyId)
		{
			if (_parties.TryGetValue(partyId, out var party))
			{
				_parties.Remove(partyId);
				_partiesByName.Remove(party.Name.ToLower());
			}
		}
	}
}
