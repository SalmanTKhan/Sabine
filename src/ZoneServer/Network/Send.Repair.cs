using System.Collections.Generic;
using Sabine.Shared;
using Sabine.Shared.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Packet senders for the BS_REPAIRWEAPON exchange. EP4+ only;
	/// alpha clients have no opcodes for these and the senders should
	/// not be reached from those code paths.
	/// </summary>
	public static partial class Send
	{
		/// <summary>
		/// Sends the broken-item picker (ZC_REPAIRITEMLIST, op 0x01FC,
		/// EP4+) to the caster of BS_REPAIRWEAPON. Each row carries the
		/// inventory index of a broken item on the target so the client
		/// can offer the picker UI.
		/// </summary>
		/// <remarks>
		/// Mirrors eAthena clif.c:clif_item_repair_list. Per-entry
		/// payload: index(2) + nameid(2) + refine(1) + 4×card(2) = 13.
		/// </remarks>
		public static void ZC_REPAIRITEMLIST(PlayerCharacter character, IReadOnlyList<Item> brokenItems)
		{
			using var packet = Packet.Rent(Op.ZC_REPAIRITEMLIST);

			foreach (var item in brokenItems)
			{
				packet.PutShort((short)item.InventoryId);
				packet.PutShort((short)item.ClassId);
				packet.PutByte(item.RefineLevel);
				for (var i = 0; i < 4; ++i)
				{
					var card = (i < item.Cards.Length) ? item.Cards[i] : 0;
					packet.PutShort((short)card);
				}
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the result of a repair attempt (ZC_ACK_ITEMREPAIR,
		/// op 0x01FE, EP4+) to a player. <paramref name="flag"/> is
		/// 0 = success, 1 = failure (mirrors eA clif_item_repaireffect).
		/// </summary>
		public static void ZC_ACK_ITEMREPAIR(PlayerCharacter character, int inventoryIndex, byte flag)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_ITEMREPAIR);

			packet.PutShort((short)inventoryIndex);
			packet.PutByte(flag);

			character.Connection.Send(packet);
		}
	}
}
