namespace Sabine.Shared.Network
{
	public static partial class PacketTable
	{
		private static void LoadVersionNostalRo20120307()
		{
			Redefine(Op.CH_MAKE_CHAR, 0x0970, 31);
			Register(Op.CH_DELETE_CHAR3_RESERVED, 0x0827, 6, false);
			Register(Op.HC_DELETE_CHAR3_RESERVED, 0x0828, 6, false);
			Register(Op.CH_DELETE_CHAR3, 0x0829, 12, false);
			Register(Op.HC_DELETE_CHAR3, 0x082A, 6, false);
			Register(Op.CH_DELETE_CHAR3_CANCEL, 0x082B, 6, false);
			Register(Op.HC_DELETE_CHAR3_CANCEL, 0x082C, 6, false);
			Redefine(Op.CZ_ENTER, 0x086A, 19);
			Redefine(Op.CZ_REQUEST_MOVE, 0x0437, 5);
			Redefine(Op.CZ_REQUEST_TIME, 0x0887, 6);
			Redefine(Op.CZ_CHANGE_DIRECTION, 0x0890, 5);
			Redefine(Op.CZ_ITEM_PICKUP, 0x0865, 6);
			Redefine(Op.CZ_ITEM_THROW, 0x02C4, 6);
			Redefine(Op.CZ_MOVE_ITEM_FROM_BODY_TO_STORE, 0x093B, 8);
			Redefine(Op.CZ_MOVE_ITEM_FROM_STORE_TO_BODY, 0x0963, 8);
			Redefine(Op.CZ_REQNAME, 0x096A, 6);
			Register(Op.CZ_REQNAME_BYGID, 0x0368, 6, false);
			Redefine(Op.CZ_REQUEST_ACT, 0x0885, 7);
			Redefine(Op.CZ_USE_SKILL, 0x0889, 10);
			Redefine(Op.CZ_USE_SKILL_TOGROUND, 0x0438, 10);
			Redefine(Op.CZ_USE_SKILL_TOGROUND_WITHTALKBOX, 0x0366, 90);
			Redefine(Op.CZ_USE_ITEM, 0x0439, 8);
			Redefine(Op.CZ_CLOSE_STORE, 0x0193, 2);
			ChangeSize(Op.CH_MAKE_CHAR, 31);
			Redefine(Op.ZC_ACCEPT_ENTER, 0x0073, 11);
			Redefine(Op.CZ_NOTIFY_ACTORINIT, 0x007D, 2);
			Redefine(Op.ZC_AID, 0x0283, 6);
			Redefine(Op.ZC_NOTIFY_STANDENTRY, 0x0857, 65);
			Redefine(Op.ZC_NOTIFY_NEWENTRY, 0x0858, 64);
			Redefine(Op.ZC_NOTIFY_MOVEENTRY, 0x0856, 71);
		}
	}
}
