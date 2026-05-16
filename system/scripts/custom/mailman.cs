//--- Sabine Script ---------------------------------------------------------
// Postman
//--- Description -----------------------------------------------------------
// In-game NPC that opens the Mail UI on contact. Sabine's Mail system
// is fully wired (compose, send, receive, take attachment, return to
// sender, auto-purge) — the only missing piece was an interactive
// entry point. The unread-mail-on-login toast still works as before;
// this just lets players re-open the window mid-session.
//---------------------------------------------------------------------------

using System.Threading.Tasks;
using Sabine.Zone.Scripting;
using static Sabine.Zone.Scripting.Shortcuts;
using static Sabine.Shared.Const.IdentityId;

[RequiresMaps("prontera")]
public class Beta1MailmanNpcsScript : GeneralScript
{
	public override void Load()
	{
		LoadNpcs();
	}

	private static void LoadNpcs()
	{
		// Postman near the Prontera Kafra counter. Sprite is the
		// generic "4_M_REPAIR" NPC; swap to a dedicated mailman sprite
		// once one is added to IdentityId.
		AddNpc("Postman", JT_4_M_REPAIR, "prontera", 146, 89, 4, RunMailmanDialog);
	}

	private static async Task RunMailmanDialog(Sabine.Zone.Scripting.Dialogues.Dialog dialog)
	{
		var player = dialog.Player;

		await dialog.Talk("[Postman]");
		await dialog.Talk("Letters! Letters for you! Want me to open your mailbox?");

		var choice = await dialog.Select(
			Option("Yes please", "open"),
			Option("Not right now", "leave"));

		if (choice != "open")
		{
			await dialog.Talk("Come back anytime — I sort mail dawn till dusk.");
			return;
		}

		// flag = 0 opens the window; the client will auto-issue
		// CZ_MAIL_GET_LIST which CZ_MAIL_GET_LIST handler answers with
		// the full inbox via ZC_MAIL_REQ_GET_LIST.
		Sabine.Zone.Network.Send.ZC_MAIL_WINDOWS(player, 0);
	}
}
