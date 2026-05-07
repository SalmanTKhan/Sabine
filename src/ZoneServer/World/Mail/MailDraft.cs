namespace Sabine.Zone.World.Mail
{
	/// <summary>
	/// Per-character staging area for an outgoing mail. The client
	/// builds the message in stages — attach item, attach zeny, then
	/// CZ_MAIL_SEND with the body — so the server holds the partial
	/// state on the player until the send packet arrives or the user
	/// clears it via <c>CZ_MAIL_RESET_ITEM</c>.
	/// </summary>
	public class MailDraft
	{
		public int InventoryIndex { get; set; }
		public int ItemAmount { get; set; }
		public int Zeny { get; set; }

		public bool HasItem => this.InventoryIndex > 0 && this.ItemAmount > 0;
		public bool HasZeny => this.Zeny > 0;

		public void ClearItem()
		{
			this.InventoryIndex = 0;
			this.ItemAmount = 0;
		}

		public void ClearZeny()
		{
			this.Zeny = 0;
		}

		public void Clear()
		{
			this.ClearItem();
			this.ClearZeny();
		}
	}
}
