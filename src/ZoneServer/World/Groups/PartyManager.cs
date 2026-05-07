using System;
using System.Collections.Generic;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Groups
{
	/// <summary>
	/// Active party registry. State lives in-memory; persistence will
	/// be added in a follow-up slice. Mirrors eAthena's
	/// <c>party_db</c>.
	/// </summary>
	public class PartyManager
	{
		/// <summary>How long a pending invite remains valid.</summary>
		private static readonly TimeSpan InviteTtl = TimeSpan.FromSeconds(60);

		public static PartyManager Instance { get; } = new PartyManager();

		/// <summary>
		/// Loads every persisted party + member row at startup.
		/// Called once from <see cref="WorldManager.LoadParties"/>.
		/// Idempotent — re-running clears the in-memory state and
		/// reloads from disk.
		/// </summary>
		public void LoadAll()
		{
			lock (_syncRoot)
			{
				_parties.Clear();
				_partiesByName.Clear();
				_partiesByMember.Clear();

				var rows = ZoneServer.Instance.Database.LoadAllParties();
				foreach (var row in rows)
				{
					var party = new Party(row);
					_parties[party.Id] = party;
					_partiesByName[party.Name] = party;
					foreach (var m in party.Members)
						_partiesByMember[m.CharacterId] = party;
				}
				Yggdrasil.Logging.Log.Info("PartyManager: loaded {0} party/parties.", _parties.Count);
			}
		}

		private readonly object _syncRoot = new();
		private readonly Dictionary<int, Party> _parties = new();
		private readonly Dictionary<string, Party> _partiesByName = new(StringComparer.OrdinalIgnoreCase);

		// Reverse index: character id -> party. Lets login/logout
		// resolve a player's party in O(1) without scanning rosters.
		private readonly Dictionary<int, Party> _partiesByMember = new();

		// Pending invites are keyed by the invitee's character id so a
		// player can only have one open invite at a time, matching the
		// eAthena behavior.
		private readonly Dictionary<int, PartyInvite> _pendingInvites = new();

		public Party CreateParty(PlayerCharacter leader, string name)
		{
			lock (_syncRoot)
			{
				if (leader.Party != null)
					return null;
				if (_partiesByName.ContainsKey(name))
					return null;

				var party = new Party(leader, name);
				_parties.Add(party.Id, party);
				_partiesByName.Add(name, party);
				_partiesByMember[leader.Id] = party;

				party.UpdateAllClients();
				ZoneServer.Instance.Database.InsertParty(party);

				return party;
			}
		}

		/// <summary>
		/// Registers a freshly added member in the lookup table.
		/// Called by <see cref="Party.AddMember"/>.
		/// </summary>
		internal void RegisterMember(int characterId, Party party)
		{
			lock (_syncRoot)
				_partiesByMember[characterId] = party;
		}

		/// <summary>
		/// Removes a member from the lookup table.
		/// </summary>
		internal void UnregisterMember(int characterId)
		{
			lock (_syncRoot)
				_partiesByMember.Remove(characterId);
		}

		/// <summary>
		/// Looks up the party that <paramref name="characterId"/> is a
		/// member of, regardless of whether the character is online.
		/// </summary>
		public Party GetPartyByMember(int characterId)
		{
			lock (_syncRoot)
			{
				_partiesByMember.TryGetValue(characterId, out var party);
				return party;
			}
		}

		/// <summary>
		/// Hook invoked once a player's character is fully on the map.
		/// Re-attaches them to their persisted party (if any) and
		/// pushes the roster to all online members.
		/// </summary>
		public void OnPlayerLogin(PlayerCharacter player)
		{
			Party party;
			lock (_syncRoot)
			{
				if (!_partiesByMember.TryGetValue(player.Id, out party))
					return;
			}
			party.MemberOnline(player);
		}

		/// <summary>
		/// Hook invoked when a player disconnects. Drops the live
		/// back-pointer so the GC can reclaim the session; the member
		/// row stays put.
		/// </summary>
		public void OnPlayerLogout(PlayerCharacter player)
		{
			Party party;
			lock (_syncRoot)
			{
				if (!_partiesByMember.TryGetValue(player.Id, out party))
					return;
			}
			party.MemberOffline(player);
			player.Party = null;
		}

		public Party GetParty(int partyId)
		{
			lock (_syncRoot)
			{
				_parties.TryGetValue(partyId, out var party);
				return party;
			}
		}

		public Party GetPartyByName(string name)
		{
			lock (_syncRoot)
			{
				_partiesByName.TryGetValue(name, out var party);
				return party;
			}
		}

		public void RemoveParty(int partyId)
		{
			lock (_syncRoot)
			{
				if (!_parties.TryGetValue(partyId, out var party))
					return;

				_parties.Remove(partyId);
				_partiesByName.Remove(party.Name);
			}
		}

		/// <summary>
		/// Records that <paramref name="inviter"/>'s party is offering
		/// membership to the given character. Overwrites any prior
		/// pending invite for that character.
		/// </summary>
		public void RegisterInvite(PlayerCharacter inviter, PlayerCharacter invitee)
		{
			if (inviter?.Party == null || invitee == null)
				return;

			lock (_syncRoot)
			{
				_pendingInvites[invitee.Id] = new PartyInvite(
					inviter.Id, inviter.Name, inviter.Party.Id,
					DateTime.UtcNow + InviteTtl);
			}
		}

		/// <summary>
		/// Returns the pending invite for <paramref name="characterId"/>
		/// without consuming it. Expired invites are pruned and
		/// reported as missing.
		/// </summary>
		public bool TryGetPendingInvite(int characterId, out PartyInvite invite)
		{
			lock (_syncRoot)
			{
				if (!_pendingInvites.TryGetValue(characterId, out invite))
					return false;

				if (invite.ExpiresAt < DateTime.UtcNow)
				{
					_pendingInvites.Remove(characterId);
					invite = default;
					return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Same as <see cref="TryGetPendingInvite"/> but also removes
		/// the entry. Use when responding to the invite.
		/// </summary>
		public bool ConsumePendingInvite(int characterId, out PartyInvite invite)
		{
			lock (_syncRoot)
			{
				if (!_pendingInvites.TryGetValue(characterId, out invite))
					return false;

				_pendingInvites.Remove(characterId);

				if (invite.ExpiresAt < DateTime.UtcNow)
				{
					invite = default;
					return false;
				}

				return true;
			}
		}
	}

	/// <summary>
	/// One outstanding invite. Stored on the manager keyed by the
	/// invitee's character id.
	/// </summary>
	public readonly struct PartyInvite
	{
		public int InviterCharacterId { get; }
		public string InviterName { get; }
		public int PartyId { get; }
		public DateTime ExpiresAt { get; }

		public PartyInvite(int inviterCharacterId, string inviterName, int partyId, DateTime expiresAt)
		{
			this.InviterCharacterId = inviterCharacterId;
			this.InviterName = inviterName;
			this.PartyId = partyId;
			this.ExpiresAt = expiresAt;
		}
	}
}
