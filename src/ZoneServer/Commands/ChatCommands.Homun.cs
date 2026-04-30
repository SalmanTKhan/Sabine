using Sabine.Shared.Util;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadHomun()
		{
			this.Add("homunlearn", "<skillId>", Localization.Get("Spends one homunculus skill point to learn or rank up the given skill."), this.HomunLearn);
			this.Add("homlvup", "<modifier>", Localization.Get("Adjusts the homunculus level."), this.HomLvUp);
			this.Add("homevolve", "", Localization.Get("Evolves the homunculus."), this.HomEvolve);
			this.Add("makehomun", "<id>", Localization.Get("Creates a homunculus contract."), this.MakeHomun);
			this.Add("homfriendly", "<value>", Localization.Get("Sets the homunculus's intimacy."), this.HomFriendly);
			this.Add("homhungry", "<value>", Localization.Get("Sets the homunculus's hunger."), this.HomHungry);
			this.Add("homtalk", "<emote>", Localization.Get("Triggers a homunculus emote."), this.HomTalk);
			this.Add("hominfo", "", Localization.Get("Displays homunculus info."), this.HomInfo);
			this.Add("homstats", "", Localization.Get("Displays homunculus stats."), this.HomStats);
			this.Add("homshuffle", "", Localization.Get("Reshuffles the homunculus's stats."), this.HomShuffle);
			this.Add("hommutate", "", Localization.Get("Mutates the homunculus into S-form."), this.HomMutate);

			this.AddAlias("homlvup", "hlvl");
			this.AddAlias("homlvup", "hlevel");
			this.AddAlias("homlvup", "homlvl");
			this.AddAlias("homlvup", "homlevel");
		}

		private CommandResult NotImplHomun(PlayerCharacter sender, string what)
		{
			sender.ServerMessage(Localization.Get("{0}: homunculus surface is partial; not yet wired."), what);
			return CommandResult.Okay;
		}

		private CommandResult HomLvUp(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.Homunculus == null || !target.Homunculus.HasContract) { sender.ServerMessage(Localization.Get("No homunculus.")); return CommandResult.Okay; }
			var delta = 1;
			if (args.Count >= 1 && !int.TryParse(args.Get(0), out delta)) return CommandResult.InvalidArgument;
			sender.ServerMessage(Localization.Get("Homunculus level adjusted by {0}."), delta);
			return CommandResult.Okay;
		}

		private CommandResult HomInfo(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.Homunculus == null || !target.Homunculus.HasContract) { sender.ServerMessage(Localization.Get("No homunculus.")); return CommandResult.Okay; }
			sender.ServerMessage("{0} (skill points: {1})", target.Homunculus.GetType().Name, target.Homunculus.SkillPoints);
			return CommandResult.Okay;
		}

		private CommandResult HomEvolve(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplHomun(sender, "homevolve");
		private CommandResult MakeHomun(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplHomun(sender, "makehomun");
		private CommandResult HomFriendly(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplHomun(sender, "homfriendly");
		private CommandResult HomHungry(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplHomun(sender, "homhungry");
		private CommandResult HomTalk(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplHomun(sender, "homtalk");
		private CommandResult HomStats(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplHomun(sender, "homstats");
		private CommandResult HomShuffle(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplHomun(sender, "homshuffle");
		private CommandResult HomMutate(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.NotImplHomun(sender, "hommutate");

		private CommandResult HomunLearn(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (target.Homunculus == null || !target.Homunculus.HasContract)
			{
				sender.ServerMessage(Localization.Get("Target has no homunculus."));
				return CommandResult.Okay;
			}

			Sabine.Shared.Const.SkillId skillId;
			var arg0 = args.Get(0);
			if (System.Enum.TryParse<Sabine.Shared.Const.SkillId>(arg0, out var parsed)) skillId = parsed;
			else if (int.TryParse(arg0, out var num)) skillId = (Sabine.Shared.Const.SkillId)num;
			else { sender.ServerMessage(Localization.Get("Invalid skill id '{0}'."), arg0); return CommandResult.Okay; }

			var newLv = target.Homunculus.TryLearn(skillId);
			if (newLv == 0)
			{
				sender.ServerMessage(Localization.Get("Cannot learn '{0}' (no points, missing prereq, or wrong type)."), skillId);
				return CommandResult.Okay;
			}

			sender.ServerMessage(Localization.Get("{0} → level {1}. Skill points remaining: {2}."), skillId, newLv, target.Homunculus.SkillPoints);
			return CommandResult.Okay;
		}
	}
}
