using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World
{
	/// <summary>
	/// Cross-player helpers for the friends subsystem. Sits above
	/// individual <see cref="Sabine.Zone.World.Actors.Components.Characters.FriendsList"/>
	/// instances so login / logout / mutual-add flows can iterate the
	/// world without each component reaching across actors directly.
	/// </summary>
	public static class FriendsService
	{
		/// <summary>
		/// Resolves an online character by char id. Returns null when the
		/// character isn't on the server (logged out, on a different
		/// shard, never existed).
		/// </summary>
		public static PlayerCharacter FindOnline(int charId)
		{
			if (charId == 0) return null;
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				if (map.TryGetPlayerById(charId, out var pc))
					return pc;
			}
			return null;
		}

		/// <summary>
		/// Resolves an online character by name (exact, case-insensitive).
		/// </summary>
		public static PlayerCharacter FindOnlineByName(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				if (map.TryGetPlayerByName(name, out var pc))
					return pc;
			}
			return null;
		}

		/// <summary>
		/// Notifies every online friend of <paramref name="character"/>
		/// that they have just changed online state. Used on login and on
		/// graceful logout.
		/// </summary>
		public static void BroadcastOnlineState(PlayerCharacter character, bool online)
		{
			var myCharId = character.Id;
			var myAccountId = character.Handle;
			var myName = character.Name;

			foreach (var entry in character.Friends.GetAll())
			{
				var friend = FindOnline(entry.CharId);
				if (friend == null) continue;
				Send.ZC_FRIENDS_STATE(friend, myAccountId, myCharId, myName, online);
			}
		}

		/// <summary>
		/// Performs the mutual add when both players have agreed: stores
		/// the entry on each side, sends an updated single-entry list to
		/// the original requester so their UI gets the row.
		/// </summary>
		public static bool MutualAdd(PlayerCharacter requester, PlayerCharacter target)
		{
			if (requester == null || target == null) return false;
			if (requester.Id == target.Id) return false;

			var requesterEntry = new Sabine.Zone.World.Actors.Components.Characters.FriendEntry
			{
				AccountId = requester.Handle,
				CharId = requester.Id,
				Name = requester.Name,
			};
			var targetEntry = new Sabine.Zone.World.Actors.Components.Characters.FriendEntry
			{
				AccountId = target.Handle,
				CharId = target.Id,
				Name = target.Name,
			};

			var addedToTarget = target.Friends.Add(requesterEntry);
			var addedToRequester = requester.Friends.Add(targetEntry);
			return addedToTarget || addedToRequester;
		}
	}
}
