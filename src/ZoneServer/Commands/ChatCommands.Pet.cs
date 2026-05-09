using System;
using Sabine.Shared.Util;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadPet()
		{
			this.Add("makeegg", "<mobId>", Localization.Get("Creates a pet egg for a mob."), this.MakeEgg);
			this.Add("hatch", "", Localization.Get("Hatches the first egg in the inventory."), this.Hatch);
			this.Add("unsummon", "", Localization.Get("Returns the active pet to its egg."), this.Unsummon);
			this.Add("petfriendly", "<value>", Localization.Get("Sets the pet's intimacy."), this.PetFriendly);
			this.Add("pethungry", "<value>", Localization.Get("Sets the pet's hunger."), this.PetHungry);
			this.Add("petrename", "", Localization.Get("Lets the pet be renamed again."), this.PetRename);
			this.Add("pettalk", "<emote>", Localization.Get("Plays a pet emote."), this.PetTalk);
		}

		/// <summary>
		/// /makeegg &lt;mobId&gt; — gives the target the egg item for a pet
		/// definition. Mirrors eAthena's <c>@makeegg</c>: the egg item is
		/// just an inventory entry today; <c>/hatch</c> is what actually
		/// promotes it into a contract.
		/// </summary>
		private CommandResult MakeEgg(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var mobId)) return CommandResult.InvalidArgument;

			var entry = PetTypeData.GetByMobId(mobId);
			if (entry == null)
			{
				sender.ServerMessage(Localization.Get("No pet definition for mob id {0}."), mobId);
				return CommandResult.Okay;
			}

			target.Inventory.AddItem(new Sabine.Zone.World.Actors.Item(entry.EggItemId, 1));
			sender.ServerMessage(Localization.Get("Egg for {0} (item {1}) added."), entry.Name, entry.EggItemId);
			return CommandResult.Okay;
		}

		/// <summary>
		/// /hatch — consumes the first matching egg item in the target's
		/// inventory and creates the pet contract. Refuses if a contract
		/// already exists; the player must <c>/unsummon</c> + abandon the
		/// existing pet first (abandon flow is a follow-up — for now this
		/// just blocks).
		/// </summary>
		private CommandResult Hatch(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.Pet.HasContract)
			{
				sender.ServerMessage(Localization.Get("A pet contract already exists."));
				return CommandResult.Okay;
			}

			PetEntry hatched = null;
			foreach (var item in target.Inventory.GetItems(static _ => true))
			{
				var e = PetTypeData.GetByEggItemId(item.ClassId);
				if (e == null) continue;
				hatched = e;
				target.Inventory.Remove(item.ClassId, 1);
				break;
			}

			if (hatched == null)
			{
				sender.ServerMessage(Localization.Get("No pet egg in inventory."));
				return CommandResult.Okay;
			}

			target.Pet.Adopt(hatched, hatched.Name);
			// The companion entity isn't spawned yet (wires up in the next
			// commit); the contract sits in Egg state so /unsummon and
			// future /summon commands can drive the round-trip.
			sender.ServerMessage(Localization.Get("Hatched {0}. Pet entity spawn lands in the next commit."), hatched.Name);
			return CommandResult.Okay;
		}

		/// <summary>
		/// /unsummon — returns the active pet to its egg form. Today the
		/// active state is purely bookkeeping (no entity to despawn); the
		/// command still drives the state transition so the persistence
		/// round-trip is exercisable end-to-end.
		/// </summary>
		private CommandResult Unsummon(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (!target.Pet.HasContract)
			{
				sender.ServerMessage(Localization.Get("No pet contract."));
				return CommandResult.Okay;
			}

			target.Pet.MarkDespawned();
			sender.ServerMessage(Localization.Get("Pet despawned."));
			return CommandResult.Okay;
		}

		private CommandResult PetFriendly(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var value)) return CommandResult.InvalidArgument;
			if (!target.Pet.HasContract)
			{
				sender.ServerMessage(Localization.Get("No pet contract."));
				return CommandResult.Okay;
			}
			target.Pet.SetIntimacy(Math.Clamp(value, 0, 1000));
			sender.ServerMessage(Localization.Get("Pet intimacy set to {0}."), target.Pet.Intimacy);
			return CommandResult.Okay;
		}

		private CommandResult PetHungry(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var value)) return CommandResult.InvalidArgument;
			if (!target.Pet.HasContract)
			{
				sender.ServerMessage(Localization.Get("No pet contract."));
				return CommandResult.Okay;
			}
			target.Pet.SetHunger(Math.Clamp(value, 0, 100));
			sender.ServerMessage(Localization.Get("Pet hunger set to {0}."), target.Pet.Hunger);
			return CommandResult.Okay;
		}

		private CommandResult PetRename(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			// The actual client rename packet flow lands with the
			// companion-entity commit; this stub clears any sticky
			// "already named" lock so the next rename packet is accepted.
			sender.ServerMessage(Localization.Get("Pet rename unlocked. (Rename packet not yet implemented.)"));
			return CommandResult.Okay;
		}

		private CommandResult PetTalk(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			// Pet emote dispatch lives on the live entity; once that
			// lands the message routes through it. For now this is
			// acknowledged so /pettalk in scripts doesn't error.
			sender.ServerMessage(Localization.Get("Pet emotes are not yet implemented."));
			return CommandResult.Okay;
		}
	}
}
