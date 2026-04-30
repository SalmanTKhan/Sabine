using Sabine.Shared.Util;
using Sabine.Zone.Network;
using Sabine.Zone.World;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadEffects()
		{
			this.Add("snow", "", Localization.Get("Toggles snow weather effect server-wide."), this.Snow);
			this.Add("sakura", "", Localization.Get("Toggles sakura petal weather effect."), this.Sakura);
			this.Add("clouds", "", Localization.Get("Toggles cloud weather effect."), this.Clouds);
			this.Add("clouds2", "", Localization.Get("Toggles alternate cloud weather effect."), this.Clouds2);
			this.Add("fog", "", Localization.Get("Toggles fog weather effect."), this.Fog);
			this.Add("fireworks", "", Localization.Get("Toggles fireworks effect."), this.Fireworks);
			this.Add("leaves", "", Localization.Get("Toggles falling leaves weather effect."), this.Leaves);
			this.Add("clearweather", "", Localization.Get("Clears all weather effects."), this.ClearWeather);
			this.Add("day", "", Localization.Get("Sets the world to day."), this.Day);
			this.Add("night", "", Localization.Get("Sets the world to night."), this.Night);
			this.Add("effect", "<id>", Localization.Get("Plays a visual effect on the target."), this.Effect);
			this.Add("misceffect", "<id>", Localization.Get("Plays a visual effect on the target."), this.Effect);
			this.Add("sound", "<file>", Localization.Get("Plays a sound for the target."), this.Sound);
		}

		private CommandResult ToggleWeather(PlayerCharacter sender, WeatherKind kind, string label)
		{
			var svc = WeatherService.Instance;
			svc.Current = svc.Current == kind ? WeatherKind.None : kind;
			sender.ServerMessage(Localization.Get("Weather: {0}"), svc.Current);
			Send.ZC_BROADCAST(string.Format("[Weather] {0}", label));
			return CommandResult.Okay;
		}

		private CommandResult Snow(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ToggleWeather(sender, WeatherKind.Snow, "Snow");
		private CommandResult Sakura(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ToggleWeather(sender, WeatherKind.Sakura, "Sakura");
		private CommandResult Clouds(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ToggleWeather(sender, WeatherKind.Clouds, "Clouds");
		private CommandResult Clouds2(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ToggleWeather(sender, WeatherKind.Clouds2, "Clouds2");
		private CommandResult Fog(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ToggleWeather(sender, WeatherKind.Fog, "Fog");
		private CommandResult Fireworks(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ToggleWeather(sender, WeatherKind.Fireworks, "Fireworks");
		private CommandResult Leaves(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args) => this.ToggleWeather(sender, WeatherKind.Leaves, "Leaves");

		private CommandResult ClearWeather(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			WeatherService.Instance.Current = WeatherKind.None;
			sender.ServerMessage(Localization.Get("Weather cleared."));
			Send.ZC_BROADCAST("[Weather] cleared");
			return CommandResult.Okay;
		}

		private CommandResult Day(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			WeatherService.Instance.IsNight = false;
			sender.ServerMessage(Localization.Get("Day."));
			Send.ZC_BROADCAST("[World] Day breaks.");
			return CommandResult.Okay;
		}

		private CommandResult Night(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			WeatherService.Instance.IsNight = true;
			sender.ServerMessage(Localization.Get("Night."));
			Send.ZC_BROADCAST("[World] Night falls.");
			return CommandResult.Okay;
		}

		private CommandResult Effect(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var id)) return CommandResult.InvalidArgument;
			Send.ZC_NOTIFY_EFFECT(target, id);
			return CommandResult.Okay;
		}

		private CommandResult Sound(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			sender.ServerMessage(Localization.Get("Sound playback is not implemented (file: {0})."), args.Get(0));
			return CommandResult.Okay;
		}
	}
}
