//--- Sabine Script ---------------------------------------------------------
// Prontera NPCs (Beta1)
//--- Description -----------------------------------------------------------
// Beta1-era Prontera NPCs that don't exist in the alpha client, such as
// Hollgrehenn the refiner. The refining UI was introduced after alpha,
// so this NPC and its dialog live here rather than in the alpha episode.
//---------------------------------------------------------------------------

using System.Linq;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Zone.Battle;
using Sabine.Zone.Scripting;
using Sabine.Zone.World.Actors;
using static Sabine.Zone.Scripting.Shortcuts;
using static Sabine.Shared.Const.IdentityId;

[RequiresMaps("prt_intr02")]
public class Beta1PronteraNpcsScript : GeneralScript
{
	public override void Load()
	{
		LoadNpcs();
	}

	private static void LoadNpcs()
	{
		// Hollgrehenn the refiner. Refines weapons or armor for ore +
		// zeny per attempt. The classic client has no refining UI, so
		// the dialog asks which equipped slot to operate on and then
		// defers to RefinementService.TryRefine.
		AddNpc("Hollgrehenn", JT_BLACKSMITH, "prt_intr02", 165, 100, 4, RunHollgrehennDialog);
	}

	private static async System.Threading.Tasks.Task RunHollgrehennDialog(Sabine.Zone.Scripting.Dialogues.Dialog dialog)
	{
		var player = dialog.Player;

		await dialog.Talk("[Hollgrehenn]");
		await dialog.Talk("I am Hollgrehenn, the refiner. I can upgrade your weapons and armor for a small fee. Which would you like to refine?");

		var choice = await dialog.Select(
			Option("My weapon", "weapon"),
			Option("My armor", "armor"),
			Option("Nothing right now", "leave"));

		if (choice == "leave")
		{
			await dialog.Talk("Come back when you have something for me to work on.");
			return;
		}

		Item item = null;
		if (choice == "weapon")
			item = player.Inventory.RightHand;
		else if (choice == "armor")
			item = player.Inventory.GetItems(i => i.IsEquipped && i.Data.Type == ItemType.Armor && (i.EquippedOn & EquipSlots.Body) != 0).FirstOrDefault();

		var group = RefinementService.ResolveGroup(item);
		if (item == null || group == null)
		{
			await dialog.Talk("You're not wearing anything I can refine.");
			return;
		}

		var weaponLevel = group == RefineGroup.Armor ? 0 : item.Data.WeaponLevel;
		var maxRefine = RefinementService.GetMaxRefine(group.Value, weaponLevel);
		if (item.RefineLevel >= maxRefine)
		{
			await dialog.Talk($"Your {item.Data.Name} is already at the maximum refine level (+{maxRefine}). I can't push it any further.");
			return;
		}

		var nextLevel = item.RefineLevel + 1;
		var available = RefinementService.GetAvailableCostTypes(item);
		var costType = RefineCostType.Normal;

		// Surface HD/Enriched options when the data offers more than
		// one cost type (renewal). Classic data only has Normal so
		// the menu collapses to a yes/no.
		if (available.Count > 1)
		{
			var typeOptions = available.Select(t => Option(DescribeCostType(t), t.ToString())).ToList();
			typeOptions.Add(Option("Cancel", "cancel"));

			await dialog.Talk($"Which ore would you like to use for your {item.Data.Name}?");
			var pick = await dialog.Select($"Refining to +{nextLevel}", typeOptions);
			if (pick == "cancel" || pick == "__cancelled__" || pick == null)
			{
				await dialog.Talk("As you wish.");
				return;
			}
			costType = System.Enum.Parse<RefineCostType>(pick);
		}

		var rate = RefinementService.GetSuccessRate(group.Value, weaponLevel, item.RefineLevel, costType);
		await dialog.Talk($"Refining your {item.Data.Name} to +{nextLevel} with {costType} ore. Estimated success chance: {rate}%. Shall I proceed?");
		var confirm = await dialog.Select(Option("Yes, refine it.", "yes"), Option("Not now.", "no"));
		if (confirm != "yes")
		{
			await dialog.Talk("As you wish.");
			return;
		}

		var result = RefinementService.TryRefine(player, item, costType);
		switch (result)
		{
			case RefineResult.Success:
				await dialog.Talk($"Wonderful! Your {item.Data.Name} is now +{item.RefineLevel}!");
				break;
			case RefineResult.Failed:
				await dialog.Talk("Hmm, the refinement failed. The item is unharmed, but the materials are gone.");
				break;
			case RefineResult.Downgraded:
				await dialog.Talk($"The HD ore protected your {item.Data.Name} from breaking, but it dropped to +{item.RefineLevel}.");
				break;
			case RefineResult.Broken:
				await dialog.Talk("...Oh no. The item couldn't withstand the strain and shattered. My condolences.");
				break;
			case RefineResult.MissingMaterial:
				await dialog.Talk("You don't have the right ore for this. Bring me the proper material first.");
				break;
			case RefineResult.MissingZeny:
				await dialog.Talk("You don't have enough Zeny.");
				break;
			case RefineResult.AtMaxRefine:
				await dialog.Talk("That item can't be refined any further.");
				break;
			case RefineResult.NotRefinable:
				await dialog.Talk("I don't know how to work that piece.");
				break;
			case RefineResult.CostTypeUnavailable:
				await dialog.Talk("I can't refine that piece with this ore.");
				break;
		}
	}

	private static string DescribeCostType(RefineCostType type) => type switch
	{
		RefineCostType.Normal => "Normal ore (cheap, may break on failure)",
		RefineCostType.HD => "HD ore (downgrades on failure instead of breaking)",
		RefineCostType.Enriched => "Enriched ore (higher chance, costs more)",
		_ => type.ToString(),
	};
}
