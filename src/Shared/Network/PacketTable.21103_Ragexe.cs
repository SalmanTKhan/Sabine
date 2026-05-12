namespace Sabine.Shared.Network
{
	public static partial class PacketTable
	{
		/// <summary>
		/// PACKETVER 20211103 (rAthena's modern default).
		/// Stacks on top of <see cref="LoadVersion2500"/> and applies
		/// the post-2018 CZ-opcode block from rAthena
		/// <c>src/map/clif_shuffle.hpp:4723-4761</c>, plus the
		/// <c>WantToConnection</c> size carve-out for PACKETVER_RE
		/// &gt;= 20211103.
		/// </summary>
		/// <remarks>
		/// Scope: only CZ entries that have a matching <see cref="Op"/>
		/// enum today. Modern-only handlers (buying store, search
		/// store, party booking v2, RODEX, cash shop) need new Op
		/// enum values + handlers and land alongside their feature
		/// slices in Phase 3.
		///
		/// ZC packets are unchanged from the legacy stack here.
		/// Phase 1 will redefine the ones a real 20211103 client
		/// expects (NEW_ENTRY11, INVENTORY_START/END, NORMAL_ITEMLIST3,
		/// NOTIFY_ACT3, etc.) once the corresponding struct writers
		/// land.
		/// </remarks>
		private static void LoadVersion21103()
		{
			// Reference: rAthena clif_shuffle.hpp post-2018 block.
			// Each Redefine maps a Sabine host Op to the wire
			// opcode + length the 20211103 client uses.

			// Retire Sak-era placeholders and the obsolete
			// CZ_REQUEST_MOVE2 alias that occupy 0x035F-0x0368.
			// Without this, the placeholder entries would still
			// be in the lookup dictionary and shadow the real
			// ops we redefine onto the same wire opcodes.
			Unregister(Op.CZ_REQUEST_MOVE2);
			Unregister(Op.UNK_0360);
			Unregister(Op.UNK_0361);
			Unregister(Op.UNK_0362);
			Unregister(Op.UNK_0363);
			Unregister(Op.UNK_0364);
			Unregister(Op.UNK_0365);
			Unregister(Op.UNK_0366);
			Unregister(Op.UNK_0367);
			Unregister(Op.UNK_0368);

			// Movement / camera / time
			Redefine(Op.CZ_REQUEST_MOVE, 0x035F, 5);
			Redefine(Op.CZ_REQUEST_TIME, 0x0360, 6);
			Redefine(Op.CZ_CHANGE_DIRECTION, 0x0361, 5);

			// Item interaction
			Redefine(Op.CZ_ITEM_PICKUP, 0x0362, 6);
			Redefine(Op.CZ_ITEM_THROW, 0x0363, 6);

			// Storage (Kafra)
			Redefine(Op.CZ_MOVE_ITEM_FROM_BODY_TO_STORE, 0x0364, 8);
			Redefine(Op.CZ_MOVE_ITEM_FROM_STORE_TO_BODY, 0x0365, 8);

			// Skills
			Redefine(Op.CZ_USE_SKILL_TOGROUND, 0x0366, 10);
			// 0x0367 UseSkillToPosMoreInfo — RE 20190904+ uses 31, prior 90.
			// 20211103 is post-2019-09-04 so the 31-byte form applies.
			Redefine(Op.CZ_USE_SKILL_TOGROUND_WITHTALKBOX, 0x0367, 31);

			// Name lookups. rAthena's GetCharNameRequest (0x0368)
			// reads a 4-byte char/account ID — Sabine's
			// CZ_REQNAME_BYGID is the GID-keyed variant.
			Redefine(Op.CZ_REQNAME_BYGID, 0x0368, 6);

			// CZ_ENTER (WantToConnection): RE 20211103+ added an
			// extra 4-byte field, so length is 23 instead of 19.
			Redefine(Op.CZ_ENTER, 0x0436, 23);

			Redefine(Op.CZ_REQUEST_ACT, 0x0437, 7);
			Redefine(Op.CZ_USE_SKILL, 0x0438, 10);

			// Friends
			Redefine(Op.CZ_ADD_FRIENDS, 0x0202, 26);

			// Homunculus menu
			Redefine(Op.CZ_COMMAND_MER, 0x022D, 5);

			// Not yet ported (need Op enum + handler):
			//   0x0369 SolveCharName        (CZ_REQNAME variant)
			//   0x023B StoragePassword
			//   0x02C4 PartyInvite2
			//   0x0802 PartyBookingRegisterReq
			//   0x07E4 ItemListWindowSelected
			//   0x0811 ReqOpenBuyingStore
			//   0x0815 ReqCloseBuyingStore
			//   0x0817 ReqClickBuyingStore
			//   0x0819 ReqTradeBuyingStore
			//   0x0835 SearchStoreInfo
			//   0x0838 SearchStoreInfoNextPage
			//   0x083C SearchStoreInfoListItemClick
		}
	}
}
