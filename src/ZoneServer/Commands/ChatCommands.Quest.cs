using Sabine.Shared.Util;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadQuest()
		{
			this.Add("setquest", "<id>", Localization.Get("Starts a quest for the target."), this.SetQuest);
			this.Add("erasequest", "<id>", Localization.Get("Removes a quest from the target."), this.EraseQuest);
			this.Add("completequest", "<id>", Localization.Get("Completes a quest for the target."), this.CompleteQuest);
			this.Add("checkquest", "<id>", Localization.Get("Reports the target's quest progress."), this.CheckQuest);
			this.Add("addfame", "<amount>", Localization.Get("Adds fame points to the target."), this.AddFame);
		}

		private CommandResult SetQuest(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var questId)) return CommandResult.InvalidArgument;
			target.Quests.Add(questId);
			sender.ServerMessage(Localization.Get("Quest {0} marked as started."), questId);
			return CommandResult.Okay;
		}

		private CommandResult EraseQuest(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var questId)) return CommandResult.InvalidArgument;
			target.Quests.Erase(questId);
			sender.ServerMessage(Localization.Get("Quest {0} erased."), questId);
			return CommandResult.Okay;
		}

		private CommandResult CompleteQuest(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var questId)) return CommandResult.InvalidArgument;
			target.Quests.Complete(questId);
			sender.ServerMessage(Localization.Get("Quest {0} marked complete."), questId);
			return CommandResult.Okay;
		}

		private CommandResult CheckQuest(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var questId)) return CommandResult.InvalidArgument;
			var state = target.Quests.GetStatus(questId);
			var accepted = target.Quests.GetAcceptedAt(questId);
			var c0 = target.Quests.GetCount(questId, 0);
			var c1 = target.Quests.GetCount(questId, 1);
			var c2 = target.Quests.GetCount(questId, 2);
			sender.ServerMessage(
				Localization.Get("Quest {0}: state={1}, accepted={2}, counts={3}/{4}/{5}"),
				questId, state, accepted == default ? "—" : accepted.ToString("u"), c0, c1, c2);
			return CommandResult.Okay;
		}

		private CommandResult AddFame(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var amount)) return CommandResult.InvalidArgument;
			target.Vars.Perm.SetInt("Sabine.Fame", target.Vars.Perm.GetInt("Sabine.Fame", 0) + amount);
			sender.ServerMessage(Localization.Get("Fame +{0}."), amount);
			return CommandResult.Okay;
		}
	}
}
