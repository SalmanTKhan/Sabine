using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sabine.Shared.Util;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	/// <summary>
	/// The chat command manager. Holds and executes chat commands.
	/// </summary>
	/// <remarks>
	/// Handlers live in ChatCommands.&lt;module&gt;.cs partials, each
	/// contributing a Load&lt;module&gt;() method that <see cref="Load"/>
	/// invokes here. Add new modules by adding a Load call below.
	/// </remarks>
	public partial class ChatCommands : CommandManager<ChatCommand, CommandFunc>
	{
		public void Load()
		{
			this.Clear();

			// Help is the dispatcher itself; keep it registered first so
			// it appears at the top of the >commands listing.
			this.Add("help", "[commandName]", Localization.Get("Displays a list of usable commands or details about one command."), this.Help);

			// Modules contributed via partial-class files.
			this.LoadServer();
			this.LoadEffects();
			this.LoadModeration();
			this.LoadMovement();
			this.LoadInfo();
			this.LoadGuild();
			this.LoadCharacter();
			this.LoadPlayer();
			this.LoadSpawn();
			this.LoadSkill();
			this.LoadPet();
			this.LoadHomun();
			this.LoadParty();
			this.LoadNpc();
			this.LoadQuest();
			this.LoadSocial();
			this.LoadMisc();
			this.LoadDebug();
		}

		/// <summary>
		/// Displays a list of usable commands or details about one command.
		/// </summary>
		private CommandResult Help(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var targetAuthLevel = target.Connection.Account.Authority;

			// Display info about one command
			if (args.Count != 0)
			{
				var helpCommandName = args.Get(0);
				var command = this.GetCommand(helpCommandName);

				if (command == null)
				{
					sender.ServerMessage(Localization.Get("Command not found or not available."));
					return CommandResult.Okay;
				}

				var levels = ZoneServer.Instance.Conf.Commands.GetLevels(command.Name) ?? ZoneServer.Instance.Conf.Commands.GetLevels("default");

				if (levels.Self > targetAuthLevel)
				{
					sender.ServerMessage(Localization.Get("Command not found or not available."));
					return CommandResult.Okay;
				}

				var aliases = _commands.Where(a => a.Value == command && a.Key != helpCommandName).Select(a => a.Key);

				sender.ServerMessage(Localization.Get("Name: {0}"), command.Name);
				if (aliases.Any())
					sender.ServerMessage(Localization.Get("Aliases: {0}"), string.Join(", ", aliases));
				sender.ServerMessage(Localization.Get("Description: {0}"), command.Description);
				sender.ServerMessage(Localization.Get("Arguments: {0}"), command.Usage);

				return CommandResult.Okay;
			}

			// Display list of available commands
			var commandNames = new List<string>();

			foreach (var command in _commands.Values.Distinct())
			{
				var levels = ZoneServer.Instance.Conf.Commands.GetLevels(command.Name) ?? ZoneServer.Instance.Conf.Commands.GetLevels("default");
				if (levels == null || levels.Self > targetAuthLevel)
					continue;

				commandNames.Add(command.Name);
			}

			if (commandNames.Count == 0)
			{
				sender.ServerMessage(Localization.Get("No commands found."));
				return CommandResult.Okay;
			}

			var sb = new StringBuilder();

			sender.ServerMessage(Localization.Get("Available commands:"));
			foreach (var name in commandNames)
			{
				// Group command names in strings up to 100 characters,
				// since some clients won't display longer messages.
				if (sb.Length + 2 + name.Length >= 100)
				{
					sender.ServerMessage(sb.ToString());
					sb.Clear();
				}

				if (sb.Length != 0)
					sb.Append(", ");

				sb.Append(name);
			}

			if (sb.Length != 0)
				sender.ServerMessage(sb.ToString());

			return CommandResult.Okay;
		}
	}
}
