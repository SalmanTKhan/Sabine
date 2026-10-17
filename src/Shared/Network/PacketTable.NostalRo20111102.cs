namespace Sabine.Shared.Network
{
	public static partial class PacketTable
	{
		/// <summary>
		/// Final wire table for the 2011-11-02 standard RO client. The
		/// historical stack supplies common packet bodies; this overlay
		/// restores the date-specific wire ids and actor lengths evidenced by
		/// the local NostalRO reference.
		/// </summary>
		private static void LoadVersionNostalRo20111102()
		{
			// These S2500 aliases occupy wire slots claimed by the standard
			// 20111102 movement/name packets.
			Unregister(Op.ZC_MEMBER_EXIT);
			Unregister(Op.CZ_CHANGE_CHATROOM);
			Unregister(Op.CZ_REQ_ROLE_CHANGE);
			Unregister(Op.CZ_CONCLUDE_EXCHANGE_ITEM);
			Unregister(Op.ZC_CONCLUDE_EXCHANGE_ITEM);
			Unregister(Op.CZ_CANCEL_EXCHANGE_ITEM);
			Unregister(Op.ZC_ACK_MAKE_GROUP);
			Unregister(Op.UNK_0364);
			Unregister(Op.UNK_0366);

			Redefine(Op.HC_NOTIFY_ZONESVR, 0x0071, 28);
			Redefine(Op.CZ_ENTER, 0x083C, 19);
			Redefine(Op.ZC_ACCEPT_ENTER, 0x0073, 11);
			Redefine(Op.CZ_NOTIFY_ACTORINIT, 0x007D, 2);
			Redefine(Op.ZC_AID, 0x0283, 6);

			Redefine(Op.ZC_NOTIFY_STANDENTRY, 0x0078, 55);
			Redefine(Op.ZC_NOTIFY_NEWENTRY, 0x0079, 53);
			Redefine(Op.ZC_NOTIFY_MOVEENTRY, 0x007B, 60);
			Redefine(Op.ZC_NOTIFY_VANISH, 0x0080, 7);
			Redefine(Op.ZC_NOTIFY_MOVE, 0x0086, 16);
			Redefine(Op.ZC_NOTIFY_PLAYERMOVE, 0x0087, 12);
			Redefine(Op.ZC_STOPMOVE, 0x0088, 10);
			// Basic attack request/result packets use the standard
			// PACKETVER 20111102 ladder.  The S2500 table above carries the
			// older EP12 aliases (and an 8-byte padded request), which would
			// otherwise desynchronise the client as soon as combat starts.
			// Evidence: rust-ro packets at 20111102 and the local NostalRO
			// packet table: CZ_REQUEST_ACT=0x08AA/7,
			// ZC_NOTIFY_ACT=0x008A/29, CZ_CANCEL_LOCKON=0x0118/2.
			Redefine(Op.CZ_REQUEST_ACT, 0x08AA, 7);
			Redefine(Op.ZC_NOTIFY_ACT, 0x008A, 29);
			Redefine(Op.CZ_CANCEL_LOCKON, 0x0118, 2);
			Redefine(Op.ZC_ACK_REQNAME, 0x0095, 30);
			Redefine(Op.ZC_CHANGE_DIRECTION, 0x009C, 9);
			Redefine(Op.CZ_REQNAME, 0x088A, 6);
			Redefine(Op.CZ_REQUEST_MOVE, 0x0364, 5);
			Redefine(Op.CZ_CHANGE_DIRECTION, 0x0366, 5);
			Redefine(Op.ZC_PAR_CHANGE, 0x00B0, 8);
			Redefine(Op.ZC_LONGPAR_CHANGE, 0x00B1, 8);
			Redefine(Op.ZC_STATUS, 0x00BD, 44);
			Redefine(Op.ZC_NORMAL_ITEMLIST, 0x00A3, Dynamic);
			Redefine(Op.ZC_EQUIPMENT_ITEMLIST, 0x00A4, Dynamic);
			Redefine(Op.ZC_SKILLINFO_UPDATE, 0x010E, 11);
			Redefine(Op.ZC_SKILLINFO_LIST, 0x010F, Dynamic);
			Redefine(Op.ZC_ADD_SKILL, 0x0111, 39);
		}
	}
}
