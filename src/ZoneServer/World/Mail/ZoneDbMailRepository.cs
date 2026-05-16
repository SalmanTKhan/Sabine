using System.Collections.Generic;

namespace Sabine.Zone.World.Mail
{
	/// <summary>
	/// Production <see cref="IMailRepository"/> — every call delegates
	/// to the live <c>ZoneDb</c> via <c>ZoneServer.Instance.Database</c>.
	/// </summary>
	public class ZoneDbMailRepository : IMailRepository
	{
		public List<MailMessage> LoadMailFor(int receiverId)
			=> ZoneServer.Instance.Database.LoadMailFor(receiverId);

		public List<MailMessage> LoadExpiringMail(int days)
			=> ZoneServer.Instance.Database.LoadExpiringMail(days);

		public void InsertMail(MailMessage msg)
			=> ZoneServer.Instance.Database.InsertMail(msg);

		public void UpdateMailRead(int mailId)
			=> ZoneServer.Instance.Database.UpdateMailRead(mailId);

		public void UpdateMailAttachmentTaken(int mailId)
			=> ZoneServer.Instance.Database.UpdateMailAttachmentTaken(mailId);

		public void DeleteMail(int mailId)
			=> ZoneServer.Instance.Database.DeleteMail(mailId);

		public int PurgeExpiredMail(int days)
			=> ZoneServer.Instance.Database.PurgeExpiredMail(days);

		public int GetCharacterIdByName(string name)
			=> ZoneServer.Instance.Database.GetCharacterIdByName(name);

		public string GetCharacterName(int characterId)
			=> ZoneServer.Instance.Database.GetCharacterName(characterId);
	}
}
