using System;

namespace Sabine.Zone.World.Mail
{
	/// <summary>
	/// One persisted mail message. A single attachment slot (item +
	/// zeny) matches the classic eAthena shape; renewal-era multi-slot
	/// inboxes are not modeled.
	/// </summary>
	public class MailMessage
	{
		public int Id { get; set; }
		public int SenderId { get; set; }
		public string SenderName { get; set; } = string.Empty;
		public int ReceiverId { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;
		public DateTime SentAt { get; set; }

		public int Zeny { get; set; }
		public int AttachedItemClassId { get; set; }
		public short AttachedItemAmount { get; set; }
		public byte AttachedItemRefine { get; set; }
		public bool AttachedItemIdentified { get; set; } = true;
		public bool AttachedItemDamaged { get; set; }

		public bool Read { get; set; }
		public bool AttachmentTaken { get; set; }

		/// <summary>
		/// True when this row is itself a bounce — sent back to the
		/// original sender via <c>MailManager.ReturnToSender</c>. Used
		/// to refuse a second bounce so a back-and-forth can't loop.
		/// </summary>
		public bool IsReturned { get; set; }

		public bool HasAttachment
			=> this.Zeny > 0 || (this.AttachedItemClassId > 0 && this.AttachedItemAmount > 0);
	}
}
