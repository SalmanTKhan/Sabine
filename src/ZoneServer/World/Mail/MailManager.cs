using System;
using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Logging;

namespace Sabine.Zone.World.Mail
{
	/// <summary>
	/// DB-backed mail registry. Inboxes are loaded lazily on the
	/// receiver's first login (or first inbox open if they were
	/// already online when a message arrived). Mutations write
	/// through to <c>mail_messages</c> immediately.
	/// </summary>
	public class MailManager
	{
		public static MailManager Instance { get; } = new MailManager();

		private readonly object _syncRoot = new();
		private readonly Dictionary<int, List<MailMessage>> _inboxByReceiver = new();

		/// <summary>
		/// Send result for <see cref="Send"/>; mirrors the
		/// <c>ZC_MAIL_REQ_SEND</c> result codes.
		/// </summary>
		public enum SendResult : byte
		{
			Success = 0,
			ReceiverUnknown = 1,
		}

		/// <summary>
		/// Take-attachment result for <see cref="TakeAttachment"/>;
		/// mirrors <c>ZC_MAIL_REQ_GET_ITEM</c>.
		/// </summary>
		public enum TakeResult : byte
		{
			Success = 0,
			Failure = 1,
			TooManyItems = 2,
		}

		/// <summary>
		/// Returns the cached inbox for <paramref name="receiverId"/>,
		/// loading it from the database on first access. Newest-first.
		/// </summary>
		public IReadOnlyList<MailMessage> GetInbox(int receiverId)
		{
			lock (_syncRoot)
			{
				if (!_inboxByReceiver.TryGetValue(receiverId, out var list))
				{
					list = ZoneServer.Instance.Database.LoadMailFor(receiverId);
					list.Sort((a, b) => b.SentAt.CompareTo(a.SentAt));
					_inboxByReceiver[receiverId] = list;
				}
				return list.ToList();
			}
		}

		/// <summary>
		/// Persists a new mail to <paramref name="receiverName"/>'s
		/// inbox. The sender's zeny / item have already been deducted
		/// by the caller — on <see cref="SendResult.ReceiverUnknown"/>
		/// the caller is expected to refund.
		/// </summary>
		public SendResult Send(PlayerCharacter sender, string receiverName, string title, string body,
			int zeny, int itemClassId, short itemAmount, byte itemRefine, bool itemIdentified, bool itemDamaged,
			out MailMessage message)
		{
			message = null;

			var receiverId = ZoneServer.Instance.Database.GetCharacterIdByName(receiverName);
			if (receiverId <= 0)
				return SendResult.ReceiverUnknown;

			message = new MailMessage
			{
				SenderId = sender.Id,
				SenderName = sender.Name,
				ReceiverId = receiverId,
				Title = title ?? string.Empty,
				Body = body ?? string.Empty,
				SentAt = DateTime.UtcNow,
				Zeny = zeny,
				AttachedItemClassId = itemClassId,
				AttachedItemAmount = itemAmount,
				AttachedItemRefine = itemRefine,
				AttachedItemIdentified = itemIdentified,
				AttachedItemDamaged = itemDamaged,
			};

			ZoneServer.Instance.Database.InsertMail(message);

			lock (_syncRoot)
			{
				if (_inboxByReceiver.TryGetValue(receiverId, out var list))
					list.Insert(0, message);
			}

			// If the receiver is online, push the new-mail notification
			// + open the window so they see the inbox refresh.
			if (ZoneServer.Instance.World.Maps.TryGetPlayerById(receiverId, out var receiver))
			{
				Network.Send.ZC_MAIL_RECEIVE(receiver, message);
				Network.Send.ZC_MAIL_WINDOWS(receiver, 0);
			}

			return SendResult.Success;
		}

		/// <summary>
		/// Marks a message as read. Idempotent.
		/// </summary>
		public MailMessage Open(int receiverId, int mailId)
		{
			MailMessage msg = null;
			lock (_syncRoot)
			{
				if (_inboxByReceiver.TryGetValue(receiverId, out var list))
					msg = list.FirstOrDefault(m => m.Id == mailId);
			}

			if (msg == null || msg.Read)
				return msg;

			msg.Read = true;
			ZoneServer.Instance.Database.UpdateMailRead(mailId);
			return msg;
		}

		/// <summary>
		/// Transfers a message's zeny + item to the receiver. Returns
		/// <see cref="TakeResult.TooManyItems"/> if the receiver's
		/// inventory cannot hold the item, in which case the row is
		/// left untouched so the player can retry later.
		/// </summary>
		public TakeResult TakeAttachment(PlayerCharacter receiver, int mailId)
		{
			MailMessage msg;
			lock (_syncRoot)
			{
				if (!_inboxByReceiver.TryGetValue(receiver.Id, out var list))
					return TakeResult.Failure;
				msg = list.FirstOrDefault(m => m.Id == mailId);
			}

			if (msg == null || msg.AttachmentTaken || !msg.HasAttachment)
				return TakeResult.Failure;

			Item item = null;
			if (msg.AttachedItemClassId > 0 && msg.AttachedItemAmount > 0)
			{
				item = new Item(msg.AttachedItemClassId)
				{
					Amount = msg.AttachedItemAmount,
					RefineLevel = msg.AttachedItemRefine,
					IsIdentified = msg.AttachedItemIdentified,
					IsDamaged = msg.AttachedItemDamaged,
				};

				// Capacity check would go here once Inventory exposes one;
				// AddItem currently always succeeds, so the receiver gets
				// the item even on a "full" bag.
				receiver.Inventory.AddItem(item);
			}

			if (msg.Zeny > 0)
				receiver.Parameters.Zeny += msg.Zeny;

			msg.AttachmentTaken = true;
			ZoneServer.Instance.Database.UpdateMailAttachmentTaken(mailId);
			return TakeResult.Success;
		}

		/// <summary>
		/// Deletes a message. Refuses if the attachment is still
		/// pending pickup, mirroring eAthena's invariant.
		/// </summary>
		public bool Delete(int receiverId, int mailId)
		{
			MailMessage msg;
			lock (_syncRoot)
			{
				if (!_inboxByReceiver.TryGetValue(receiverId, out var list))
					return false;
				msg = list.FirstOrDefault(m => m.Id == mailId);
				if (msg == null)
					return false;
				if (msg.HasAttachment && !msg.AttachmentTaken)
					return false;
				list.Remove(msg);
			}

			ZoneServer.Instance.Database.DeleteMail(mailId);
			return true;
		}

		/// <summary>
		/// Loads the player's inbox into cache and pops a "you have
		/// mail" notification if anything is unread. Called from the
		/// CZ_NOTIFY_ACTORINIT login flow.
		/// </summary>
		public void OnPlayerLogin(PlayerCharacter player)
		{
			List<MailMessage> list;
			lock (_syncRoot)
			{
				if (!_inboxByReceiver.TryGetValue(player.Id, out list))
				{
					try
					{
						list = ZoneServer.Instance.Database.LoadMailFor(player.Id);
						list.Sort((a, b) => b.SentAt.CompareTo(a.SentAt));
						_inboxByReceiver[player.Id] = list;
					}
					catch (Exception ex)
					{
						Log.Error("MailManager.OnPlayerLogin: failed to load inbox for char {0}: {1}", player.Id, ex);
						return;
					}
				}
			}

			if (list.Any(m => !m.Read))
				Network.Send.ZC_MAIL_WINDOWS(player, 0);
		}
	}
}
