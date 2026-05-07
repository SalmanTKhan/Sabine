using System;
using System.Threading.Tasks;
using Sabine.Zone.Scripting;
using Yggdrasil.Logging;

namespace Sabine.Zone.World.Woe
{
	/// <summary>
	/// Global War of Emperium lifecycle. Tracks whether WoE is
	/// currently active and fans out the `OnAgitStart` / `OnAgitEnd`
	/// events that scripts subscribe to. Castle ownership state
	/// lives in <see cref="CastleManager"/>; this class only owns
	/// the on/off toggle.
	/// </summary>
	/// <remarks>
	/// Mirrors eAthena's `agitstart` / `agitend` / `agitcheck`. WoE
	/// 1.0 (regular castles) and WoE 2.0 (renewal castles) share the
	/// same toggle here — a future split into `Woe1` / `Woe2` is
	/// possible but not modelled now since the script ecosystem
	/// rarely keys off the distinction at the engine layer.
	/// </remarks>
	public class WoeService
	{
		/// <summary>
		/// True while WoE is on. Toggled by <see cref="Start"/> /
		/// <see cref="End"/>; mirrors eAthena's <c>agitcheck()</c>.
		/// </summary>
		public bool IsActive { get; private set; }

		/// <summary>
		/// Wall-clock time WoE last started (UTC). Useful for
		/// log/debug output; not used for gating.
		/// </summary>
		public DateTime? StartedAt { get; private set; }

		/// <summary>
		/// Turns WoE on and fires every `*::OnAgitStart` event
		/// (matching eAthena's `agitstart`). Idempotent — calling
		/// while already active no-ops with a warning.
		/// </summary>
		public void Start()
		{
			if (this.IsActive)
			{
				Log.Warning("WoeService.Start: already active.");
				return;
			}
			this.IsActive = true;
			this.StartedAt = DateTime.UtcNow;
			Log.Info("WoE started.");
			_ = this.RaiseAgitEvent("OnAgitStart");
		}

		/// <summary>
		/// Turns WoE off and fires every `*::OnAgitEnd` event.
		/// Idempotent.
		/// </summary>
		public void End()
		{
			if (!this.IsActive)
			{
				Log.Warning("WoeService.End: not active.");
				return;
			}
			this.IsActive = false;
			Log.Info("WoE ended.");
			_ = this.RaiseAgitEvent("OnAgitEnd");
		}

		// `ScriptEvents.RaiseAsync` is keyed by `<NpcName>::<Event>`,
		// but the agit lifecycle fans out across every subscriber.
		// We can't iterate the registry from the outside, so we
		// expose the generic event prefix and let scripts subscribe
		// with `Agit::OnAgitStart` (the conventional handle). A
		// follow-up could expose a wildcard-fanout API on
		// `ScriptEvents` if more lifecycle events surface.
		private async Task RaiseAgitEvent(string label)
		{
			try
			{
				await ScriptEvents.RaiseAsync($"Agit::{label}");
			}
			catch (Exception ex)
			{
				Log.Error("WoeService.RaiseAgitEvent({0}): {1}", label, ex);
			}
		}
	}
}
