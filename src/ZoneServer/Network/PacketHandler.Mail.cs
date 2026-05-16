using Sabine.Shared.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Mail;

namespace Sabine.Zone.Network
{
	public partial class PacketHandler
	{
		/// <summary>
		/// Inbox refresh request (CZ_MAIL_GET_LIST). Replies with the
		/// receiver's full inbox via <see cref="Send.ZC_MAIL_REQ_GET_LIST"/>.
		/// </summary>
		[PacketHandler(Op.CZ_MAIL_GET_LIST)]
		public void CZ_MAIL_GET_LIST(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			var inbox = ZoneServer.Instance.World.Mail.GetInbox(character.Id);
			Send.ZC_MAIL_REQ_GET_LIST(character, inbox);
		}

		/// <summary>
		/// Open-mail request (CZ_MAIL_OPEN). Marks the message read
		/// and pushes the detailed view back.
		/// </summary>
		[PacketHandler(Op.CZ_MAIL_OPEN)]
		public void CZ_MAIL_OPEN(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			var mailId = packet.GetInt();

			if (mailId <= 0)
				return;

			var msg = ZoneServer.Instance.World.Mail.Open(character.Id, mailId);
			if (msg == null)
				return;

			Send.ZC_MAIL_REQ_OPEN(character, msg);
		}

		/// <summary>
		/// Delete request (CZ_MAIL_DELETE). Refused while an unclaimed
		/// attachment is still attached.
		/// </summary>
		[PacketHandler(Op.CZ_MAIL_DELETE)]
		public void CZ_MAIL_DELETE(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			var mailId = packet.GetInt();

			if (mailId <= 0)
				return;

			var ok = ZoneServer.Instance.World.Mail.Delete(character.Id, mailId);
			Send.ZC_ACK_MAIL_DELETE(character, mailId, (short)(ok ? 0 : 1));
		}

		/// <summary>
		/// Take-attachment request (CZ_MAIL_GET_ITEM). Transfers the
		/// item + zeny to the receiver's inventory.
		/// </summary>
		[PacketHandler(Op.CZ_MAIL_GET_ITEM)]
		public void CZ_MAIL_GET_ITEM(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			var mailId = packet.GetInt();

			if (mailId <= 0)
				return;

			var result = ZoneServer.Instance.World.Mail.TakeAttachment(character, mailId);
			Send.ZC_MAIL_REQ_GET_ITEM(character, (byte)result);
		}

		/// <summary>
		/// Outgoing-mail staging clear (CZ_MAIL_RESET_ITEM).
		/// type 0 = clear both, 1 = clear item, 2 = clear zeny.
		/// </summary>
		[PacketHandler(Op.CZ_MAIL_RESET_ITEM)]
		public void CZ_MAIL_RESET_ITEM(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			var type = packet.GetShort();

			if (type == 0 || type == 1)
				character.MailDraft.ClearItem();
			if (type == 0 || type == 2)
				character.MailDraft.ClearZeny();
		}

		/// <summary>
		/// Stage an item or zeny on the outgoing draft (CZ_MAIL_ADD_ITEM).
		/// Ammount &gt; 0 stages; amount == 0 is treated as a "no change".
		/// The deduction happens at send time, not here.
		/// </summary>
		[PacketHandler(Op.CZ_MAIL_ADD_ITEM)]
		public void CZ_MAIL_ADD_ITEM(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			var index = packet.GetShort();
			var amount = packet.GetInt();

			byte flag = 0;
			// index 0 means "this is a zeny attachment"; otherwise it's
			// a 1-based inventory id.
			if (index == 0)
			{
				if (amount < 0 || amount > character.Parameters.Zeny)
					flag = 1;
				else
					character.MailDraft.Zeny = amount;
			}
			else
			{
				if (!character.Inventory.TryGetItem(index, out var item) || item.Amount < amount || amount <= 0)
				{
					flag = 1;
				}
				else
				{
					character.MailDraft.InventoryIndex = index;
					character.MailDraft.ItemAmount = amount;
				}
			}

			Send.ZC_ACK_MAIL_ADD_ITEM(character, index, flag);
		}

		/// <summary>
		/// Return-to-sender request (CZ_REQ_MAIL_RETURN). Bounces the
		/// mail back to its original sender via
		/// <see cref="MailManager.ReturnToSender"/>; refused if the
		/// attachment was already taken or the mail was itself a bounce.
		/// </summary>
		[PacketHandler(Op.CZ_REQ_MAIL_RETURN)]
		public void CZ_REQ_MAIL_RETURN(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			var mailId = packet.GetInt();
			var _ = packet.GetString(24);   // sender name echoed by client; ignored — we re-resolve from DB.

			if (mailId <= 0)
			{
				Send.ZC_ACK_MAIL_RETURN(character, 0, 1);
				return;
			}

			var ok = ZoneServer.Instance.World.Mail.ReturnToSender(character, mailId);
			Send.ZC_ACK_MAIL_RETURN(character, mailId, (short)(ok ? 0 : 1));
		}

		/// <summary>
		/// Send-mail request (CZ_MAIL_SEND). Validates recipient,
		/// snapshots the staged item + zeny, persists, and notifies.
		/// </summary>
		[PacketHandler(Op.CZ_MAIL_SEND)]
		public void CZ_MAIL_SEND(ZoneConnection conn, Packet packet)
		{
			var character = conn.GetCurrentCharacter();
			var len = packet.GetShort();
			var recipient = packet.GetString(24);
			var title = packet.GetString(40);
			var bodyLen = packet.GetByte();
			var body = bodyLen > 0 ? packet.GetString(bodyLen) : string.Empty;

			if (len < 69)
				return;

			var draft = character.MailDraft;
			var zeny = draft.HasZeny ? draft.Zeny : 0;
			var itemClassId = 0;
			short itemAmount = 0;
			byte itemRefine = 0;
			var itemIdentified = true;
			var itemDamaged = false;

			Item stagedItem = null;
			if (draft.HasItem)
			{
				if (!character.Inventory.TryGetItem(draft.InventoryIndex, out stagedItem)
					|| stagedItem.Amount < draft.ItemAmount)
				{
					Send.ZC_MAIL_REQ_SEND(character, 1);
					draft.Clear();
					return;
				}

				itemClassId = stagedItem.ClassId;
				itemAmount = (short)draft.ItemAmount;
				itemRefine = stagedItem.RefineLevel;
				itemIdentified = stagedItem.IsIdentified;
				itemDamaged = stagedItem.IsDamaged;
			}

			if (zeny > 0 && character.Parameters.Zeny < zeny)
			{
				Send.ZC_MAIL_REQ_SEND(character, 1);
				draft.Clear();
				return;
			}

			// Deduct first; refund if recipient unknown.
			if (zeny > 0)
				character.Parameters.Zeny -= zeny;

			if (stagedItem != null)
			{
				if (stagedItem.Amount == draft.ItemAmount)
					character.Inventory.RemoveItem(stagedItem);
				else
					character.Inventory.DecrementItem(stagedItem, draft.ItemAmount);
			}

			var result = ZoneServer.Instance.World.Mail.Send(
				character, recipient, title, body,
				zeny, itemClassId, itemAmount, itemRefine,
				itemIdentified, itemDamaged, out _);

			if (result == MailManager.SendResult.ReceiverUnknown)
			{
				if (zeny > 0)
					character.Parameters.Zeny += zeny;
				if (stagedItem != null && itemAmount > 0)
				{
					var refund = new Item(itemClassId)
					{
						Amount = itemAmount,
						RefineLevel = itemRefine,
						IsIdentified = itemIdentified,
						IsDamaged = itemDamaged,
					};
					character.Inventory.AddItem(refund);
				}
				Send.ZC_MAIL_REQ_SEND(character, 1);
			}
			else
			{
				Send.ZC_MAIL_REQ_SEND(character, 0);
			}

			draft.Clear();
		}
	}
}
