using Sabine.Shared.Util;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadMercenary()
		{
			this.Add("mercsummon", "<mobId>", Localization.Get("Hires a mercenary by mob id."), this.MercSummon);
			this.Add("mercdismiss", "", Localization.Get("Dismisses the active mercenary."), this.MercDismiss);
			this.Add("mercresummon", "", Localization.Get("Re-summons the mercenary from a paused contract."), this.MercResummon);
			this.Add("mercfaith", "<faction> [delta]", Localization.Get("Reports or adjusts faith with a faction."), this.MercFaith);
			this.Add("mercinfo", "", Localization.Get("Reports the active contract state."), this.MercInfo);
		}

		/// <summary>
		/// /mercsummon &lt;mobId&gt; — hires a mercenary by mob id, GM
		/// shortcut for the contract-card double-click flow. Calls
		/// MercenaryService.Hire which validates the contract row.
		/// </summary>
		private CommandResult MercSummon(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var mobId)) return CommandResult.InvalidArgument;

			if (!Sabine.Zone.Skills.Mercenaries.MercenaryService.Hire(target, mobId))
			{
				sender.ServerMessage(Localization.Get("Hire refused — already contracted, no map, or unknown mob id {0}."), mobId);
				return CommandResult.Okay;
			}

			sender.ServerMessage(Localization.Get("{0} hired mercenary {1}."), target.Name, mobId);
			return CommandResult.Okay;
		}

		/// <summary>
		/// /mercresummon — brings the merc back after a re-login, warp,
		/// or owner death paused the contract. Reads the existing
		/// contract from the component and spawns the entity beside
		/// the owner. Refuses when there's no contract or the contract
		/// is already active with a live entity. Mirrors eAthena's
		/// auto-resume on map enter — Sabine doesn't auto-spawn so the
		/// player has explicit control over when the rental clock
		/// runs alongside their session.
		/// </summary>
		private CommandResult MercResummon(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.Mercenary == null || !target.Mercenary.HasContract)
			{
				sender.ServerMessage(Localization.Get("No paused contract to re-summon."));
				return CommandResult.Okay;
			}

			if (target.Mercenary.IsActive && target.Mercenary.Entity != null)
			{
				sender.ServerMessage(Localization.Get("Mercenary already summoned."));
				return CommandResult.Okay;
			}

			// Force the state back to Active so Spawn doesn't refuse,
			// then place the entity. The component's heartbeat will
			// resume ticking the contract clock from the next frame.
			target.Mercenary.StartContractResume();
			if (Sabine.Zone.Skills.Mercenaries.MercenaryService.Spawn(target))
				sender.ServerMessage(Localization.Get("Mercenary re-summoned."));
			else
				sender.ServerMessage(Localization.Get("Re-summon failed."));
			return CommandResult.Okay;
		}

		private CommandResult MercDismiss(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (target.Mercenary == null || !target.Mercenary.HasContract)
			{
				sender.ServerMessage(Localization.Get("No active contract."));
				return CommandResult.Okay;
			}

			Sabine.Zone.Skills.Mercenaries.MercenaryService.Dismiss(target);
			sender.ServerMessage(Localization.Get("Mercenary dismissed."));
			return CommandResult.Okay;
		}

		/// <summary>
		/// /mercfaith &lt;faction&gt; [delta] — without delta, reports
		/// the player's current faith with the named faction. With
		/// delta, adjusts (positive or negative). Faction names match
		/// the MercenaryFaction enum: Archer / Sword / Spearman /
		/// Lancer.
		/// </summary>
		private CommandResult MercFaith(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!System.Enum.TryParse<Sabine.Shared.Data.Databases.MercenaryFaction>(args.Get(0), true, out var faction))
				return CommandResult.InvalidArgument;

			if (args.Count >= 2 && int.TryParse(args.Get(1), out var delta))
				target.Mercenary.AddFaith(faction, delta);

			sender.ServerMessage(Localization.Get("Faith with {0}: {1}"), faction, target.Mercenary.GetFaith(faction));
			return CommandResult.Okay;
		}

		private CommandResult MercInfo(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var m = target.Mercenary;
			if (m == null || !m.HasContract)
			{
				sender.ServerMessage(Localization.Get("No active contract."));
				return CommandResult.Okay;
			}

			sender.ServerMessage(Localization.Get("Mercenary {0} ({1}), {2}s left, state {3}."),
				m.MobId, m.Faction, m.ContractSecondsRemaining, m.State);
			return CommandResult.Okay;
		}
	}
}
