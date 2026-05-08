//--- Sabine Script ---------------------------------------------------------
// Kafra Employee
//--- Description -----------------------------------------------------------
// Minimal Kafra NPC that grants access to the player's account storage.
// Other Kafra services (warp, save, cart rental, etc.) are out of scope
// for this NPC; they live on the parity backlog.
//---------------------------------------------------------------------------

using System.Threading.Tasks;
using Sabine.Zone.Scripting;
using Sabine.Zone.Scripting.Dialogues;
using static Sabine.Zone.Scripting.Shortcuts;
using static Sabine.Shared.Const.IdentityId;

public class KafraScript : GeneralScript
{
	public override void Load()
	{
		if (MapsExist("prontera"))
			AddNpc("Kafra Employee", JT_4_F_KAFRA1, "prontera", 146, 89, KafraDialog);
	}

	private static async Task KafraDialog(Dialog dialog)
	{
		var player = dialog.Player;

		dialog.Msg("[Kafra Employee]");
		dialog.Msg("Welcome to Kafra Corporation. How may I serve you?");
		await dialog.Next();

		var response = await dialog.Select(
			Option("Use Storage", "storage"),
			Option("Use Guild Storage", "guildstorage"),
			Option("Cancel", "cancel"));

		switch (response)
		{
			case "storage":
			{
				if (!CanOpenStorage(player))
				{
					dialog.Msg("[Kafra Employee]");
					dialog.Msg("I'm sorry, but you must complete your basic training before I can let you use our storage.");
					return;
				}

				dialog.Msg("[Kafra Employee]");
				dialog.Msg("Thank you. Please come again.");
				OpenStorage(player);
				break;
			}
			case "guildstorage":
			{
				// v1 gate: master-only. Per-position permission UI is
				// out of scope for now; the `GuildPermission.Storage`
				// flag is reserved for the follow-up that exposes
				// position-level grants.
				if (player.Guild == null)
				{
					dialog.Msg("[Kafra Employee]");
					dialog.Msg("I'm sorry, but you must belong to a guild to access guild storage.");
					player.ServerMessage("Guild Storage: you are not in a guild.");
					return;
				}

				if (player.Id != player.Guild.MasterCharId)
				{
					dialog.Msg("[Kafra Employee]");
					dialog.Msg("Only the guild master may access the guild storage at this time.");
					player.ServerMessage("Guild Storage: master-only access (v1).");
					return;
				}

				dialog.Msg("[Kafra Employee]");
				dialog.Msg("Opening your guild's storage. Please come again.");
				OpenGuildStorage(player);
				break;
			}
			case "cancel":
			{
				dialog.Msg("[Kafra Employee]");
				dialog.Msg("Have a good day.");
				break;
			}
		}
	}
}
