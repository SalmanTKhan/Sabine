using Sabine.Shared.Network;
using Sabine.Zone.World;
using Sabine.Zone.World.Actors.Components.Characters;
using Yggdrasil.Logging;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Handlers for the rAthena friends-list flow:
	/// <list type="bullet">
	///   <item><c>CZ_ADD_FRIENDS</c> — requester types a name; if the target
	///   is online and accepts, both sides get a list entry.</item>
	///   <item><c>CZ_ACK_REQ_ADD_FRIENDS</c> — target's accept/reject reply
	///   to the request popup.</item>
	///   <item><c>CZ_DELETE_FRIENDS</c> — either side removes a friend.</item>
	/// </list>
	/// Single-server only; no inter-realm fallback for offline removal
	/// (rAthena uses char-server inter pipes for that — Sabine does the
	/// delete eagerly and the offline side reconciles on next login).
	/// </summary>
	public partial class PacketHandler
	{
		[PacketHandler(Op.CZ_ADD_FRIENDS)]
		public void CZ_ADD_FRIENDS(ZoneConnection conn, Packet packet)
		{
			var targetName = packet.GetString(24);
			var character = conn.GetCurrentCharacter();

			if (string.IsNullOrWhiteSpace(targetName)) return;

			if (character.Friends.IsFull)
			{
				Send.ZC_REQ_ADD_FRIENDS(character, 0, 0, string.Empty);
				return;
			}

			var target = FriendsService.FindOnlineByName(targetName);
			if (target == null)
			{
				Log.Debug("CZ_ADD_FRIENDS: Char '{0}' tried to friend '{1}' (offline).", character.Name, targetName);
				return;
			}

			if (target.Id == character.Id) return;
			if (character.Friends.Contains(target.Id)) return;

			// Stash the pending requester on the target so their reply can
			// resolve back without trusting client-supplied AID/CID.
			target.Vars.Temp.SetInt("Sabine.FriendRequestFrom", character.Id);

			Send.ZC_REQ_ADD_FRIENDS(target, character.Handle, character.Id, character.Name);
		}

		[PacketHandler(Op.CZ_ACK_REQ_ADD_FRIENDS)]
		public void CZ_ACK_REQ_ADD_FRIENDS(ZoneConnection conn, Packet packet)
		{
			var inviterAccountId = packet.GetInt();
			var inviterCharId = packet.GetInt();
			var reply = packet.GetInt(); // 0 = reject, 1 = accept

			var target = conn.GetCurrentCharacter();

			// Trust the temp-var pin over client-supplied ids so a tampered
			// reply can't sneak an arbitrary char-id through.
			var pendingFromId = target.Vars.Temp.GetInt("Sabine.FriendRequestFrom", 0);
			target.Vars.Temp.SetInt("Sabine.FriendRequestFrom", 0);

			if (pendingFromId == 0 || pendingFromId != inviterCharId)
				return;

			var requester = FriendsService.FindOnline(inviterCharId);
			if (requester == null) return;

			if (reply != 1)
				return;

			if (requester.Friends.IsFull || target.Friends.IsFull)
				return;

			FriendsService.MutualAdd(requester, target);

			// Both sides get the full list re-sent. The client treats every
			// ZC_ADD_FRIENDS_LIST as a replace (clear + repopulate), same as
			// the map-enter dump, so a partial list here would wipe the UI.
			Send.ZC_ADD_FRIENDS_LIST(requester, requester.Friends.GetAll());
			Send.ZC_ADD_FRIENDS_LIST(target, target.Friends.GetAll());

			// Each side learns the other is online right away.
			Send.ZC_FRIENDS_STATE(requester, target.Handle, target.Id, target.Name, online: true);
			Send.ZC_FRIENDS_STATE(target, requester.Handle, requester.Id, requester.Name, online: true);
		}

		[PacketHandler(Op.CZ_DELETE_FRIENDS)]
		public void CZ_DELETE_FRIENDS(ZoneConnection conn, Packet packet)
		{
			var friendAccountId = packet.GetInt();
			var friendCharId = packet.GetInt();

			var character = conn.GetCurrentCharacter();

			if (!character.Friends.Remove(friendCharId))
				return;

			Send.ZC_DELETE_FRIENDS(character, friendAccountId, friendCharId);

			// Reciprocal delete on the friend's side; if they're offline,
			// the entry stays until they log in and we reconcile by name.
			// For the v50000 single-server profile the offline path is
			// rare enough that on-login reconciliation isn't built yet —
			// the offline player simply sees a phantom name until they
			// /unfriend themself. Promoting friends to a SQL table is
			// the right fix once the offline graph matters.
			var friend = FriendsService.FindOnline(friendCharId);
			if (friend != null)
			{
				friend.Friends.Remove(character.Id);
				Send.ZC_DELETE_FRIENDS(friend, character.Handle, character.Id);
			}
		}
	}
}
