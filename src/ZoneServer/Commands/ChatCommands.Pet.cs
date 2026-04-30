using Sabine.Shared.Util;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadPet()
		{
			this.Add("makeegg", "<id>", Localization.Get("Creates a pet egg."), this.MakeEgg);
			this.Add("hatch", "", Localization.Get("Hatches an egg."), this.Hatch);
			this.Add("petfriendly", "<value>", Localization.Get("Sets the pet's intimacy."), this.PetFriendly);
			this.Add("pethungry", "<value>", Localization.Get("Sets the pet's hunger."), this.PetHungry);
			this.Add("petrename", "", Localization.Get("Lets the pet be renamed again."), this.PetRename);
			this.Add("pettalk", "<emote>", Localization.Get("Plays a pet emote."), this.PetTalk);
		}

		private CommandResult NotImplPet(PlayerCharacter sender, string what)
		{
			sender.ServerMessage(Localization.Get("{0}: pet system is partial; not yet wired up."), what);
			return CommandResult.Okay;
		}

		private CommandResult MakeEgg(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplPet(sender, "makeegg");
		private CommandResult Hatch(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplPet(sender, "hatch");
		private CommandResult PetFriendly(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplPet(sender, "petfriendly");
		private CommandResult PetHungry(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplPet(sender, "pethungry");
		private CommandResult PetRename(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplPet(sender, "petrename");
		private CommandResult PetTalk(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplPet(sender, "pettalk");
	}
}
