using System.Collections.Generic;

namespace Sabine.Zone.World.Mail
{
	/// <summary>
	/// Persistence seam for <see cref="MailManager"/>. The production
	/// adapter delegates to <c>ZoneDb</c>; tests substitute a fake
	/// in-memory implementation so the manager's validation rules can
	/// be exercised without a live database.
	/// </summary>
	public interface IMailRepository
	{
		/// <summary>Returns every mail row addressed to <paramref name="receiverId"/>.</summary>
		List<MailMessage> LoadMailFor(int receiverId);

		/// <summary>Returns every mail row older than <paramref name="days"/> days.</summary>
		List<MailMessage> LoadExpiringMail(int days);

		/// <summary>Inserts a new mail row and assigns its DB id back onto the message.</summary>
		void InsertMail(MailMessage msg);

		/// <summary>Marks a mail row's <c>read</c> flag.</summary>
		void UpdateMailRead(int mailId);

		/// <summary>Marks a mail row's <c>attachmentTaken</c> flag.</summary>
		void UpdateMailAttachmentTaken(int mailId);

		/// <summary>Removes a single mail row.</summary>
		void DeleteMail(int mailId);

		/// <summary>Deletes every mail row older than <paramref name="days"/> days and returns the row count.</summary>
		int PurgeExpiredMail(int days);

		/// <summary>Looks up a character's id by name, or 0 if not found.</summary>
		int GetCharacterIdByName(string name);

		/// <summary>Looks up a character's name by id, or null if not found.</summary>
		string GetCharacterName(int characterId);
	}
}
