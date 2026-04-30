using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Sabine.Zone.World
{
	/// <summary>
	/// Bare-bones in-memory mail queue. Receivers see queued mail as a
	/// server message on next login; persistence is not implemented yet.
	/// </summary>
	public class MailService
	{
		public static MailService Instance { get; } = new MailService();

		private readonly ConcurrentDictionary<string, List<MailMessage>> _byReceiver = new(StringComparer.OrdinalIgnoreCase);
		private int _nextId;

		public MailMessage Send(string sender, string receiver, string title, string body)
		{
			var msg = new MailMessage
			{
				Id = Interlocked.Increment(ref _nextId),
				Sender = sender,
				Receiver = receiver,
				Title = title,
				Body = body,
				SentAt = DateTime.UtcNow,
			};

			var list = _byReceiver.GetOrAdd(receiver, _ => new List<MailMessage>());
			lock (list) list.Add(msg);
			return msg;
		}

		public IReadOnlyList<MailMessage> GetUnread(string receiver)
		{
			if (!_byReceiver.TryGetValue(receiver, out var list))
				return Array.Empty<MailMessage>();

			lock (list) return list.Where(m => !m.Read).ToList();
		}

		public void MarkAllRead(string receiver)
		{
			if (!_byReceiver.TryGetValue(receiver, out var list))
				return;

			lock (list)
				foreach (var m in list)
					m.Read = true;
		}
	}

	public class MailMessage
	{
		public int Id { get; set; }
		public string Sender { get; set; }
		public string Receiver { get; set; }
		public string Title { get; set; }
		public string Body { get; set; }
		public DateTime SentAt { get; set; }
		public bool Read { get; set; }
	}
}
