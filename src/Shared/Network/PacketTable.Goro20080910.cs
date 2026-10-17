namespace Sabine.Shared.Network
{
	public static partial class PacketTable
	{
		private static void LoadVersionGoro20080910()
		{
			// The 2008 client does not consume the later ZC_AID prelude.
			// Remove it after the S2500 base has been loaded so this profile
			// cannot accidentally inherit the modern map-login handshake.
			Unregister(Op.ZC_AID);
			Unregister(Op.CH_DELETE_CHAR2);

			// Plain Sakexe packetdb from the goro rAthena branch. The entries
			// below intentionally override the S2500 table's shifted IDs and
			// packet bodies; the client is 2008-era despite using the S2500
			// effective data stack for Sabine's shared gameplay code.
			Redefine(Op.CZ_ENTER, 0x0436, 19);
			Redefine(Op.CZ_REQUEST_ACT, 0x0437, 7);
			Redefine(Op.CZ_USE_SKILL, 0x0438, 10);
			Redefine(Op.CZ_USE_ITEM, 0x0439, 8);
			Redefine(Op.CZ_REQUEST_TIME, 0x0089, 8);
			Redefine(Op.CZ_REQNAME, 0x008C, 11);
			Redefine(Op.CZ_REQUEST_MOVE, 0x00A7, 8);
			Redefine(Op.CZ_CHANGE_DIRECTION, 0x0085, 11);
			Redefine(Op.CZ_ITEM_PICKUP, 0x00F5, 8);
			Redefine(Op.CZ_ITEM_THROW, 0x0116, 10);
			Redefine(Op.CZ_MOVE_ITEM_FROM_BODY_TO_STORE, 0x0094, 14);
			Redefine(Op.CZ_MOVE_ITEM_FROM_STORE_TO_BODY, 0x00F7, 22);
			Redefine(Op.CZ_CLOSE_STORE, 0x0193, 2);
			Redefine(Op.CZ_USE_SKILL_TOGROUND, 0x0113, 22);
			Redefine(Op.CZ_USE_SKILL_TOGROUND_WITHTALKBOX, 0x007E, 102);
			Redefine(Op.CH_DELETE_CHAR, 0x01FB, 56);

			// rAthena's legacy actor serializers remain active for this
			// client, even though the S2500 table has the later *_ENTRY4
			// opcodes and sizes.
			Redefine(Op.ZC_NOTIFY_STANDENTRY, 0x0078, 55);
			Redefine(Op.ZC_NOTIFY_NEWENTRY, 0x0079, 53);
			Redefine(Op.ZC_NOTIFY_ACTENTRY, 0x007A, 58);
			Redefine(Op.ZC_NOTIFY_MOVEENTRY, 0x007B, 60);
			Redefine(Op.ZC_NOTIFY_STANDENTRY_NPC, 0x007C, 42);
			Redefine(Op.ZC_NOTIFY_ACT, 0x008A, 29);
			Redefine(Op.ZC_NOTIFY_ACT_POSITION, 0x008B, 23);
			Redefine(Op.ZC_ITEM_ENTRY, 0x009E, 17);
			Redefine(Op.ZC_ITEM_FALL_ENTRY, 0x009E, 17);
			Redefine(Op.ZC_USE_ITEM_ACK, 0x00A8, 7);
			Redefine(Op.ZC_REQ_WEAR_EQUIP_ACK, 0x00AA, 7);
			Redefine(Op.ZC_REQ_TAKEOFF_EQUIP_ACK, 0x00AC, 7);
			Redefine(Op.ZC_PAR_CHANGE, 0x00B0, 8);
			Redefine(Op.ZC_LONGPAR_CHANGE, 0x00B1, 8);
			Redefine(Op.ZC_STATUS_CHANGE_ACK, 0x00BC, 6);
			Redefine(Op.ZC_STATUS, 0x00BD, 44);
			Redefine(Op.ZC_STATUS_CHANGE, 0x00BE, 5);
			Redefine(Op.ZC_SKILLINFO_UPDATE, 0x010E, 11);
			Redefine(Op.ZC_SKILLINFO_LIST, 0x010F, Dynamic);
			Redefine(Op.ZC_ADD_SKILL, 0x0111, 39);
			Redefine(Op.ZC_NOTIFY_SKILL, 0x0114, 31);
			Redefine(Op.ZC_NOTIFY_SKILL_POSITION, 0x0115, 35);
			Redefine(Op.ZC_NOTIFY_GROUNDSKILL, 0x0117, 18);
			Redefine(Op.ZC_ACCEPT_ENTER, 0x02EB, 13);
			Redefine(Op.ZC_NORMAL_ITEMLIST, 0x02E8, Dynamic);
			Redefine(Op.ZC_EQUIPMENT_ITEMLIST, 0x02D0, Dynamic);

			// Some clients in the same family sent the old dynamic action
			// packet. Accept it inbound while keeping 0x0437 canonical for
			// outbound traffic.
			RegisterInboundAlias(0x0190, Op.CZ_REQUEST_ACT, 19);
		}
	}
}
