using System;
using Sabine.Shared.Util;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.World;
using Sabine.Zone.World.Actors;
using Yggdrasil.Collections;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadModeration()
		{
			this.Add("kick", "[reason]", Localization.Get("Disconnects the target."), this.Kick);
			this.Add("kickall", "", Localization.Get("Disconnects everyone but the sender."), this.KickAll);
			this.Add("recall", "", Localization.Get("Warps target to sender."), this.Recall);
			this.Add("recallall", "", Localization.Get("Warps every online player to sender."), this.RecallAll);

			this.Add("jail", "", Localization.Get("Jails the target."), this.Jail);
			this.Add("unjail", "", Localization.Get("Releases the target from jail."), this.Unjail);
			this.Add("jailfor", "<minutes>", Localization.Get("Jails the target for a duration."), this.JailFor);
			this.Add("jailtime", "", Localization.Get("Shows time remaining on the target's jail sentence."), this.JailTime);

			this.Add("mute", "<minutes>", Localization.Get("Mutes the target for a duration."), this.Mute);
			this.Add("unmute", "", Localization.Get("Unmutes the target."), this.Unmute);
			this.Add("mutearea", "<minutes>", Localization.Get("Mutes everyone in the visible area."), this.MuteArea);

			this.Add("doom", "", Localization.Get("Kills every player on the same map."), this.Doom);
			this.Add("doommap", "", Localization.Get("Kills every player on the target's map."), this.Doom);
			this.Add("raise", "", Localization.Get("Resurrects every dead player on the same map."), this.Raise);
			this.Add("raisemap", "", Localization.Get("Resurrects every dead player on the target's map."), this.Raise);
			this.Add("nuke", "", Localization.Get("Detonates an explosion on the target."), this.Nuke);

			this.Add("ban", "", Localization.Get("Bans the target's account."), this.Ban);
			this.Add("unban", "", Localization.Get("Unbans the target's account."), this.Unban);
			this.Add("charban", "", Localization.Get("Bans the target character."), this.Ban);
			this.Add("charunban", "", Localization.Get("Unbans the target character."), this.Unban);
			this.Add("block", "", Localization.Get("Permanently blocks the target's account."), this.Ban);
			this.Add("unblock", "", Localization.Get("Unblocks the target's account."), this.Unban);
			this.Add("charblock", "", Localization.Get("Permanently blocks the target character."), this.Ban);
			this.Add("charunblock", "", Localization.Get("Unblocks the target character."), this.Unban);

			this.Add("ksprotection", "", Localization.Get("Toggles KS-protection on the target."), this.KsProtection);
			this.Add("allowks", "", Localization.Get("Allows others to kill-steal from the target."), this.AllowKs);
			this.Add("macrochecker", "", Localization.Get("Triggers a macro-detection prompt for the target."), this.MacroChecker);
		}

		private CommandResult Kick(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var reason = args.Count > 0 ? string.Join(" ", args.GetAll()) : "kicked";
			target.ServerMessage(Localization.Get("You have been kicked: {0}"), reason);
			try { target.Connection.Close(); } catch { }
			sender.ServerMessage(Localization.Get("Kicked {0}."), target.Name);
			return CommandResult.Okay;
		}

		private CommandResult KickAll(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			using var players = PooledList<PlayerCharacter>.Rent();
			ZoneServer.Instance.World.Maps.Do(m => m.GetPlayers(players, sender, static (s, p) => p != s));
			var count = 0;
			foreach (var p in players)
			{
				try { p.Connection.Close(); count++; } catch { }
			}
			sender.ServerMessage(Localization.Get("Kicked {0} player(s)."), count);
			return CommandResult.Okay;
		}

		private CommandResult Recall(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (sender == target)
			{
				sender.ServerMessage(Localization.Get("Recall needs a target via >>recall <name>."));
				return CommandResult.Okay;
			}

			target.Warp(sender.MapId, sender.Position);
			sender.ServerMessage(Localization.Get("Recalled {0}."), target.Name);
			target.ServerMessage(Localization.Get("You were recalled by {0}."), sender.Name);
			return CommandResult.Okay;
		}

		private CommandResult RecallAll(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			using var players = PooledList<PlayerCharacter>.Rent();
			ZoneServer.Instance.World.Maps.Do(m => m.GetPlayers(players, sender, static (s, p) => p != s));
			foreach (var p in players)
				p.Warp(sender.MapId, sender.Position);
			sender.ServerMessage(Localization.Get("Recalled {0} player(s)."), players.Count);
			return CommandResult.Okay;
		}

		private CommandResult Jail(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
			=> this.JailInternal(sender, target, TimeSpan.FromHours(24));

		private CommandResult JailFor(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var minutes)) return CommandResult.InvalidArgument;
			return this.JailInternal(sender, target, TimeSpan.FromMinutes(Math.Max(1, minutes)));
		}

		private CommandResult JailInternal(PlayerCharacter sender, PlayerCharacter target, TimeSpan duration)
		{
			var jailMap = JailService.Instance.DefaultJailMap;
			var pos = new Position(60, 60);
			var mapId = target.MapId;

			if (ZoneServer.Instance.World.Maps.TryGetByStringId(jailMap, out var map))
				mapId = map.Id;

			JailService.Instance.Jail(target.Connection.Account.Id, mapId, pos, duration);
			target.Warp(mapId, pos);
			target.ServerMessage(Localization.Get("You have been jailed for {0} minutes."), (int)duration.TotalMinutes);
			sender.ServerMessage(Localization.Get("Jailed {0} for {1} minutes."), target.Name, (int)duration.TotalMinutes);
			return CommandResult.Okay;
		}

		private CommandResult Unjail(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var released = JailService.Instance.Release(target.Connection.Account.Id);
			sender.ServerMessage(released ? Localization.Get("Released {0}.") : Localization.Get("{0} is not jailed."), target.Name);
			if (released) target.ServerMessage(Localization.Get("You have been released from jail."));
			return CommandResult.Okay;
		}

		private CommandResult JailTime(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var remaining = JailService.Instance.RemainingTime(target.Connection.Account.Id);
			if (remaining == null) sender.ServerMessage(Localization.Get("{0} is not jailed."), target.Name);
			else sender.ServerMessage(Localization.Get("{0} has {1} minutes left in jail."), target.Name, (int)remaining.Value.TotalMinutes);
			return CommandResult.Okay;
		}

		private CommandResult Mute(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var minutes = 5;
			if (args.Count >= 1 && !int.TryParse(args.Get(0), out minutes))
				return CommandResult.InvalidArgument;

			target.MutedUntil = DateTime.UtcNow.AddMinutes(Math.Max(1, minutes));
			sender.ServerMessage(Localization.Get("Muted {0} for {1} minutes."), target.Name, minutes);
			target.ServerMessage(Localization.Get("You have been muted for {0} minutes."), minutes);
			return CommandResult.Okay;
		}

		private CommandResult Unmute(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.MutedUntil = null;
			sender.ServerMessage(Localization.Get("Unmuted {0}."), target.Name);
			target.ServerMessage(Localization.Get("You have been unmuted."));
			return CommandResult.Okay;
		}

		private CommandResult MuteArea(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var minutes = 5;
			if (args.Count >= 1 && !int.TryParse(args.Get(0), out minutes))
				return CommandResult.InvalidArgument;

			using var players = PooledList<PlayerCharacter>.Rent();
			sender.Map.GetPlayers(players, sender, static (s, p) => p != s && p.Position.InRange(s.Position, s.Map.VisibleRange));
			var until = DateTime.UtcNow.AddMinutes(Math.Max(1, minutes));
			foreach (var p in players) p.MutedUntil = until;
			sender.ServerMessage(Localization.Get("Muted {0} nearby player(s)."), players.Count);
			return CommandResult.Okay;
		}

		private CommandResult Doom(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			using var players = PooledList<PlayerCharacter>.Rent();
			target.Map.GetPlayers(players, sender, static (s, p) => p != s);
			foreach (var p in players)
			{
				try { p.Kill(sender); } catch { }
			}
			sender.ServerMessage(Localization.Get("Doom dealt to {0}."), players.Count);
			return CommandResult.Okay;
		}

		private CommandResult Raise(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			using var players = PooledList<PlayerCharacter>.Rent();
			target.Map.GetPlayers(players, 0, static (_, p) => p.IsDead);
			foreach (var p in players)
			{
				try { p.Heal(); } catch { }
			}
			sender.ServerMessage(Localization.Get("Raised {0} player(s)."), players.Count);
			return CommandResult.Okay;
		}

		private CommandResult Nuke(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			Send.ZC_NOTIFY_EFFECT(target, 70);
			try { target.Kill(sender); } catch { }
			sender.ServerMessage(Localization.Get("Nuked {0}."), target.Name);
			return CommandResult.Okay;
		}

		private CommandResult Ban(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Vars.Perm.SetBool("Sabine.Banned", true);
			target.ServerMessage(Localization.Get("Your account has been banned."));
			try { target.Connection.Close(); } catch { }
			sender.ServerMessage(Localization.Get("Banned {0}."), target.Name);
			return CommandResult.Okay;
		}

		private CommandResult Unban(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Vars.Perm.SetBool("Sabine.Banned", false);
			sender.ServerMessage(Localization.Get("Unbanned {0}."), target.Name);
			return CommandResult.Okay;
		}

		private CommandResult KsProtection(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var v = target.Vars.Temp.ToggleBool("Sabine.KsProtection");
			sender.ServerMessage(Localization.Get("KS protection on {0}: {1}"), target.Name, v);
			return CommandResult.Okay;
		}

		private CommandResult AllowKs(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var v = target.Vars.Temp.ToggleBool("Sabine.AllowKs");
			sender.ServerMessage(Localization.Get("Allow KS on {0}: {1}"), target.Name, v);
			return CommandResult.Okay;
		}

		private CommandResult MacroChecker(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.ServerMessage(Localization.Get("[Macro check] Please type the captcha (not implemented)."));
			sender.ServerMessage(Localization.Get("Macro check sent to {0}."), target.Name);
			return CommandResult.Okay;
		}
	}
}
