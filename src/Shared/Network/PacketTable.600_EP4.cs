﻿namespace Sabine.Shared.Network
{
	public static partial class PacketTable
	{
		private static void LoadVersion600()
		{
			// Reference: euRO EP4 Ragexe.exe, dated 2004-05-12

			// No major changes in this packet table, though the entry
			// packets changed slightly in size, most of them increasing
			// in size by two.

			// "Are we there yet???"
			// No, we still haven't reached eAthena packet version 5.
			// Which is interesting, because I've seen people say that
			// *Athena supports clients all the way back to 2003, but
			// this is a 2004 client. Either kRO clients were very
			// different and the packet tables changed years in advance,
			// or *Athena support doesn't actually go back that far.
			// The latter would make sense, because even though they
			// were working on the server in 2003, AppleMod did not
			// have a packet_db in December of that year yet. Packet
			// changes weren't being tracked, and I doubt that someone
			// went back through the changelogs and documented them.
			// Even eAthena's main branch didn't have a packet_db yet
			// in mid-2004. It was apparently added in late October
			// of that year, presumably based on the latest version
			// at the time, which was ~EP8.

			ChangeSize(Op.ZC_NOTIFY_STANDENTRY, 54);
			ChangeSize(Op.ZC_NOTIFY_NEWENTRY, 53);
			ChangeSize(Op.ZC_NOTIFY_ACTENTRY, 58);
			ChangeSize(Op.ZC_NOTIFY_MOVEENTRY, 60);

			// ZC_SPIRITS2, 0x01E1
			Register(Op.ZC_REQ_COUPLE, 0x01E2, 34);
			Register(Op.CZ_JOIN_COUPLE, 0x01E3, 14);
			Register(Op.ZC_START_COUPLE, 0x01E4, 2);
			Register(Op.CZ_REQ_JOIN_COUPLE, 0x01E5, 6);
			Register(Op.ZC_COUPLENAME, 0x01E6, 26);
			Register(Op.CZ_DORIDORI, 0x01E7, 2);
			Register(Op.CZ_MAKE_GROUP2, 0x01E8, 28);
			Register(Op.ZC_ADD_MEMBER_TO_GROUP2, 0x01E9, 81);
			Register(Op.ZC_CONGRATULATION, 0x01EA, 6);
			Register(Op.ZC_NOTIFY_POSITION_TO_GUILDM, 0x01EB, 10);
			Register(Op.ZC_GUILD_MEMBER_MAP_CHANGE, 0x01EC, 26);
			Register(Op.CZ_CHOPOKGI, 0x01ED, 2);
			Register(Op.ZC_NORMAL_ITEMLIST2, 0x01EE, -1);
			Register(Op.ZC_CART_NORMAL_ITEMLIST2, 0x01EF, -1);
			Register(Op.ZC_STORE_NORMAL_ITEMLIST2, 0x01F0, -1);
			Register(Op.AC_NOTIFY_ERROR, 0x01F1, -1);
			Register(Op.ZC_UPDATE_CHARSTAT2, 0x01F2, 20);
			Register(Op.ZC_NOTIFY_EFFECT2, 0x01F3, 10);
			Register(Op.ZC_REQ_EXCHANGE_ITEM2, 0x01F4, 32);
			Register(Op.ZC_ACK_EXCHANGE_ITEM2, 0x01F5, 9);
			Register(Op.ZC_REQ_BABY, 0x01F6, 34);
			Register(Op.CZ_JOIN_BABY, 0x01F7, 14);
			Register(Op.ZC_START_BABY, 0x01F8, 2);
			Register(Op.CZ_REQ_JOIN_BABY, 0x01F9, 6);
			Register(Op.CA_LOGIN3, 0x01FA, 48);
			Register(Op.CH_DELETE_CHAR2, 0x01FB, 56);
			Register(Op.ZC_REPAIRITEMLIST, 0x01FC, -1);
			Register(Op.CZ_REQ_ITEMREPAIR, 0x01FD, 4);
			Register(Op.ZC_ACK_ITEMREPAIR, 0x01FE, 5);
			Register(Op.ZC_HIGHJUMP, 0x01FF, 10);
		}
	}
}
