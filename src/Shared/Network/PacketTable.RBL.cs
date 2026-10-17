using System;
using System.Collections.Generic;

namespace Sabine.Shared.Network
{
	public static partial class PacketTable
	{
		/// <summary>
		/// roBrowser / rAthena standard wire-ID protocol profile.
		/// Stacks body sizes from Alpha through S2500, then remaps
		/// all wire opcodes to the standard RO client IDs (0x0064+).
		///
		/// This makes Sabine speak the same wire protocol as
		/// rAthena, roBrowser (RBL), and AppleClient so that
		/// standard RO clients and tools can connect directly.
		///
		/// Wire IDs are based on the 2003-era modern RO protocol,
		/// which is the base that all later PACKETVERs extend.
		/// Ops not mapped here are assigned to a safe 0x8000+
		/// range to avoid collisions with the standard range.
		/// </summary>
		private static void LoadVersionRBL()
		{
			// ── Body-shape overrides (S2500 / rA-latest layouts) ──

			ChangeSize(Op.AC_REFUSE_LOGIN, 26);
			ChangeSize(Op.HC_NOTIFY_ZONESVR, 156);
			ChangeSize(Op.CH_MAKE_CHAR, 36);
			ChangeSize(Op.CH_DELETE_CHAR, 56);
			Unregister(Op.CH_DELETE_CHAR2);
			ChangeSize(Op.ZC_ACCEPT_ENTER, 14);
			ChangeSize(Op.ZC_NPCACK_SERVERMOVE, 156);
			ChangeSize(Op.CZ_REQUEST_TIME, 6);
			ChangeSize(Op.ZC_NOTIFY_STANDENTRY, 108);
			ChangeSize(Op.ZC_NOTIFY_STANDENTRY_NPC, 108);
			ChangeSize(Op.ZC_NOTIFY_NEWENTRY, 107);
			ChangeSize(Op.ZC_NOTIFY_MOVEENTRY, 114);
			ChangeSize(Op.ZC_NOTIFY_ACT, 34);
			ChangeSize(Op.ZC_RECOVERY, 8);
			ChangeSize(Op.CZ_USE_SKILL, 10);
			ChangeSize(Op.CZ_USE_SKILL_TOGROUND, 10);
			ChangeSize(Op.CZ_REQUEST_MOVE, 5);
			ChangeSize(Op.CZ_REQUEST_ACT, 7);
			ChangeSize(Op.CZ_CHANGE_DIRECTION, 5);
			ChangeSize(Op.ZC_SPRITE_CHANGE, 15);
			ChangeSize(Op.ZC_REQ_WEAR_EQUIP_ACK, 11);
			ChangeSize(Op.ZC_REQ_TAKEOFF_EQUIP_ACK, 9);
			ChangeSize(Op.ZC_ITEM_ENTRY, 19);
			ChangeSize(Op.ZC_ITEM_FALL_ENTRY, 24);
			ChangeSize(Op.ZC_USE_ITEM_ACK, 15);
			ChangeSize(Op.ZC_ADD_ITEM_TO_CART, 49);
			ChangeSize(Op.ZC_ADD_ITEM_TO_STORE, 49);
			ChangeSize(Op.ZC_ADD_EXCHANGE_ITEM, 53);
			ChangeSize(Op.ZC_USESKILL_ACK, 29);
			ChangeSize(Op.ZC_STATE_CHANGE, 15);
			ChangeSize(Op.ZC_NOTIFY_HP_TO_GROUPM, 14);
			ChangeSize(Op.ZC_WARPLIST, Dynamic);
			ChangeSize(Op.ZC_ITEM_PICKUP_ACK, 61);
			ChangeSize(Op.ZC_ITEM_PICKUP_PARTY, 24);
			ChangeSize(Op.ZC_ACK_REQMAKINGITEM, 8);
			ChangeSize(Op.ZC_FEED_PET, 7);
			ChangeSize(Op.ZC_PROPERTY_PET, 37);
			ChangeSize(Op.CZ_ITEM_PICKUP, 6);
			ChangeSize(Op.CZ_ITEM_THROW, 6);
			ChangeSize(Op.ZC_ADD_FRIENDS_LIST, Dynamic);
			ChangeSize(Op.ZC_FRIENDS_STATE, 35);

			// ── Wire-ID remapping ──────────────────────────────────

			// Standard RO wire IDs (2003-era modern protocol).
			// Verified against AppleClient ProtocolManager._modernOps
			// and rAthena packet_db.txt for the critical login/char ops.
			// Ops not listed here → fallback to 0x8000+.

			var rblOps = new Dictionary<Op, int>
			{
				// ── Auth (CA → AC) ──────────────────────────────
				[Op.CA_LOGIN]                  = 0x0064,
				[Op.CA_LOGIN2]                 = 0x01DD,
				[Op.CA_LOGIN3]                 = 0x01FA,
				[Op.CA_LOGIN4]                 = 0x027C,
				[Op.CA_LOGIN_PCBANG]           = 0x02B1,
				[Op.CA_LOGIN_HAN]              = 0x02B7,
				[Op.AC_ACCEPT_LOGIN]           = 0x0069,
				[Op.AC_REFUSE_LOGIN]           = 0x006A,
				[Op.CA_CONNECT_INFO_CHANGED]   = 0x0200,
				[Op.CA_EXE_HASHCHECK]          = 0x0204,
				[Op.CA_REQ_HASH]               = 0x01DB,
				[Op.AC_ACK_HASH]               = 0x01DC,
				[Op.CA_REQ_GAME_GUARD_CHECK]   = 0x022D,
				[Op.AC_ACK_GAME_GUARD]         = 0x022E,
				[Op.AC_ASK_PNGAMEROOM]         = 0x0273,
				[Op.CA_REPLY_PNGAMEROOM]       = 0x0274,
				[Op.AC_NOTIFY_ERROR]           = 0x01F1,
				[Op.CA_CLIENT_TYPE]            = 0x02B8,
				[Op.AC_REQUEST_SECOND_PASSWORD] = 0x08C2,

				// ── Char (CH → HC) ──────────────────────────────
				[Op.CH_ENTER]                  = 0x0065,
				[Op.CH_SELECT_CHAR]            = 0x0066,
				[Op.CH_MAKE_CHAR]              = 0x0067,
				[Op.CH_DELETE_CHAR]            = 0x0068,
				[Op.CH_DELETE_CHAR2]           = 0x01FB,
				[Op.HC_ACCEPT_ENTER]           = 0x006B,
				[Op.HC_REFUSE_ENTER]           = 0x006C,
				[Op.HC_ACCEPT_MAKECHAR]        = 0x006D,
				[Op.HC_REFUSE_MAKECHAR]        = 0x006E,
				[Op.HC_ACCEPT_DELETECHAR]      = 0x006F,
				[Op.HC_REFUSE_DELETECHAR]      = 0x0070,
				[Op.HC_NOTIFY_ZONESVR]         = 0x0071,
				[Op.HC_BLOCK_CHARACTER]        = 0x0202,
				[Op.CH_EXE_HASHCHECK]          = 0x0205,

				// ── Zone C→S (CZ) ───────────────────────────────
				[Op.CZ_ENTER]                  = 0x0072,
				[Op.CZ_NOTIFY_ACTORINIT]       = 0x007D,
				[Op.CZ_REQUEST_TIME]           = 0x007E,
				[Op.CZ_REQUEST_MOVE]           = 0x0085,
				[Op.CZ_REQUEST_ACT]            = 0x0089,
				[Op.CZ_REQUEST_CHAT]           = 0x008C,
				[Op.CZ_CONTACTNPC]             = 0x0090,
				[Op.CZ_REQNAME]                = 0x0094,
				[Op.CZ_REQNAME_BYGID]          = 0x0193,
				[Op.CZ_WHISPER]                = 0x0096,
				[Op.CZ_BROADCAST]              = 0x0099,
				[Op.CZ_CHANGE_DIRECTION]       = 0x009B,
				[Op.CZ_ITEM_PICKUP]            = 0x009F,
				[Op.CZ_ITEM_THROW]             = 0x00A2,
				[Op.CZ_USE_ITEM]               = 0x00A7,
				[Op.CZ_REQ_WEAR_EQUIP]         = 0x00A9,
				[Op.CZ_REQ_TAKEOFF_EQUIP]      = 0x00AB,
				[Op.CZ_RESTART]                = 0x00B2,
				[Op.CZ_REQ_EMOTION]            = 0x00BF,
				[Op.CZ_USE_SKILL]              = 0x0113,
				[Op.CZ_USE_SKILL_TOGROUND]     = 0x0116,

				// ── Zone S→C (ZC) ───────────────────────────────
				[Op.ZC_ACCEPT_ENTER]           = 0x0073,
				[Op.ZC_REFUSE_ENTER]           = 0x0074,
				[Op.ZC_NOTIFY_INITCHAR]        = 0x007D,
				[Op.ZC_NOTIFY_UPDATECHAR]      = 0x007E,
				[Op.ZC_NOTIFY_UPDATEPLAYER]    = 0x007F,
				[Op.ZC_NOTIFY_STANDENTRY]      = 0x0080,
				[Op.SC_NOTIFY_BAN]             = 0x0081,
				[Op.ZC_NOTIFY_NEWENTRY]        = 0x0082,
				[Op.ZC_NOTIFY_ACTENTRY]        = 0x0083,
				[Op.ZC_NOTIFY_MOVEENTRY]       = 0x0084,
				[Op.ZC_NOTIFY_STANDENTRY_NPC]  = 0x0085,
				[Op.ZC_NOTIFY_TIME]            = 0x0086,
				[Op.ZC_NOTIFY_VANISH]          = 0x0087,
				[Op.ZC_NOTIFY_PLAYERMOVE]      = 0x0088,
				[Op.ZC_STOPMOVE]               = 0x0089,
				[Op.ZC_NOTIFY_ACT]             = 0x008A,
				[Op.ZC_NOTIFY_ACT_POSITION]    = 0x008B,
				[Op.ZC_NOTIFY_CHAT]            = 0x008D,
				[Op.ZC_NOTIFY_PLAYERCHAT]      = 0x008E,
				[Op.ZC_NPCACK_MAPMOVE]         = 0x0091,
				[Op.ZC_NPCACK_SERVERMOVE]      = 0x0092,
				[Op.ZC_NPCACK_ENABLE]          = 0x0093,
				[Op.ZC_ACK_REQNAME]            = 0x0095,
				[Op.ZC_WHISPER]                = 0x0097,
				[Op.ZC_ACK_WHISPER]            = 0x0098,
				[Op.SERVER_ENTRY_ACK]          = 0x009A,
				[Op.ZC_BROADCAST]              = 0x009A,  // (same wire as SERVER_ENTRY_ACK in some versions)
				[Op.ZC_CHANGE_DIRECTION]       = 0x009C,
				[Op.ZC_ITEM_ENTRY]             = 0x009D,
				[Op.ZC_ITEM_FALL_ENTRY]        = 0x009E,
				[Op.ZC_ITEM_PICKUP_ACK]        = 0x00A0,
				[Op.ZC_ITEM_DISAPPEAR]         = 0x00A1,
				[Op.ZC_NORMAL_ITEMLIST]        = 0x00A3,
				[Op.ZC_EQUIPMENT_ITEMLIST]     = 0x00A4,
				[Op.ZC_STORE_NORMAL_ITEMLIST]  = 0x00A5,
				[Op.ZC_STORE_EQUIPMENT_ITEMLIST] = 0x00A6,
				[Op.ZC_USE_ITEM_ACK]           = 0x00A8,
				[Op.ZC_REQ_WEAR_EQUIP_ACK]     = 0x00AA,
				[Op.ZC_REQ_TAKEOFF_EQUIP_ACK]  = 0x00AC,
				[Op.ZC_ITEM_THROW_ACK]         = 0x00B1,
				[Op.ZC_PAR_CHANGE]             = 0x00B3,
				[Op.ZC_LONGPAR_CHANGE]         = 0x00BE,
				[Op.ZC_RESTART_ACK]            = 0x00B4,
				[Op.ZC_SAY_DIALOG]             = 0x00B5,
				[Op.ZC_WAIT_DIALOG]            = 0x00B6,
				[Op.ZC_CLOSE_DIALOG]           = 0x00B7,
				[Op.ZC_MENU_LIST]              = 0x00B8,
				[Op.ZC_STATUS_CHANGE_ACK]      = 0x00BD,
				[Op.ZC_STATUS]                 = 0x00C0,
				[Op.ZC_STATUS_CHANGE]          = 0x00C3,
				[Op.ZC_EMOTION]                = 0x00C2,
				[Op.ZC_SPRITE_CHANGE]          = 0x00C5,
				[Op.ZC_NOTIFY_MOVE]            = 0x008C,
				[Op.ZC_STATE_CHANGE]           = 0x0123,
				[Op.ZC_RECOVERY]               = 0x013F,
				[Op.ZC_USESKILL_ACK]           = 0x0144,

				// ── Misc ─────────────────────────────────────────
				[Op.PING]                      = 0x0187,
				[Op.ZC_ACK_REQ_DISCONNECT]     = 0x018A,
				[Op.ZC_ACK_REQNAME_BYGID]      = 0x0194,
			};

			// Detect duplicate wire IDs from pre-RBL stacking BEFORE
			// applying the RBL remapping.  This catches any alpha-era
			// collisions that would silently corrupt BuildLists().
			var seen = new HashSet<int>();
			foreach (var entry in Entries)
			{
				if (!seen.Add(entry.OpNetwork))
					throw new InvalidOperationException(
						$"Duplicate wire ID 0x{entry.OpNetwork:X4} for {entry.Op} " +
						$"(pre-RBL stacking collision); fix the mapping or unregister one op.");
			}

			// Apply standard RO wire IDs where mapped; fallback to 0x8000+.
			var nextFallback = 0x8000;
			foreach (var entry in Entries)
			{
				if (rblOps.TryGetValue(entry.Op, out var rblId))
					entry.OpNetwork = rblId;
				else
					entry.OpNetwork = nextFallback++;
			}
		}
	}
}
