using System;
using System.Collections.Generic;
using Sabine.Shared.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Mail;

namespace Sabine.Zone.Network
{
	public static partial class Send
	{
		/// <summary>
		/// Inbox listing. Mirrors eAthena's <c>clif_Mail_refreshinbox</c>
		/// (op 0x0240). Per-entry layout: id.L + title.40B + read.B +
		/// sender.24B + time.L = 73 bytes.
		/// </summary>
		public static void ZC_MAIL_REQ_GET_LIST(PlayerCharacter character, IReadOnlyList<MailMessage> inbox)
		{
			using var packet = Packet.Rent(Op.ZC_MAIL_REQ_GET_LIST);

			packet.PutInt(inbox.Count);
			foreach (var m in inbox)
			{
				packet.PutInt(m.Id);
				packet.PutString(m.Title, 40);
				packet.PutByte((byte)(m.Read ? 1 : 0));
				packet.PutString(m.SenderName, 24);
				packet.PutInt((int)ToUnixSeconds(m.SentAt));
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Detailed view of a single message. Mirrors eAthena's
		/// <c>clif_Mail_read</c> (op 0x0242).
		/// </summary>
		public static void ZC_MAIL_REQ_OPEN(PlayerCharacter character, MailMessage msg)
		{
			using var packet = Packet.Rent(Op.ZC_MAIL_REQ_OPEN);

			packet.PutInt(msg.Id);
			packet.PutString(msg.Title, 40);
			packet.PutString(msg.SenderName, 24);
			packet.PutInt((int)ToUnixSeconds(msg.SentAt));
			// Treat picked-up attachments as gone for display purposes;
			// the client wouldn't be able to take them again anyway.
			var hasItem = !msg.AttachmentTaken && msg.AttachedItemClassId > 0 && msg.AttachedItemAmount > 0;
			var zeny = msg.AttachmentTaken ? 0 : msg.Zeny;
			packet.PutInt(zeny);
			packet.PutInt(hasItem ? msg.AttachedItemAmount : 0);
			packet.PutShort((short)(hasItem ? msg.AttachedItemClassId : 0));
			packet.PutShort(0); // item type (looked up by client; 0 keeps it generic)
			packet.PutByte((byte)(hasItem && msg.AttachedItemIdentified ? 1 : 0));
			packet.PutByte((byte)(hasItem && msg.AttachedItemDamaged ? 1 : 0));
			packet.PutByte(hasItem ? msg.AttachedItemRefine : (byte)0);
			packet.PutShort(0); // card1
			packet.PutShort(0); // card2
			packet.PutShort(0); // card3
			packet.PutShort(0); // card4

			var body = msg.Body ?? string.Empty;
			var bodyLen = (byte)Math.Min(body.Length + 1, 200);
			packet.PutByte(bodyLen);
			packet.PutString(body, bodyLen);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Acks a CZ_MAIL_ADD_ITEM staging request.
		/// <paramref name="flag"/>: 0 = success, anything else = failure.
		/// </summary>
		public static void ZC_ACK_MAIL_ADD_ITEM(PlayerCharacter character, int inventoryIndex, byte flag)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_MAIL_ADD_ITEM);

			packet.PutShort((short)inventoryIndex);
			packet.PutByte(flag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Acks a CZ_MAIL_GET_ITEM (take-attachment) request.
		/// 0 = success, 1 = failure, 2 = inventory full.
		/// </summary>
		public static void ZC_MAIL_REQ_GET_ITEM(PlayerCharacter character, byte flag)
		{
			using var packet = Packet.Rent(Op.ZC_MAIL_REQ_GET_ITEM);

			packet.PutByte(flag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Acks a CZ_MAIL_SEND request.
		/// 0 = success, 1 = recipient unknown.
		/// </summary>
		public static void ZC_MAIL_REQ_SEND(PlayerCharacter character, byte flag)
		{
			using var packet = Packet.Rent(Op.ZC_MAIL_REQ_SEND);

			packet.PutByte(flag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Acks a CZ_MAIL_DELETE request.
		/// 0 = success, 1 = failure.
		/// </summary>
		public static void ZC_ACK_MAIL_DELETE(PlayerCharacter character, int mailId, short flag)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_MAIL_DELETE);

			packet.PutInt(mailId);
			packet.PutShort(flag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// New-mail notification. Pops the "you have mail" toast and
		/// previews the title + sender.
		/// </summary>
		public static void ZC_MAIL_RECEIVE(PlayerCharacter character, MailMessage msg)
		{
			using var packet = Packet.Rent(Op.ZC_MAIL_RECEIVE);

			packet.PutInt(msg.Id);
			packet.PutString(msg.Title, 40);
			packet.PutString(msg.SenderName, 24);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens (<paramref name="flag"/> = 0) or closes (1) the mail
		/// window on the client.
		/// </summary>
		public static void ZC_MAIL_WINDOWS(PlayerCharacter character, int flag)
		{
			using var packet = Packet.Rent(Op.ZC_MAIL_WINDOWS);

			packet.PutInt(flag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Acks a CZ_REQ_MAIL_RETURN (return-to-sender) request.
		/// fail = 0 = success, 1 = failure. Mirrors eAthena's
		/// <c>clif_Mail_return</c> (op 0x0274, 8 bytes).
		/// </summary>
		public static void ZC_ACK_MAIL_RETURN(PlayerCharacter character, int mailId, short fail)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_MAIL_RETURN);

			packet.PutInt(mailId);
			packet.PutShort(fail);

			character.Connection.Send(packet);
		}

		private static long ToUnixSeconds(DateTime utc)
			=> new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeSeconds();
	}
}
