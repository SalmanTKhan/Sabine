using System;

namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines the read status of a mail message.
	/// </summary>
	public enum MailStatus
	{
		New,
		Unread,
		Read,
	}

	/// <summary>
	/// Defines the type of inbox a mail message belongs to.
	/// </summary>
	public enum MailInboxType
	{
		Normal = 0,
		Account,
		Returned
	}

	/// <summary>
	/// Defines the type of attachment in a mail message.
	/// </summary>
	[Flags]
	public enum MailAttachmentType
	{
		None = 0,
		Zeny = 1,
		Item = 2,
		All = Zeny | Item
	}
}
