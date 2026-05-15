//--- Sabine Script ---------------------------------------------------------
// Geneticist (eAthena era)
//--- Description -----------------------------------------------------------
// In-world entry point for homunculus maintenance services: rename,
// stat shuffle, intimacy reset, and (admin / completion of Bioethics)
// evolution / mutation gates. Player-facing wrapper around
// HomunculusService and HomunculusComponent — keeps the chat-command
// surface usable through dialogue too.
//
// Lives in alde_alche (Al De Baran Alchemist Guild), eAthena's
// canonical homunculus-research location. RequiresMaps gates the
// load so eras without the map silently skip the NPC.
//---------------------------------------------------------------------------

using System.Threading.Tasks;
using Sabine.Shared.Util;
using Sabine.Zone.Scripting;
using Sabine.Zone.Scripting.Dialogues;
using Sabine.Zone.Skills.Homunculi;
using Sabine.Zone.World.Actors.Components.Characters;
using static Sabine.Zone.Scripting.Shortcuts;
using static Sabine.Shared.Const.IdentityId;

[RequiresMaps("alde_alche")]
public class EathenaGeneticistNpcScript : GeneralScript
{
	private const int IntimacyResetBaseline = 100;

	public override void Load()
	{
		AddNpc("Geneticist", JT_4_M_REPAIR, "alde_alche", 49, 134, 4, RunGeneticistDialog);
	}

	private static async Task RunGeneticistDialog(Dialog dialog)
	{
		var player = dialog.Player;
		var homun = player.Homunculus;

		await dialog.Talk("[Geneticist]");

		if (homun == null || !homun.HasContract)
		{
			await dialog.Talk("You don't have a homunculus contract. Come back once you've raised one through Bioethics.");
			return;
		}

		await dialog.Talk($"Your {homun.Type} '{homun.Name}' — what can I do for it today?");

		// Rename is not offered through dialogue — pre-2008 clients
		// have no string-input dialog packet. Players use the homun
		// panel's rename button (CZ_RENAME_MER) instead.
		var choice = await dialog.Select(
			Option("Shuffle baseline stats", "shuffle"),
			Option("Reset intimacy", "intimacy"),
			Option("Evolve", "evolve"),
			Option("Mutate (S-class)", "mutate"),
			Option("Nothing", "leave"));

		switch (choice)
		{
			case "shuffle":
				homun.ShuffleBaselineStats();
				await dialog.Talk($"Done. Baseline reshuffled: HP {homun.Hp}/{homun.HpMax}.");
				break;
			case "intimacy":
				homun.SetIntimacy(IntimacyResetBaseline);
				await dialog.Talk($"I've reset its intimacy to {IntimacyResetBaseline}. Treat it well from here.");
				break;
			case "evolve":
				await HandleEvolve(dialog, homun);
				break;
			case "mutate":
				await HandleMutate(dialog, homun);
				break;
			case "leave":
				await dialog.Talk("Come back any time.");
				break;
		}
	}

	private static async Task HandleEvolve(Dialog dialog, HomunculusComponent homun)
	{
		if (homun.Type.IsEvolved() || homun.Type.IsSClass())
		{
			await dialog.Talk("Already evolved. There's nothing left for me to coax out of it on that front.");
			return;
		}

		var result = homun.Evolve();
		if (result == HomunculusType.None)
			await dialog.Talk($"It isn't ready. Evolution wants intimacy of at least {HomunculusComponent.EvolveIntimacyThreshold} on a live homunculus.");
		else
			await dialog.Talk($"Marvelous — it has evolved into a {result}.");
	}

	private static async Task HandleMutate(Dialog dialog, HomunculusComponent homun)
	{
		if (!homun.Type.IsEvolved())
		{
			await dialog.Talk("Mutation requires an already-evolved homunculus. Come back after the first transformation.");
			return;
		}

		var result = homun.Mutate();
		if (result == HomunculusType.None)
			await dialog.Talk($"Not yet — mutation requires level {HomunculusComponent.MutateLevelThreshold} and intimacy {HomunculusComponent.MutateIntimacyThreshold}.");
		else
			await dialog.Talk($"Astounding. Your companion has mutated into a {result}.");
	}
}
