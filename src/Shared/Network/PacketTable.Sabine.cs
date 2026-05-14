using System;
using System.Collections.Generic;

namespace Sabine.Shared.Network
{
	public static partial class PacketTable
	{
		/// <summary>
		/// Sabine-native protocol profile for the open-midgard custom
		/// client. Stacks on top of every historical era table (Alpha
		/// through S2500) so packet body sizes inherit the most-recent
		/// effective layout, then renumbers every registered op into a
		/// clean grouped wire-ID layout:
		///
		/// <list type="bullet">
		///   <item>0x0001 - 0x00FF: Auth (CA_* / AC_*)</item>
		///   <item>0x0100 - 0x01FF: Char (CH_* / HC_*)</item>
		///   <item>0x0200 - 0x03FF: Zone client to server (CZ_*)</item>
		///   <item>0x0400 - 0x0FFF: Zone server to client (ZC_*)</item>
		///   <item>0x4000 +     : Misc (PING, SC_*, SERVER_ENTRY_ACK, etc.)</item>
		/// </list>
		///
		/// UNK_* placeholders left over from the historical-client
		/// pre-reservation style are dropped — Sabine does not need
		/// to reserve wire slots for client features it isn't going
		/// to ship.
		/// </summary>
		private static void LoadVersionSabine()
		{
			// Body-shape upgrades to rAthena-latest layouts. Sizes
			// inherited from S2500 stacking get overridden here so the
			// framer reads/writes the right number of bytes for each
			// rA-upgraded sender/handler.
			//
			// AC_REFUSE_LOGIN: rA PACKETVER >= 20120000 widens error
			// from u8 to u32. Body = 4 + 20 = 24, wire = 26.
			// Ref: rathena packets.hpp:225, loginclif.cpp logclif_auth_failed.
			ChangeSize(Op.AC_REFUSE_LOGIN, 26);

			// HC_NOTIFY_ZONESVR: rA PACKETVER >= 20170315 appends 128-byte
			// domain field for hostname-based zone routing. Body = 4 CID +
			// 16 mapname + 4 ip + 2 port + 128 domain = 154, wire = 156.
			// Ref: rathena packets.hpp:291, char_clif.cpp chclif_send_map_data.
			ChangeSize(Op.HC_NOTIFY_ZONESVR, 156);

			// CH_MAKE_CHAR: rA PACKETVER >= 20151001 carries name[24] +
			// slot + hair_color(u16) + hair_style(u16) + job(u32) + sex(u8),
			// wire = 2 + 24 + 1 + 2 + 2 + 4 + 1 = 36. Drops the classic
			// per-stat distribution (str/agi/vit/int/dex/luk) — stats are
			// server-defaulted on the modern flow.
			// Ref: rathena packets.hpp:122, char_clif.cpp chclif_parse_CharMake.
			ChangeSize(Op.CH_MAKE_CHAR, 36);

			// CH_DELETE_CHAR: rA PACKETVER >= 20040419 widened the email/
			// key field from char[40] to char[50]. Body = 4 CID + 50 key,
			// wire = 56. Sabine collapses CH_DELETE_CHAR2 into this single
			// modern op below.
			// Ref: rathena packets.hpp:159, char_clif.cpp chclif_parse_delchar.
			ChangeSize(Op.CH_DELETE_CHAR, 56);

			// Sabine ships one delete op. The historical CH_DELETE_CHAR2
			// (EP4-era 0x1FB shadow) is collapsed away — open-midgard
			// sends the rA-modern 56-byte body via CH_DELETE_CHAR.
			Unregister(Op.CH_DELETE_CHAR2);

			// ZC_ACCEPT_ENTER: rA PACKETVER >= 20141022 (except the
			// 20141022-20160329 window which uses 0x2eb) appends a 1-byte
			// sex field. Body = 4 startTime + 3 posDir + 1 xSize + 1 ySize
			// + 2 font + 1 sex = 12, wire = 14.
			// Ref: rathena packets.hpp:573, map/clif.cpp clif_authok.
			ChangeSize(Op.ZC_ACCEPT_ENTER, 14);

			// ZC_NPCACK_SERVERMOVE: rA PACKETVER >= 20170315 appends a
			// 128-byte domain field for hostname-based zone routing.
			// Body = 16 mapName + 2 xPos + 2 yPos + 4 ip + 2 port + 128
			// domain = 154, wire = 156.
			// Ref: rathena packets.hpp:706, map/clif.cpp clif_changemapserver.
			ChangeSize(Op.ZC_NPCACK_SERVERMOVE, 156);

			// CZ_REQUEST_TIME: rA at 0x0360 PACKETVER >= 20101124 is a
			// clean 6-byte clientTick request. S2500's PV23 redefine to
			// 0x0089 carries an 8-byte body — Sabine drops that extension
			// and matches rA-latest exactly. Body = 4 clientTick, wire = 6.
			// Ref: rathena clif_packetdb.hpp:1382.
			ChangeSize(Op.CZ_REQUEST_TIME, 6);

			// Actor-entry triad: rA's packet_idle_unit / packet_spawn_unit /
			// packet_unit_walking grew significantly between EP12 and
			// PACKETVER_MAIN_NUM >= 20181121. Adds packetLength + objecttype
			// (20091103), AID (20131223), shield (20181121), robe (20101124),
			// maxHP/HP/isBoss (20120221), body (20150513), name[24] (20131223).
			// Wire sizes: idle=108 (with `state` byte), spawn=107 (no state),
			// walking=114 (no state, MoveData[6] in place of PosDir[3], adds
			// moveStartTime). See ZoneServer/Network/Helpers/Character.cs
			// AddStandEntrySabine / AddNewEntrySabine / AddMoveEntrySabine.
			// Ref: rathena packets_struct.hpp:687, 758, 832; clif.cpp
			// clif_set_unit_idle, clif_spawn_unit, clif_set_unit_walking.
			ChangeSize(Op.ZC_NOTIFY_STANDENTRY, 108);
			ChangeSize(Op.ZC_NOTIFY_STANDENTRY_NPC, 108);
			ChangeSize(Op.ZC_NOTIFY_NEWENTRY, 107);
			ChangeSize(Op.ZC_NOTIFY_MOVEENTRY, 114);

			// ZC_NOTIFY_ACT: rA at 0x8c8 PACKETVER >= 20131223 widens
			// damage and damage2 from int16 to int32 and inserts a 1-byte
			// isSPDamage flag between damage and div. Body = 4 srcID + 4
			// targetID + 4 serverTick + 4 srcSpeed + 4 dmgSpeed + 4 damage
			// + 1 isSPDamage + 2 div + 1 type + 4 damage2 = 32, wire = 34.
			// Ref: rathena packets.hpp:1413, map/clif.cpp clif_damage.
			ChangeSize(Op.ZC_NOTIFY_ACT, 34);

			// ZC_RECOVERY: rA at 0xa27 PACKETVER >= 20141022 widens the
			// recovery amount from int16 to int32. Body = 2 type + 4
			// amount, wire = 8.
			// Ref: rathena packets.hpp:1210, map/clif.cpp clif_heal.
			ChangeSize(Op.ZC_RECOVERY, 8);

			// CZ_USE_SKILL / CZ_USE_SKILL_TOGROUND: S2500's PV23 inflates
			// these to 22 bytes with garbage padding (kRO obfuscation
			// vestige). The rA-latest base shape at 0x0113 / 0x0116 is a
			// clean 10 bytes: 2 skillLv + 2 skillId + 4 targetId (or
			// 2 x + 2 y), wire = 10. Sabine drops the obfuscation padding.
			// Ref: rathena clif_packetdb.hpp:111,114; map/clif.cpp
			// clif_parse_UseSkillToId / clif_parse_UseSkillToPos.
			ChangeSize(Op.CZ_USE_SKILL, 10);
			ChangeSize(Op.CZ_USE_SKILL_TOGROUND, 10);

			// CZ_REQUEST_MOVE: S2500's PV23 inflates to 11 bytes with
			// obfuscation filler. rA at 0x0085 / 0x0035F is a clean
			// 5-byte body: 3-byte PackedPosition.
			// Ref: rathena clif_packetdb.hpp packet(0x0085,5).
			ChangeSize(Op.CZ_REQUEST_MOVE, 5);

			// CZ_REQUEST_ACT: S2500 has 8 bytes with 1-byte padding;
			// rA classic is 7 bytes: 4 targetHandle + 1 action.
			// Ref: rathena clif_packetdb.hpp packet(0x0089,7).
			ChangeSize(Op.CZ_REQUEST_ACT, 7);

			// CZ_CHANGE_DIRECTION: S2500 inflates to 26 bytes for
			// obfuscation. rA at 0x0085 / 0x0361 is 5 bytes: 2 headDir +
			// 1 dir.
			// Ref: rathena clif_packetdb.hpp packet(0x009b,5).
			ChangeSize(Op.CZ_CHANGE_DIRECTION, 5);

			// ZC_SPRITE_CHANGE: rA at PACKETVER_MAIN_NUM >= 20181121
			// widens val and adds val2 (both uint32) — body = 4 AID +
			// 1 type + 4 val + 4 val2, wire = 15.
			// Ref: rathena packets_struct.hpp:2591.
			ChangeSize(Op.ZC_SPRITE_CHANGE, 15);

			// ZC_REQ_WEAR_EQUIP_ACK: rA at 0x0999 PACKETVER_MAIN_NUM >=
			// 20121205 widens wearLocation to uint32 and adds
			// wItemSpriteNumber (uint16). Body = 2 index + 4 wearLocation
			// + 2 spriteNum + 1 result, wire = 11.
			// Ref: rathena packets_struct.hpp:1269.
			ChangeSize(Op.ZC_REQ_WEAR_EQUIP_ACK, 11);

			// ZC_REQ_TAKEOFF_EQUIP_ACK: rA at 0x099a PACKETVER >=
			// 20130000 widens wearLocation to uint32. Body = 2 index +
			// 4 wearLocation + 1 flag, wire = 9.
			// Ref: rathena packets.hpp:1007.
			ChangeSize(Op.ZC_REQ_TAKEOFF_EQUIP_ACK, 9);

			// ZC_ITEM_ENTRY: rA at PACKETVER_MAIN_NUM >= 20181121 widens
			// itemId to uint32. Body = 4 AID + 4 itemId + 1 identify +
			// 2 x + 2 y + 2 amount + 1 subX + 1 subY = 17, wire = 19.
			// Ref: rathena packets_struct.hpp:2350.
			ChangeSize(Op.ZC_ITEM_ENTRY, 19);

			// ZC_ITEM_FALL_ENTRY (rA packet_dropflooritem): at PACKETVER
			// >= 20180418 adds showdropeffect + dropeffectmode tail.
			// Body = 4 AID + 4 itemId + 2 type + 1 IsIdentified + 2 x +
			// 2 y + 1 subX + 1 subY + 2 count + 1 showdropeffect +
			// 2 dropeffectmode = 22, wire = 24.
			// Ref: rathena packets_struct.hpp:597.
			ChangeSize(Op.ZC_ITEM_FALL_ENTRY, 24);

			// ZC_USE_ITEM_ACK: rA at PACKETVER_MAIN_NUM >= 20181121
			// widens itemId to uint32 and inserts AID after it. Body =
			// 2 index + 4 itemId + 4 AID + 2 amount + 1 result, wire = 15.
			// Ref: rathena packets_struct.hpp:2577.
			ChangeSize(Op.ZC_USE_ITEM_ACK, 15);

			// ZC_ADD_ITEM_TO_CART: rA 0x0a0b at PACKETVER_MAIN_NUM >=
			// 20181121. Body = 2 index + 4 amount + 4 itemId + 1 itemType
			// + 1 identified + 1 damaged + 1 refine + 8 slot + 25
			// option_data = 47, wire = 49.
			// Ref: rathena packets_struct.hpp:2483.
			ChangeSize(Op.ZC_ADD_ITEM_TO_CART, 49);

			// ZC_ADD_ITEM_TO_STORE: rA 0x0a0a at PACKETVER_MAIN_NUM >=
			// 20181121. Same V6 layout as cart. Wire = 49.
			// Ref: rathena packets_struct.hpp:2386.
			ChangeSize(Op.ZC_ADD_ITEM_TO_STORE, 49);

			// ZC_ADD_EXCHANGE_ITEM: rA 0x0a96 at PACKETVER_MAIN_NUM >=
			// 20181121 (pre-grade era). Body = 4 itemId + 1 itemType +
			// 4 amount + 1 identified + 1 damaged + 1 refine + 8 slot +
			// 25 option_data + 4 location + 2 look = 51, wire = 53.
			// Ref: rathena packets_struct.hpp:2608.
			ChangeSize(Op.ZC_ADD_EXCHANGE_ITEM, 53);

			// ZC_USESKILL_ACK: rA 0x0b1a at PACKETVER_MAIN_NUM >=
			// 20181212 appends uint32 attackMT after the disposable
			// byte. Body = 4 srcId + 4 dstId + 2 x + 2 y + 2 skillId +
			// 4 element + 4 delayTime + 1 disposable + 4 attackMT = 27,
			// wire = 29.
			// Ref: rathena packets_struct.hpp:3952.
			ChangeSize(Op.ZC_USESKILL_ACK, 29);

			// ZC_STATE_CHANGE: rA 0x0229 at PACKETVER >= 7 widens
			// effectState (opt3) from int16 to int32. Body = 4 AID +
			// 2 bodyState + 2 healthState + 4 effectState + 1 isPKModeON
			// = 13, wire = 15.
			// Ref: rathena packets_struct.hpp:4532.
			ChangeSize(Op.ZC_STATE_CHANGE, 15);

			// ZC_NOTIFY_HP_TO_GROUPM: rA 0x080e at PACKETVER >= 20100119
			// widens hp/maxhp from int16 to int32. Body = 4 AID + 4 hp +
			// 4 maxhp = 12, wire = 14. (Sabine writer was already
			// emitting int32 at Beta1+; this fixes a pre-existing
			// writer/table mismatch.)
			// Ref: rathena packets_struct.hpp:5131.
			ChangeSize(Op.ZC_NOTIFY_HP_TO_GROUPM, 14);

			// ZC_WARPLIST: rA at PACKETVER_MAIN_NUM >= 20170502 makes
			// the packet variable-length with an explicit packetLength
			// field. Sabine emits packetLength + skillId + N × 16-byte
			// map names. The fixed 68-byte Beta2 layout is dropped.
			// Ref: rathena packets_struct.hpp:3613.
			ChangeSize(Op.ZC_WARPLIST, Dynamic);

			// ZC_ITEM_PICKUP_PARTY: rA at PACKETVER_MAIN_NUM >= 20181121
			// widens itemId to uint32. Body = 4 AID + 4 itemId +
			// 1 identified + 1 damaged + 1 refine + 8 slot + 2 location +
			// 1 itemType = 22, wire = 24.
			// Ref: rathena packets_struct.hpp:2692.
			ChangeSize(Op.ZC_ITEM_PICKUP_PARTY, 24);

			// ZC_ACK_REQMAKINGITEM: rA at PACKETVER_MAIN_NUM >= 20181121
			// widens itemId to uint32. Body = 2 result + 4 itemId, wire = 8.
			// Ref: rathena packets_struct.hpp:2547.
			ChangeSize(Op.ZC_ACK_REQMAKINGITEM, 8);

			// ZC_FEED_PET: rA at PACKETVER_MAIN_NUM >= 20181121 widens
			// itemId to uint32. Body = 1 result + 4 itemId, wire = 7.
			// Ref: rathena packets_struct.hpp:2557.
			ChangeSize(Op.ZC_FEED_PET, 7);

			// ZC_PROPERTY_PET: rA at PACKETVER >= 20081126 appends a
			// 2-byte `job` field after the egg ITID. Body = 24 name +
			// 1 modified + 2 level + 2 fullness + 2 relationship +
			// 2 itid + 2 job = 35, wire = 37.
			// Ref: rathena packets_struct.hpp:5441.
			ChangeSize(Op.ZC_PROPERTY_PET, 37);

			// CZ_ITEM_PICKUP / CZ_ITEM_THROW: PV23 (2008-09-10aSakexe)
			// inserts a 2-byte filler before the payload, which is what
			// the open-midgard client sends per its committed PV23
			// client-send alignment. Pickup wire = 2 padding + 4
			// invItemHandle = 6 body, opcode + body = 8. Throw keeps
			// the rA classic shape for now (2 invId + 2 amount, wire 6).
			// Handler in PacketHandler.CZ_ITEM_PICKUP skips the padding.
			// Ref: eA db/packet_db.txt PV22 line 1009 (0x00f5,8,takeitem,4).
			ChangeSize(Op.CZ_ITEM_PICKUP, 8);
			ChangeSize(Op.CZ_ITEM_THROW, 6);

			// ZC_ADD_FRIENDS_LIST: Sabine repurposes this as the initial
			// friends-list dump. rA's 0x0201 is a variable-length packet —
			// 4 header + N × 32-byte entries (AID + CID + name[24]).
			// Single-entry post-add ack is dispatched as a one-element
			// list rather than rA's separate 0x0209 fixed packet.
			// Ref: rathena clif.cpp clif_friendslist_send.
			ChangeSize(Op.ZC_ADD_FRIENDS_LIST, Dynamic);

			// ZC_FRIENDS_STATE: rA at PACKETVER >= 20180221 appends a
			// 24-byte name field after the offline flag. Body = 4 AID +
			// 4 CID + 1 offline + 24 name = 33, wire = 35.
			// Ref: rathena packets_struct.hpp clif_friendslist_toggle.
			ChangeSize(Op.ZC_FRIENDS_STATE, 35);

			// ZC_ITEM_PICKUP_ACK: rA at PACKETVER_MAIN_NUM >= 20181121
			// (0x0a37 variant) — Index(2) + count(2) + nameid(4 u32) +
			// IsIdentified(1) + IsDamaged(1) + refiningLevel(1) +
			// slot(8) + location(4 u32) + type(1) + result(1) +
			// HireExpireDate(4) + bindOnEquipType(2) +
			// option_data[5](25) + favorite(1) + look(2) = 59 body,
			// wire = 61. (refineGrade was added at PACKETVER >= 20200724;
			// not included here.)
			// Ref: rathena packets_struct.hpp:540.
			ChangeSize(Op.ZC_ITEM_PICKUP_ACK, 61);

			// Drop placeholder UNK_ entries first; they're not part of
			// the Sabine wire surface.
			Entries.RemoveAll(static e => e.Op.ToString().StartsWith("UNK_", StringComparison.Ordinal));

			// Stable ordering by enum ordinal keeps wire IDs deterministic
			// across rebuilds so the open-midgard client can pin a fixed
			// mapping. Sequential assignment from 0x0001 means every Op
			// ordinal that survived the UNK_ filter has a registered
			// network opcode — no gaps. This is intentional: framework
			// code that occasionally treats `(int)packet.Op` as a wire
			// ID (e.g. legacy receive-path normalization) only works if
			// the ordinal-to-wire mapping is dense.
			Entries.Sort(static (a, b) => ((int)a.Op).CompareTo((int)b.Op));

			var next = 0x0001;
			foreach (var entry in Entries)
				entry.OpNetwork = next++;

			if (next > 0x10000)
				throw new InvalidOperationException("Sabine wire-ID space overflowed 0xFFFF; protocol exceeds 16-bit opcode capacity.");
		}
	}
}
