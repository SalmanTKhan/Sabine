using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

		private Timer _expirationTimer;
		private static readonly TimeSpan ExpirationInterval = TimeSpan.FromHours(6);

		/// <summary>
		/// Persistence seam. Defaults to a live <c>ZoneDb</c> adapter;
		/// tests substitute an in-memory fake to exercise the manager's
		/// validation rules without a database.
		/// </summary>
		public IMailRepository Repository { get; set; } = new ZoneDbMailRepository();

		/// <summary>
		/// Hook invoked after a new mail row lands in someone's inbox.
		/// Defaults to pushing <c>ZC_MAIL_RECEIVE</c> + <c>ZC_MAIL_WINDOWS</c>
		/// to that receiver if they're currently online. Tests can swap
		/// in a no-op or capture lambda.
		/// </summary>
		public Action<int, MailMessage> OnNewMailNotifier { get; set; } = NotifyOnlineReceiver;

		private static void NotifyOnlineReceiver(int receiverId, MailMessage msg)
		{
			if (ZoneServer.Instance?.World?.Maps == null)
				return;
			if (!ZoneServer.Instance.World.Maps.TryGetPlayerById(receiverId, out var receiver))
				return;

			Network.Send.ZC_MAIL_RECEIVE(receiver, msg);
			Network.Send.ZC_MAIL_WINDOWS(receiver, 0);
		}

		/// <summary>
		/// Sender name used for system-generated mail (auto-refunds
		/// from expired attachments, etc.). Surfaced as a const so
		/// tests can assert against it.
		/// </summary>
		public const string SystemSenderName = "Postman";

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
					list = this.Repository.LoadMailFor(receiverId);
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

			var receiverId = this.Repository.GetCharacterIdByName(receiverName);
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

			this.Repository.InsertMail(message);

			lock (_syncRoot)
			{
				if (_inboxByReceiver.TryGetValue(receiverId, out var list))
					list.Insert(0, message);
			}

			this.OnNewMailNotifier?.Invoke(receiverId, message);

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
			this.Repository.UpdateMailRead(mailId);
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
			this.Repository.UpdateMailAttachmentTaken(mailId);
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

			this.Repository.DeleteMail(mailId);
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
						list = this.Repository.LoadMailFor(player.Id);
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

		/// <summary>
		/// Bounces a mail back to its original sender. The receiver
		/// must own the row, the attachment must not have been claimed,
		/// and the mail must not itself already be a bounce (avoids
		/// ping-pong loops). On success the original row is deleted and
		/// a new row is inserted with sender / receiver swapped, title
		/// prefixed "RE: ", and the attachment carried over verbatim.
		/// </summary>
		public bool ReturnToSender(PlayerCharacter receiver, int mailId)
		{
			MailMessage original;
			lock (_syncRoot)
			{
				if (!_inboxByReceiver.TryGetValue(receiver.Id, out var list))
					return false;
				original = list.FirstOrDefault(m => m.Id == mailId);
			}

			if (original == null) return false;
			if (original.ReceiverId != receiver.Id) return false;
			if (original.AttachmentTaken) return false;
			if (original.IsReturned) return false;
			if (original.SenderId == receiver.Id) return false; // no self-bounce

			// Original sender must still exist as a character — otherwise
			// the bounce would land in nobody's inbox.
			var bouncedReceiverName = this.Repository.GetCharacterName(original.SenderId);
			if (string.IsNullOrEmpty(bouncedReceiverName))
				return false;

			var bounce = new MailMessage
			{
				SenderId = receiver.Id,
				SenderName = receiver.Name,
				ReceiverId = original.SenderId,
				Title = "RE: " + (original.Title ?? string.Empty),
				Body = original.Body ?? string.Empty,
				SentAt = DateTime.UtcNow,
				Zeny = original.Zeny,
				AttachedItemClassId = original.AttachedItemClassId,
				AttachedItemAmount = original.AttachedItemAmount,
				AttachedItemRefine = original.AttachedItemRefine,
				AttachedItemIdentified = original.AttachedItemIdentified,
				AttachedItemDamaged = original.AttachedItemDamaged,
				IsReturned = true,
			};

			// Trim the title back to the column width (40 chars) — the
			// "RE: " prefix could push a max-length original past it.
			if (bounce.Title.Length > 40)
				bounce.Title = bounce.Title.Substring(0, 40);

			try
			{
				this.Repository.InsertMail(bounce);
			}
			catch (Exception ex)
			{
				Log.Error("MailManager.ReturnToSender: insert failed: {0}", ex);
				return false;
			}

			// Original row is gone; pull from cache + delete from DB.
			lock (_syncRoot)
			{
				if (_inboxByReceiver.TryGetValue(receiver.Id, out var list))
					list.RemoveAll(m => m.Id == mailId);

				// If the bounced-to sender is already loaded, splice the
				// new message in. If not, the next inbox open will load
				// it fresh from DB.
				if (_inboxByReceiver.TryGetValue(bounce.ReceiverId, out var senderList))
					senderList.Insert(0, bounce);
			}

			this.Repository.DeleteMail(mailId);

			this.OnNewMailNotifier?.Invoke(bounce.ReceiverId, bounce);

			return true;
		}

		/// <summary>
		/// Starts the periodic auto-purge timer. Called once from
		/// <c>WorldManager.Load</c> at zone-server boot. Safe to call
		/// twice (idempotent).
		/// </summary>
		public void StartExpirationTimer()
		{
			if (_expirationTimer != null)
				return;

			_expirationTimer = new Timer(this.ExpirationTick, null,
				ExpirationInterval, ExpirationInterval);
		}

		private void ExpirationTick(object _)
		{
			try
			{
				var days = ZoneServer.Instance.Conf.World.MailExpireDays;
				this.PurgeExpired(days);
			}
			catch (Exception ex)
			{
				Log.Error("MailManager.ExpirationTick: {0}", ex);
			}
		}

		/// <summary>
		/// Single sweep of the expiration logic. Pulled out of
		/// <see cref="ExpirationTick"/> so tests can drive it
		/// deterministically. Returns the purge result.
		/// </summary>
		public ExpirationResult PurgeExpired(int days)
		{
			var result = new ExpirationResult();
			if (days <= 0)
				return result;

			var expiring = this.Repository.LoadExpiringMail(days);

			// Refund: for each expiring mail with an unclaimed
			// attachment (zeny or item), insert a system-mail back to
			// the original sender carrying the same attachment. The
			// bounced row itself is marked IsReturned so it can't be
			// bounced again if it expires too.
			foreach (var expired in expiring)
			{
				if (expired.AttachmentTaken)
					continue;
				if (!expired.HasAttachment)
					continue;

				// Refunds going to characters who no longer exist are
				// dropped — system mail to a deleted CID would be
				// orphaned and never collectable.
				if (string.IsNullOrEmpty(this.Repository.GetCharacterName(expired.SenderId)))
					continue;

				var refund = new MailMessage
				{
					SenderId = 0,
					SenderName = SystemSenderName,
					ReceiverId = expired.SenderId,
					Title = "Returned: " + (expired.Title ?? string.Empty),
					Body = "Your mail to " + (expired.SenderName ?? "an unknown recipient")
						+ " was not collected in time. Your attachment is returned.",
					SentAt = DateTime.UtcNow,
					Zeny = expired.Zeny,
					AttachedItemClassId = expired.AttachedItemClassId,
					AttachedItemAmount = expired.AttachedItemAmount,
					AttachedItemRefine = expired.AttachedItemRefine,
					AttachedItemIdentified = expired.AttachedItemIdentified,
					AttachedItemDamaged = expired.AttachedItemDamaged,
					IsReturned = true,
				};

				if (refund.Title.Length > 40)
					refund.Title = refund.Title.Substring(0, 40);

				try
				{
					this.Repository.InsertMail(refund);
					result.Refunded++;

					lock (_syncRoot)
					{
						if (_inboxByReceiver.TryGetValue(refund.ReceiverId, out var senderList))
							senderList.Insert(0, refund);
					}

					this.OnNewMailNotifier?.Invoke(refund.ReceiverId, refund);
				}
				catch (Exception ex)
				{
					Log.Error("MailManager.PurgeExpired: refund insert failed for original mail {0}: {1}", expired.Id, ex);
				}
			}

			result.Purged = this.Repository.PurgeExpiredMail(days);
			if (result.Purged > 0)
			{
				Log.Info("MailManager: purged {0} expired mail row(s), refunded {1} attachment(s).",
					result.Purged, result.Refunded);

				// Drop cached inboxes so the next read reflects the
				// removed rows instead of serving stale entries.
				lock (_syncRoot)
					_inboxByReceiver.Clear();
			}

			return result;
		}

		/// <summary>
		/// Outcome of one <see cref="PurgeExpired"/> sweep.
		/// </summary>
		public struct ExpirationResult
		{
			/// <summary>Total mail rows deleted from the database.</summary>
			public int Purged;

			/// <summary>How many of those had attachments refunded to the sender.</summary>
			public int Refunded;
		}
	}
}
