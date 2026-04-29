using Sabine.Shared;
using Sabine.Shared.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Versioning.ManagedEnum;

namespace Sabine.Zone.Network
{
	public static partial class Send
	{
		/// <summary>
		/// Notifies the client that a status icon should appear or
		/// disappear on the given character. Op only exists from Beta2
		/// onward — older clients silently no-op.
		/// </summary>
		/// <param name="character">The character whose UI is updated. The
		/// packet is sent only to this player (status icons live on the
		/// owning client's UI).</param>
		/// <param name="iconId">SI_* id; see
		/// <c>Battle.StatusIcons.GetIcon</c>. An icon of 0 (paired with
		/// <paramref name="active"/>=false) clears the slot on the
		/// shorter Beta2 layout.</param>
		/// <param name="active">true to show, false to hide. On Beta2
		/// the on/off byte is absent — the wrapper sends iconId=0 when
		/// asked to deactivate so the client clears the existing icon.</param>
		public static void ZC_MSG_STATE_CHANGE(PlayerCharacter character, int iconId, bool active)
		{
			if (Game.Version < Versions.Beta2)
				return;

			using var packet = Packet.Rent(Op.ZC_MSG_STATE_CHANGE);

			// Beta2 layout (size 8): WORD cmd + WORD icon + DWORD aid.
			// EP3+ (size 9): adds a trailing on/off byte.
			var sentIcon = active ? iconId : 0;
			packet.PutShort((short)sentIcon);
			packet.PutInt(character.Handle);

			if (Game.Version >= Versions.S400)
				packet.PutByte(active);

			character.Connection.Send(packet);
		}
	}
}
