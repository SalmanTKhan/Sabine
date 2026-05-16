using Sabine.Shared;
using Sabine.Shared.Network;
using Xunit;

namespace Tests.Sabine.Shared.Network
{
	/// <summary>
	/// Smoke tests for the Sabine-native protocol profile. Pins the
	/// grouped wire-ID layout (0x00xx auth / 0x01xx char / 0x02xx CZ /
	/// 0x04xx ZC) so the open-midgard custom client has a stable
	/// contract to implement against.
	/// </summary>
	[Collection("PacketTable")]
	public class PacketTableSabineTests
	{
		[Fact]
		public void Auth_ops_land_in_0x00xx_block()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			AssertInBlock(Op.CA_LOGIN, 0x0000, 0x00FF);
			AssertInBlock(Op.AC_ACCEPT_LOGIN, 0x0000, 0x00FF);
			AssertInBlock(Op.AC_REFUSE_LOGIN, 0x0000, 0x00FF);
		}

		[Fact]
		public void Char_ops_land_in_0x01xx_block()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			AssertInBlock(Op.CH_ENTER, 0x0100, 0x01FF);
			AssertInBlock(Op.HC_ACCEPT_ENTER, 0x0100, 0x01FF);
			AssertInBlock(Op.CH_SELECT_CHAR, 0x0100, 0x01FF);
			AssertInBlock(Op.HC_NOTIFY_ZONESVR, 0x0100, 0x01FF);
		}

		[Fact]
		public void Cz_ops_land_in_0x02xx_block()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			AssertInBlock(Op.CZ_ENTER, 0x0200, 0x03FF);
			AssertInBlock(Op.CZ_REQUEST_MOVE, 0x0200, 0x03FF);
			AssertInBlock(Op.CZ_USE_SKILL, 0x0200, 0x03FF);
			AssertInBlock(Op.CZ_USE_ITEM, 0x0200, 0x03FF);
		}

		[Fact]
		public void Zc_ops_land_in_0x04xx_or_higher_block()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			AssertInBlock(Op.ZC_ACCEPT_ENTER, 0x0400, 0x0FFF);
			AssertInBlock(Op.ZC_NOTIFY_STANDENTRY, 0x0400, 0x0FFF);
			AssertInBlock(Op.ZC_NOTIFY_NEWENTRY, 0x0400, 0x0FFF);
			AssertInBlock(Op.ZC_NOTIFY_MOVEENTRY, 0x0400, 0x0FFF);
			AssertInBlock(Op.ZC_NORMAL_ITEMLIST, 0x0400, 0x0FFF);
		}

		[Fact]
		public void Auth_and_char_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// rA-latest body shapes — see PacketTable.Sabine.cs and
			// rathena/src/common/packets.hpp for citations.
			Assert.Equal(26, PacketTable.GetSize(PacketTable.ToNetwork(Op.AC_REFUSE_LOGIN)));
			Assert.Equal(156, PacketTable.GetSize(PacketTable.ToNetwork(Op.HC_NOTIFY_ZONESVR)));
			Assert.Equal(36, PacketTable.GetSize(PacketTable.ToNetwork(Op.CH_MAKE_CHAR)));
			Assert.Equal(56, PacketTable.GetSize(PacketTable.ToNetwork(Op.CH_DELETE_CHAR)));

			// Fixed-size auth/char ops inherited unchanged from the
			// historical tables; rA-latest matches these exactly.
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.HC_REFUSE_ENTER)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.HC_REFUSE_MAKECHAR)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.HC_ACCEPT_DELETECHAR)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.HC_REFUSE_DELETECHAR)));

			// Variable-length lists keep their dynamic flag.
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.AC_ACCEPT_LOGIN)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.HC_ACCEPT_ENTER)));
		}

		[Fact]
		public void CH_DELETE_CHAR2_is_collapsed_under_sabine()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// Sabine ships one delete op — the historical EP4-era
			// CH_DELETE_CHAR2 shadow is unregistered in LoadVersionSabine.
			Assert.True(PacketTable.IsRegistered(Op.CH_DELETE_CHAR));
			Assert.False(PacketTable.IsRegistered(Op.CH_DELETE_CHAR2));
		}

		[Fact]
		public void Final_v6_wrap_up_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// Single-item-add packets upgraded to rA V6 shape with
			// uint32 itemId + option_data[5] array.
			Assert.Equal(49, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ADD_ITEM_TO_CART)));
			Assert.Equal(49, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ADD_ITEM_TO_STORE)));
			Assert.Equal(53, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ADD_EXCHANGE_ITEM)));

			// ZC_USESKILL_ACK gains the attackMT u32 tail at PV>=20181212.
			Assert.Equal(29, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_USESKILL_ACK)));
		}

		[Fact]
		public void Status_party_warp_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// ZC_STATE_CHANGE: effectState widened to int32 at PV>=7.
			Assert.Equal(15, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_STATE_CHANGE)));

			// ZC_NOTIFY_HP_TO_GROUPM: hp/maxhp widened to int32 at PV>=20100119.
			Assert.Equal(14, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_HP_TO_GROUPM)));

			// ZC_WARPLIST: dynamic with packetLength header at PV>=20170502.
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_WARPLIST)));
		}

		[Fact]
		public void Final_audit_pins()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// Audit-OK pins for ops that already match rA-latest by
			// virtue of inheriting earlier-era shapes. These guard
			// against regressions if a future table edit mis-sizes them.
			Assert.Equal(33, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_SKILL2)));   // S2500 V2, int32 damage
			Assert.Equal(35, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_SKILL_POSITION)));
			Assert.Equal(39, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ADD_SKILL)));
			Assert.Equal(10, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_EFFECT)));
			Assert.Equal(10, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_EFFECT2)));

			// Skill/blacksmith/effect ops verified against rA-latest.
			Assert.Equal(39, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_AUTORUN_SKILL)));      // rA 0x0147
			Assert.Equal(8, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_RESURRECTION)));        // rA 0x0148
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_DISPEL)));              // rA 0x01b9
			Assert.Equal(11, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NPCSPRITE_CHANGE)));   // rA 0x01b0
			Assert.Equal(5, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ACK_ITEMIDENTIFY)));    // rA 0x0179
			Assert.Equal(5, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ACK_ITEMREPAIR)));      // rA 0x01fe
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_REPAIRITEMLIST)));  // rA 0x01fc
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_BROADCAST2)));      // rA 0x01c3
		}

		[Fact]
		public void Equipment_ack_and_item_entry_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// rA widens equip ACK location to uint32 and adds spriteNum.
			Assert.Equal(11, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_REQ_WEAR_EQUIP_ACK)));
			Assert.Equal(9, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_REQ_TAKEOFF_EQUIP_ACK)));

			// Item-entry packets widen itemId to uint32; FALL_ENTRY adds
			// type + dropeffect tail.
			Assert.Equal(19, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ITEM_ENTRY)));
			Assert.Equal(24, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ITEM_FALL_ENTRY)));
			Assert.Equal(15, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_USE_ITEM_ACK)));
		}

		[Fact]
		public void Movement_op_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// Drop S2500's PV23 obfuscation padding back to the rA-clean
			// shapes. Handlers branch on Versions.Sabine to read the
			// shorter bodies; see ZoneServer/Network/PacketHandler.cs.
			Assert.Equal(5, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQUEST_MOVE)));
			Assert.Equal(7, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQUEST_ACT)));
			Assert.Equal(5, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_CHANGE_DIRECTION)));

			// rA PV>=20181121 widens ZC_SPRITE_CHANGE to int32 val + val2.
			Assert.Equal(15, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_SPRITE_CHANGE)));
		}

		[Fact]
		public void Every_registered_op_has_a_valid_sabine_wire_size()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// Final sweep: every entry that survived into the Sabine
			// profile after the LoadVersionSabine renumber pass resolves
			// to a valid size. Catches regressions where a future
			// ChangeSize / Unregister leaves an entry in a broken state.
			foreach (var entry in PacketTable.GetTable())
			{
				Assert.True(entry.Size == PacketTable.Dynamic || entry.Size >= 2,
					$"{entry.Op} resolves to invalid size {entry.Size}");
				Assert.True(entry.OpNetwork >= 0x0001 && entry.OpNetwork < 0x10000,
					$"{entry.Op} resolves to invalid wire id 0x{entry.OpNetwork:X4}");
			}

			// And the deliberate-unregister set holds.
			Assert.False(PacketTable.IsRegistered(Op.CH_DELETE_CHAR2),
				"CH_DELETE_CHAR2 should be collapsed into CH_DELETE_CHAR under Sabine");
		}

		[Fact]
		public void Npc_quest_and_cashshop_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// NPC dialog flow — rA classic shapes inherited unchanged.
			Assert.Equal(7, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_CONTACTNPC)));
			Assert.Equal(7, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_CHOOSE_MENU)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQ_NEXT_SCRIPT)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_SAY_DIALOG)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_WAIT_DIALOG)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_CLOSE_DIALOG)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_MENU_LIST)));

			// Name lookups — CZ_REQNAME widens to 11 at S2000 (kRO Sak)
			// for the gid+entity-type pair; Sabine inherits that shape.
			Assert.Equal(11, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQNAME)));
			Assert.Equal(30, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ACK_REQNAME)));

			// Quest mission UI — S2500 EP12 quest pack.
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ALL_QUEST_LIST)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ALL_QUEST_MISSION)));
			Assert.Equal(107, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ADD_QUEST)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_DEL_QUEST)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_UPDATE_MISSION_HUNT)));

			// Cash shop (CashShop feature on the Sabine profile).
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_PC_CASH_POINT_ITEMLIST)));
			Assert.Equal(10, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_PC_BUY_CASH_POINT_ITEM)));
			Assert.Equal(12, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_PC_CASH_POINT_UPDATE)));
		}

		[Fact]
		public void Party_and_chat_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// Party ops — Sabine sticks with the classic 26-byte
			// CZ_MAKE_GROUP (party-name only). rA's modern CZ_MAKE_GROUP2
			// (0x01E8, 28 bytes with item-share flags) is a separate op
			// retained for older eAthena callers; Sabine keeps it as
			// Sabine-only and routes via the classic shape.
			Assert.Equal(26, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_MAKE_GROUP)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ACK_MAKE_GROUP)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQ_LEAVE_GROUP)));

			// Chat — variable length on both sides.
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQUEST_CHAT)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_CHAT)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_PLAYERCHAT)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_WHISPER)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_WHISPER)));
			// ZC_ACK_WHISPER: alpha-era 3-byte single-result body; rA
			// at PV>=20131223 widens to 7 (adds 4-byte CID) but Sabine
			// keeps the classic shape since open-midgard's whisper UI
			// doesn't read the CID echo.
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ACK_WHISPER)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_BROADCAST)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_BROADCAST)));

			// Chat rooms — variable-length lists.
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_CREATE_CHATROOM)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ACK_CREATE_CHATROOM)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ROOM_NEWENTRY)));
			Assert.Equal(14, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQ_ENTER_ROOM)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_REFUSE_ENTER_ROOM)));
		}

		[Fact]
		public void Trade_and_vending_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// Trade flow — rA classic ops, sizes stable across PV history.
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQ_EXCHANGE_ITEM)));
			Assert.Equal(26, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_REQ_EXCHANGE_ITEM)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_ACK_EXCHANGE_ITEM)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ACK_EXCHANGE_ITEM)));
			Assert.Equal(8, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_ADD_EXCHANGE_ITEM)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_CONCLUDE_EXCHANGE_ITEM)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_CONCLUDE_EXCHANGE_ITEM)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_CANCEL_EXCHANGE_ITEM)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_CANCEL_EXCHANGE_ITEM)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_EXEC_EXCHANGE_ITEM)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_EXEC_EXCHANGE_ITEM)));

			// NPC shop / vending — variable-length lists driven by Item helpers.
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_PC_PURCHASE_ITEMLIST)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_PC_SELL_ITEMLIST)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_PC_PURCHASE_ITEMLIST)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_PC_SELL_ITEMLIST)));

			// Result + close-store ops are uniform across PV.
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_PC_PURCHASE_RESULT)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_PC_SELL_RESULT)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_SELECT_DEALTYPE)));
			Assert.Equal(7, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_ACK_SELECT_DEALTYPE)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_CLOSE_STORE)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_CLOSE_STORE)));
		}

		[Fact]
		public void Inventory_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// ZC_ITEM_PICKUP_ACK upgraded to V6 shape (favorite + look + options).
			// Body 59 + opcode 2 = wire 61. Matches rA at PACKETVER >= 20181121.
			Assert.Equal(61, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ITEM_PICKUP_ACK)));

			// Variable-length lists: per-entry shape is V6 (26 / 59 bytes)
			// via PacketItemExtensions.AddNormalItemV6 / AddEquipItemV6.
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NORMAL_ITEMLIST)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_EQUIPMENT_ITEMLIST)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_STORE_NORMAL_ITEMLIST)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_STORE_EQUIPMENT_ITEMLIST)));

			// Fixed-size adjacent ops inherited from earlier tables.
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_ITEM_PICKUP)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_ITEM_THROW)));
			Assert.Equal(8, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_USE_ITEM)));
			// ZC_USE_ITEM_ACK upgraded to V6 shape — assertion lives in
			// Equipment_ack_and_item_entry_sizes_match_ra_latest.
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ITEM_DISAPPEAR)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ITEM_THROW_ACK)));
			Assert.Equal(4, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_EQUIP_ARROW)));
		}

		[Fact]
		public void Skill_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// CZ_USE_SKILL / CZ_USE_SKILL_TOGROUND drop the kRO Sak EP12
			// obfuscation padding back to rA's clean 10-byte shape.
			Assert.Equal(10, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_USE_SKILL)));
			Assert.Equal(10, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_USE_SKILL_TOGROUND)));

			// Inherited from earlier tables and already aligned with rA-latest.
			Assert.Equal(31, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_SKILL)));
			Assert.Equal(18, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_GROUNDSKILL)));
			Assert.Equal(15, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_USE_SKILL)));
			// ZC_USESKILL_ACK upgraded to V6 — pin lives in
			// Final_v6_wrap_up_sizes_match_ra_latest.
			Assert.Equal(11, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_SKILLINFO_UPDATE)));
			Assert.Equal(10, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ACK_TOUSESKILL)));
			Assert.Equal(4, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_UPGRADE_SKILLLEVEL)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ACK_REMEMBER_WARPPOINT)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_SKILL_DISAPPEAR)));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_SKILLINFO_LIST)));
		}

		[Fact]
		public void Combat_and_status_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// rA-latest body shapes — see PacketTable.Sabine.cs for citations.
			Assert.Equal(34, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_ACT)));
			Assert.Equal(8, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_RECOVERY)));

			// Inherited from S2500 / earlier and already aligned with rA-latest.
			Assert.Equal(44, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_STATUS)));
			Assert.Equal(8, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_PAR_CHANGE)));
			Assert.Equal(8, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_LONGPAR_CHANGE)));
			Assert.Equal(7, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_EMOTION)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQ_EMOTION)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_USER_COUNT)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQ_USER_COUNT)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQ_STATUS)));
			Assert.Equal(5, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_STATUS_CHANGE)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_STATUS_CHANGE_ACK)));
		}

		[Fact]
		public void Actor_entry_triad_matches_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// rA packet_idle_unit / packet_spawn_unit / packet_unit_walking
			// at PACKETVER_MAIN_NUM >= 20181121. See ZoneServer/Network/
			// Helpers/Character.cs AddStandEntrySabine and PacketTable.Sabine.cs
			// ChangeSize calls for citations.
			Assert.Equal(108, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_STANDENTRY)));
			Assert.Equal(108, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_STANDENTRY_NPC)));
			Assert.Equal(107, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_NEWENTRY)));
			Assert.Equal(114, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_MOVEENTRY)));
		}

		[Fact]
		public void Zone_entry_sizes_match_ra_latest()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// rA-latest zone-entry/map-change body shapes — see
			// PacketTable.Sabine.cs ChangeSize calls for citations.
			Assert.Equal(14, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_ACCEPT_ENTER)));
			Assert.Equal(156, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NPCACK_SERVERMOVE)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_REQUEST_TIME)));

			// Inherited from S2500 / earlier tables; already match rA-latest.
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_REFUSE_ENTER)));
			Assert.Equal(22, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NPCACK_MAPMOVE)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_TIME)));
			Assert.Equal(6, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_AID)));
			Assert.Equal(2, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_NOTIFY_ACTORINIT)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.CZ_RESTART)));
			Assert.Equal(3, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_RESTART_ACK)));
		}

		[Fact]
		public void Unk_placeholders_are_dropped()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			// The historical-client UNK_ rows reserve wire slots Sabine
			// will never ship. They should be unregistered so the
			// Sabine wire surface contains only real, named opcodes.
			Assert.False(PacketTable.IsRegistered(Op.UNK_0300));
			Assert.False(PacketTable.IsRegistered(Op.UNK_03FF));
			Assert.False(PacketTable.IsRegistered(Op.UNK_0435));
		}

		[Fact]
		public void Round_trip_resolves_for_named_ops()
		{
			using var _ = new PacketTableScope(Versions.Sabine);

			var wire = PacketTable.ToNetwork(Op.CZ_ENTER);
			Assert.Equal(Op.CZ_ENTER, PacketTable.ToHost(wire));
		}

		private static void AssertInBlock(Op op, int lo, int hi)
		{
			var wire = PacketTable.ToNetwork(op);
			Assert.True(
				wire >= lo && wire <= hi,
				$"{op} wire 0x{wire:X4} should be in 0x{lo:X4}-0x{hi:X4}");
		}

		/// <summary>
		/// Switches Game.Version + reloads the static PacketTable for
		/// one test, then restores it on dispose so other tests see
		/// the prior table state.
		/// </summary>
		private sealed class PacketTableScope : System.IDisposable
		{
			private readonly int _previousVersion;

			public PacketTableScope(int version)
			{
				_previousVersion = Game.Version;
				Game.Version = version;
				PacketTable.Load();
			}

			public void Dispose()
			{
				Game.Version = _previousVersion;
				PacketTable.Load();
			}
		}
	}
}
