using System;
using Sabine.Shared.Const;
using Sabine.Shared.Util;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadCharacter()
		{
			this.Add("sprite", "<Class|Hair> <value>", Localization.Get("Changes the target's sprite."), this.Sprite);
			this.Add("job", "<job>", Localization.Get("Changes character's job."), this.Job);

			this.Add("hide", "", Localization.Get("Toggles invisibility on the target."), this.Hide);
			this.Add("fakename", "<name>", Localization.Get("Displays a fake name for the target."), this.FakeName);
			this.Add("size", "<0|1|2>", Localization.Get("Sets the target's display size."), this.Size);
			this.Add("hairstyle", "<id>", Localization.Get("Changes the target's hairstyle."), this.HairStyle);
			this.Add("haircolor", "<id>", Localization.Get("Changes the target's hair color."), this.HairColor);
			this.Add("dye", "<id>", Localization.Get("Changes the target's clothing dye."), this.Dye);
			this.Add("model", "<hair> <hairColor> <clothingColor>", Localization.Get("Changes the target's full appearance."), this.Model);
			this.Add("option", "<flags>", Localization.Get("Sets the target's option flags."), this.Option);
			this.Add("changelook", "<part> <id>", Localization.Get("Changes a single appearance slot."), this.ChangeLook);
			this.Add("bodystyle", "<id>", Localization.Get("Changes the target's body style."), this.BodyStyle);
			this.Add("costume", "<id>", Localization.Get("Toggles a costume on the target."), this.BodyStyle);
			this.Add("mount", "", Localization.Get("Toggles a mount on the target."), this.Mount);
			this.Add("mount2", "", Localization.Get("Toggles the alternate mount on the target."), this.Mount);
			this.Add("mountpeco", "", Localization.Get("Toggles a peco mount on the target."), this.Mount);
			this.Add("jobchange", "<job>", Localization.Get("Changes the target's job."), this.Job);

			this.AddAlias("dye", "ccolor");
			this.AddAlias("hairstyle", "hstyle");
			this.AddAlias("haircolor", "hcolor");
		}

		private CommandResult Hide(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var v = target.Vars.Temp.ToggleBool("Sabine.Hidden");
			sender.ServerMessage(Localization.Get("Hide on {0}: {1}"), target.Name, v);
			return CommandResult.Okay;
		}

		private CommandResult FakeName(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var name = args.Count > 0 ? args.Get(0) : null;
			if (name == null) target.Vars.Temp.SetString("Sabine.FakeName", null);
			else target.Vars.Temp.SetString("Sabine.FakeName", name);
			sender.ServerMessage(Localization.Get("Fake name set to '{0}'."), name ?? "(cleared)");
			return CommandResult.Okay;
		}

		private CommandResult Size(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var size)) return CommandResult.InvalidArgument;
			target.Vars.Temp.SetInt("Sabine.Size", size);
			sender.ServerMessage(Localization.Get("Size set to {0}."), size);
			return CommandResult.Okay;
		}

		private CommandResult HairStyle(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
			=> this.SpriteShortcut(sender, target, args, SpriteType.Hair);

		private CommandResult HairColor(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
			=> this.SpriteShortcut(sender, target, args, SpriteType.HairColor);

		private CommandResult Dye(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
			=> this.SpriteShortcut(sender, target, args, SpriteType.ClothesColor);

		private CommandResult Model(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 3) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var hair)) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(1), out var hairColor)) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(2), out var clothes)) return CommandResult.InvalidArgument;
			Send.ZC_SPRITE_CHANGE(target, SpriteType.Hair, hair);
			Send.ZC_SPRITE_CHANGE(target, SpriteType.HairColor, hairColor);
			Send.ZC_SPRITE_CHANGE(target, SpriteType.ClothesColor, clothes);
			sender.ServerMessage(Localization.Get("Model applied."));
			return CommandResult.Okay;
		}

		private CommandResult SpriteShortcut(PlayerCharacter sender, PlayerCharacter target, Arguments args, SpriteType type)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var v)) return CommandResult.InvalidArgument;
			Send.ZC_SPRITE_CHANGE(target, type, v);
			sender.ServerMessage(Localization.Get("Set {0} to {1}."), type, v);
			return CommandResult.Okay;
		}

		private CommandResult Option(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var v)) return CommandResult.InvalidArgument;
			target.Vars.Temp.SetInt("Sabine.Option", v);
			sender.ServerMessage(Localization.Get("Option flags set to {0}."), v);
			return CommandResult.Okay;
		}

		private CommandResult ChangeLook(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 2) return CommandResult.InvalidArgument;
			if (!Enum.TryParse<SpriteType>(args.Get(0), true, out var type)) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(1), out var v)) return CommandResult.InvalidArgument;
			Send.ZC_SPRITE_CHANGE(target, type, v);
			sender.ServerMessage(Localization.Get("Changed {0} to {1}."), type, v);
			return CommandResult.Okay;
		}

		private CommandResult BodyStyle(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var v)) return CommandResult.InvalidArgument;
			target.BodyStyle = v;
			sender.ServerMessage(Localization.Get("Body style set to {0} (alpha client may not display this)."), v);
			return CommandResult.Okay;
		}

		private CommandResult Mount(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var v = target.Vars.Temp.ToggleBool("Sabine.Mounted");
			sender.ServerMessage(Localization.Get("Mount: {0}"), v);
			return CommandResult.Okay;
		}

		private CommandResult Sprite(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 2) return CommandResult.InvalidArgument;
			if (!Enum.TryParse<SpriteType>(args.Get(0), out var type)) return CommandResult.InvalidArgument;

			var value1 = 0;
			var value2 = 0;
			if (args.Count >= 2)
			{
				if (!int.TryParse(args.Get(1), out value1)) return CommandResult.InvalidArgument;
			}
			if (args.Count >= 3)
			{
				if (!int.TryParse(args.Get(2), out value2)) return CommandResult.InvalidArgument;
			}

			if (Sabine.Shared.Game.Version < Sabine.Shared.Versions.S500)
				Send.ZC_SPRITE_CHANGE(target, type, value1);
			else
				Send.ZC_SPRITE_CHANGE2(target, type, value1, value2);

			sender.ServerMessage(Localization.Get("Changed {0} to {1}/{2}."), type, value1, value2);
			return CommandResult.Okay;
		}

		private CommandResult Job(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count == 0) return CommandResult.InvalidArgument;
			if (!Enum.TryParse<JobId>(args.Get(0), out var jobId))
			{
				sender.ServerMessage(Localization.Get("Unknown job '{0}'."), args.Get(0));
				return CommandResult.Okay;
			}
			target.ChangeJob(jobId);
			sender.ServerMessage(Localization.Get("Job changed to {0}."), jobId);
			if (target != sender)
				target.ServerMessage(Localization.Get("{0} changed your job to {1}."), sender.Name, jobId);
			return CommandResult.Okay;
		}
	}
}
