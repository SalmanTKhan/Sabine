using System;
using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Mail;
using Xunit;

namespace Tests.Sabine.Shared.Mail
{
	/// <summary>
	/// Exercises <see cref="MailManager"/>'s validation rules with an
	/// in-memory <see cref="IMailRepository"/> fake. The manager's hard
	/// dependencies on <c>ZoneServer.Instance.World.Maps</c> and the
	/// player object are routed through the new <c>Repository</c>
	/// property and <c>OnNewMailNotifier</c> hook so the singleton can
	/// be driven without booting a server.
	/// </summary>
	[Collection("MailManager")]
	public class MailManagerTests
	{
		// ---------- Send -----------------------------------------------

		[Fact]
		public void Send_returns_ReceiverUnknown_when_recipient_not_in_repository()
		{
			using var scope = NewScope(out var repo, out var notifications);

			// No characters registered → name lookup returns 0.
			var (result, msg) = SendMail(repo, senderId: 1, senderName: "Alice", receiverName: "Bob",
				title: "Hi", body: "...", zeny: 100);

			Assert.Equal(MailManager.SendResult.ReceiverUnknown, result);
			Assert.Null(msg);
			Assert.Empty(repo.AllMail());
			Assert.Empty(notifications);
		}

		[Fact]
		public void Send_inserts_row_and_notifies_when_receiver_resolves()
		{
			using var scope = NewScope(out var repo, out var notifications);
			repo.RegisterCharacter(id: 7, name: "Bob");

			var (result, msg) = SendMail(repo, 1, "Alice", "Bob", "Hi", "Hello there", 250);

			Assert.Equal(MailManager.SendResult.Success, result);
			Assert.NotNull(msg);
			Assert.NotEqual(0, msg.Id);
			Assert.Equal(7, msg.ReceiverId);
			Assert.Equal(250, msg.Zeny);
			Assert.False(msg.IsReturned);
			Assert.False(msg.Read);

			Assert.Single(repo.AllMail());
			Assert.Single(notifications);
			Assert.Equal((7, msg.Id), notifications[0]);
		}

		// ---------- Open -----------------------------------------------

		[Fact]
		public void Open_marks_read_and_is_idempotent()
		{
			using var scope = NewScope(out var repo, out var _);
			repo.RegisterCharacter(7, "Bob");
			var msg = SeedMail(repo, senderId: 1, senderName: "Alice", receiverId: 7);

			// Prime the cache.
			_ = MailManager.Instance.GetInbox(7);

			var opened1 = MailManager.Instance.Open(7, msg.Id);
			Assert.True(opened1.Read);
			Assert.Equal(1, repo.UpdateReadCalls);

			// Second open is a no-op write — flag already set.
			var opened2 = MailManager.Instance.Open(7, msg.Id);
			Assert.True(opened2.Read);
			Assert.Equal(1, repo.UpdateReadCalls);
		}

		// ---------- Delete ---------------------------------------------

		[Fact]
		public void Delete_refuses_while_attachment_pending()
		{
			using var scope = NewScope(out var repo, out var _);
			repo.RegisterCharacter(7, "Bob");
			var msg = SeedMail(repo, 1, "Alice", 7, zeny: 100);
			_ = MailManager.Instance.GetInbox(7);

			var ok = MailManager.Instance.Delete(7, msg.Id);

			Assert.False(ok);
			Assert.Single(repo.AllMail());
		}

		[Fact]
		public void Delete_succeeds_after_attachment_taken_flag_set()
		{
			using var scope = NewScope(out var repo, out var _);
			repo.RegisterCharacter(7, "Bob");
			var msg = SeedMail(repo, 1, "Alice", 7, zeny: 100);
			msg.AttachmentTaken = true;
			_ = MailManager.Instance.GetInbox(7);

			var ok = MailManager.Instance.Delete(7, msg.Id);

			Assert.True(ok);
			Assert.Empty(repo.AllMail());
		}

		[Fact]
		public void Delete_succeeds_when_no_attachment_present()
		{
			using var scope = NewScope(out var repo, out var _);
			repo.RegisterCharacter(7, "Bob");
			var msg = SeedMail(repo, 1, "Alice", 7);   // no zeny, no item
			_ = MailManager.Instance.GetInbox(7);

			var ok = MailManager.Instance.Delete(7, msg.Id);
			Assert.True(ok);
		}

		// ---------- ReturnToSender -------------------------------------

		[Fact]
		public void ReturnToSender_refuses_when_attachment_already_taken()
		{
			using var scope = NewScope(out var repo, out var _);
			repo.RegisterCharacter(1, "Alice");
			repo.RegisterCharacter(7, "Bob");
			var msg = SeedMail(repo, 1, "Alice", 7, zeny: 100);
			msg.AttachmentTaken = true;
			_ = MailManager.Instance.GetInbox(7);

			var ok = MailManager.Instance.ReturnToSender(FakePlayer(7, "Bob"), msg.Id);

			Assert.False(ok);
		}

		[Fact]
		public void ReturnToSender_refuses_when_mail_is_already_a_bounce()
		{
			using var scope = NewScope(out var repo, out var _);
			repo.RegisterCharacter(1, "Alice");
			repo.RegisterCharacter(7, "Bob");
			var msg = SeedMail(repo, 1, "Alice", 7, zeny: 100);
			msg.IsReturned = true;
			_ = MailManager.Instance.GetInbox(7);

			var ok = MailManager.Instance.ReturnToSender(FakePlayer(7, "Bob"), msg.Id);

			Assert.False(ok);
		}

		[Fact]
		public void ReturnToSender_refuses_when_original_sender_no_longer_exists()
		{
			using var scope = NewScope(out var repo, out var _);
			// Bob exists; Alice does NOT (sender was deleted).
			repo.RegisterCharacter(7, "Bob");
			var msg = SeedMail(repo, 1, "Alice", 7, zeny: 100);
			_ = MailManager.Instance.GetInbox(7);

			var ok = MailManager.Instance.ReturnToSender(FakePlayer(7, "Bob"), msg.Id);

			Assert.False(ok);
		}

		[Fact]
		public void ReturnToSender_inserts_bounce_swaps_addressee_and_deletes_original()
		{
			using var scope = NewScope(out var repo, out var notifications);
			repo.RegisterCharacter(1, "Alice");
			repo.RegisterCharacter(7, "Bob");
			var msg = SeedMail(repo, 1, "Alice", 7, zeny: 100, title: "Heya");
			_ = MailManager.Instance.GetInbox(7);

			var ok = MailManager.Instance.ReturnToSender(FakePlayer(7, "Bob"), msg.Id);

			Assert.True(ok);

			// Original is gone; one bounce is present, addressed to Alice.
			var all = repo.AllMail();
			Assert.Single(all);
			var bounce = all.Single();
			Assert.Equal(7, bounce.SenderId);
			Assert.Equal("Bob", bounce.SenderName);
			Assert.Equal(1, bounce.ReceiverId);
			Assert.StartsWith("RE: ", bounce.Title);
			Assert.True(bounce.IsReturned);
			Assert.Equal(100, bounce.Zeny);

			// Notification went to Alice's id.
			Assert.Single(notifications);
			Assert.Equal((1, bounce.Id), notifications[0]);
		}

		// ---------- PurgeExpired (refund-on-expire) --------------------

		[Fact]
		public void PurgeExpired_refunds_unclaimed_attachments_to_sender()
		{
			using var scope = NewScope(out var repo, out var notifications);
			repo.RegisterCharacter(1, "Alice");
			repo.RegisterCharacter(7, "Bob");

			var hasAttachment = SeedMail(repo, 1, "Alice", 7, zeny: 500);
			hasAttachment.SentAt = DateTime.UtcNow - TimeSpan.FromDays(30);
			repo.MarkExpiring(hasAttachment);

			var result = MailManager.Instance.PurgeExpired(days: 15);

			Assert.Equal(1, result.Purged);
			Assert.Equal(1, result.Refunded);

			// Original is gone; refund row exists addressed to Alice.
			var all = repo.AllMail();
			Assert.Single(all);
			var refund = all.Single();
			Assert.Equal(0, refund.SenderId);                          // system sender
			Assert.Equal(MailManager.SystemSenderName, refund.SenderName);
			Assert.Equal(1, refund.ReceiverId);
			Assert.True(refund.IsReturned);
			Assert.Equal(500, refund.Zeny);

			Assert.Single(notifications);
		}

		[Fact]
		public void PurgeExpired_skips_refund_when_attachment_already_taken()
		{
			using var scope = NewScope(out var repo, out var notifications);
			repo.RegisterCharacter(1, "Alice");
			repo.RegisterCharacter(7, "Bob");

			var taken = SeedMail(repo, 1, "Alice", 7, zeny: 500);
			taken.SentAt = DateTime.UtcNow - TimeSpan.FromDays(30);
			taken.AttachmentTaken = true;
			repo.MarkExpiring(taken);

			var result = MailManager.Instance.PurgeExpired(days: 15);

			Assert.Equal(1, result.Purged);
			Assert.Equal(0, result.Refunded);

			Assert.Empty(repo.AllMail());
			Assert.Empty(notifications);
		}

		[Fact]
		public void PurgeExpired_skips_refund_when_sender_no_longer_exists()
		{
			using var scope = NewScope(out var repo, out var notifications);
			// Only Bob exists — Alice was deleted before her mail expired.
			repo.RegisterCharacter(7, "Bob");

			var orphan = SeedMail(repo, 1, "Alice", 7, zeny: 500);
			orphan.SentAt = DateTime.UtcNow - TimeSpan.FromDays(30);
			repo.MarkExpiring(orphan);

			var result = MailManager.Instance.PurgeExpired(days: 15);

			Assert.Equal(1, result.Purged);
			Assert.Equal(0, result.Refunded);

			Assert.Empty(repo.AllMail());
			Assert.Empty(notifications);
		}

		[Fact]
		public void PurgeExpired_with_zero_days_is_noop()
		{
			using var scope = NewScope(out var repo, out var _);
			repo.RegisterCharacter(7, "Bob");
			var msg = SeedMail(repo, 1, "Alice", 7, zeny: 100);
			repo.MarkExpiring(msg);

			var result = MailManager.Instance.PurgeExpired(days: 0);

			Assert.Equal(0, result.Purged);
			Assert.Equal(0, result.Refunded);
			Assert.Single(repo.AllMail());
		}

		// ===== Helpers =================================================

		private static (MailManager.SendResult, MailMessage) SendMail(
			FakeMailRepository repo, int senderId, string senderName, string receiverName,
			string title, string body, int zeny)
		{
			// MailManager.Send needs a PlayerCharacter for sender.Id and
			// sender.Name only; we construct a minimal stand-in.
			var sender = FakePlayer(senderId, senderName);
			var result = MailManager.Instance.Send(sender, receiverName, title, body,
				zeny, itemClassId: 0, itemAmount: 0, itemRefine: 0,
				itemIdentified: true, itemDamaged: false, out var msg);
			return (result, msg);
		}

		private static MailMessage SeedMail(FakeMailRepository repo, int senderId, string senderName,
			int receiverId, int zeny = 0, string title = "Test")
		{
			var msg = new MailMessage
			{
				SenderId = senderId,
				SenderName = senderName,
				ReceiverId = receiverId,
				Title = title,
				Body = "body",
				Zeny = zeny,
				SentAt = DateTime.UtcNow,
			};
			repo.InsertMail(msg);
			return msg;
		}

		/// <summary>
		/// PlayerCharacter requires extensive runtime state (Inventory,
		/// Connection, Parameters). MailManager only reads <c>Id</c> +
		/// <c>Name</c> on the sender path tested here, so a derived
		/// stub with parameterless construction is enough.
		/// </summary>
		private static global::Sabine.Zone.World.Actors.PlayerCharacter FakePlayer(int id, string name)
		{
			var pc = (global::Sabine.Zone.World.Actors.PlayerCharacter)System.Runtime.Serialization.FormatterServices
				.GetUninitializedObject(typeof(global::Sabine.Zone.World.Actors.PlayerCharacter));
			pc.Id = id;
			SetPrivateName(pc, name);
			return pc;
		}

		private static void SetPrivateName(global::Sabine.Zone.World.Actors.PlayerCharacter pc, string name)
		{
			var prop = typeof(global::Sabine.Zone.World.Actors.PlayerCharacter).GetProperty("Name");
			var setter = prop?.GetSetMethod(nonPublic: true);
			if (setter != null)
				setter.Invoke(pc, new object[] { name });
			else
			{
				// Fall back to backing field.
				var field = typeof(global::Sabine.Zone.World.Actors.PlayerCharacter)
					.GetField("<Name>k__BackingField",
						System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				field?.SetValue(pc, name);
			}
		}

		// Scopes the MailManager singleton so each test gets a fresh
		// fake repo + notification capture; restores defaults on Dispose.
		private static IDisposable NewScope(out FakeMailRepository repo, out List<(int receiverId, int mailId)> notifications)
		{
			repo = new FakeMailRepository();
			var captured = new List<(int, int)>();
			notifications = captured;

			var prevRepo = MailManager.Instance.Repository;
			var prevNotifier = MailManager.Instance.OnNewMailNotifier;

			MailManager.Instance.Repository = repo;
			MailManager.Instance.OnNewMailNotifier = (rid, msg) => captured.Add((rid, msg.Id));

			// Clear the manager's inbox cache so prior tests don't leak.
			InvalidateInboxCache();

			return new ActionDisposable(() =>
			{
				MailManager.Instance.Repository = prevRepo;
				MailManager.Instance.OnNewMailNotifier = prevNotifier;
				InvalidateInboxCache();
			});
		}

		private static void InvalidateInboxCache()
		{
			var field = typeof(MailManager).GetField("_inboxByReceiver",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			var dict = (System.Collections.IDictionary)field.GetValue(MailManager.Instance);
			lock (typeof(MailManager))
				dict.Clear();
		}

		private sealed class ActionDisposable : IDisposable
		{
			private readonly Action _onDispose;
			public ActionDisposable(Action onDispose) { _onDispose = onDispose; }
			public void Dispose() => _onDispose();
		}
	}

	/// <summary>
	/// In-memory <see cref="IMailRepository"/> for tests. Tracks calls
	/// and lets tests register characters by name/id.
	/// </summary>
	internal sealed class FakeMailRepository : IMailRepository
	{
		private readonly Dictionary<int, MailMessage> _mail = new();
		private readonly Dictionary<int, string> _charNames = new();
		private readonly Dictionary<string, int> _charIds = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<int> _expiring = new();
		private int _nextId = 1;

		public int UpdateReadCalls { get; private set; }

		public void RegisterCharacter(int id, string name)
		{
			_charNames[id] = name;
			_charIds[name] = id;
		}

		public void MarkExpiring(MailMessage msg) => _expiring.Add(msg.Id);

		public IReadOnlyList<MailMessage> AllMail() => _mail.Values.ToList();

		public List<MailMessage> LoadMailFor(int receiverId)
			=> _mail.Values.Where(m => m.ReceiverId == receiverId).ToList();

		public List<MailMessage> LoadExpiringMail(int days)
		{
			if (days <= 0) return new List<MailMessage>();
			return _mail.Values.Where(m => _expiring.Contains(m.Id)).ToList();
		}

		public void InsertMail(MailMessage msg)
		{
			msg.Id = _nextId++;
			_mail[msg.Id] = msg;
		}

		public void UpdateMailRead(int mailId)
		{
			UpdateReadCalls++;
			if (_mail.TryGetValue(mailId, out var m))
				m.Read = true;
		}

		public void UpdateMailAttachmentTaken(int mailId)
		{
			if (_mail.TryGetValue(mailId, out var m))
				m.AttachmentTaken = true;
		}

		public void DeleteMail(int mailId)
		{
			_mail.Remove(mailId);
			_expiring.Remove(mailId);
		}

		public int PurgeExpiredMail(int days)
		{
			if (days <= 0) return 0;
			var ids = _expiring.ToList();
			foreach (var id in ids)
				_mail.Remove(id);
			_expiring.Clear();
			return ids.Count;
		}

		public int GetCharacterIdByName(string name)
			=> _charIds.TryGetValue(name, out var id) ? id : 0;

		public string GetCharacterName(int characterId)
			=> _charNames.TryGetValue(characterId, out var n) ? n : null;
	}
}
