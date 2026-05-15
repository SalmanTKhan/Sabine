using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sabine.Zone.Network;
using Sabine.Zone.Scripting;
using Yggdrasil.Configuration;
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
		private static readonly TimeSpan SchedulePollInterval = TimeSpan.FromSeconds(30);
		private Timer _scheduleTimer;
		private readonly object _scheduleLock = new();
		private List<DayOfWeek> _scheduleDays;
		private TimeSpan _scheduleStart;
		private TimeSpan _scheduleDuration;
		private DateTime? _autoEndAt;
		private DateTime _lastAutoStartAt = DateTime.MinValue;

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

		/// <summary>Raised after <see cref="Start"/> flips state on.</summary>
		public event Action Started;

		/// <summary>Raised after <see cref="End"/> flips state off.</summary>
		public event Action Ended;

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
			try { this.Started?.Invoke(); }
			catch (Exception ex) { Log.Error("WoeService.Started handler: {0}", ex); }
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
			try { this.Ended?.Invoke(); }
			catch (Exception ex) { Log.Error("WoeService.Ended handler: {0}", ex); }
			_ = this.RaiseAgitEvent("OnAgitEnd");
		}

		/// <summary>
		/// Castle-broken hook fired from <c>Monster.Kill</c> when an
		/// Emperium dies. Transfers ownership to the breaking guild
		/// and fans out the per-castle `OnGuildBreak` event so the
		/// converted scripts can run their bookkeeping (banner swap,
		/// kafra reset, treasure-room teardown).
		/// </summary>
		public void OnEmperiumBroken(Castle castle, int newGuildId)
		{
			if (castle == null)
				return;

			var manager = ZoneServer.Instance.World.Castles;
			manager.TransferOwner(castle, newGuildId);

			Send.ZC_BROADCAST(string.Format(
				"[Server] {0} has been claimed by a new guild!",
				castle.Name ?? castle.MapStringId));

			var hookEvent = castle.Template?.OnGuildBreakEvent;
			if (!string.IsNullOrEmpty(hookEvent))
			{
				_ = this.SafeRaise(hookEvent);
			}
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

		private async Task SafeRaise(string eventName)
		{
			try
			{
				await ScriptEvents.RaiseAsync(eventName);
			}
			catch (Exception ex)
			{
				Log.Error("WoeService.SafeRaise({0}): {1}", eventName, ex);
			}
		}

		/// <summary>
		/// Reads the WoE schedule from <c>system/conf/world/woe.conf</c>
		/// (falls back to defaults if the file is absent) and starts
		/// the polling timer. Idempotent.
		/// </summary>
		public void StartSchedule()
		{
			lock (_scheduleLock)
			{
				if (_scheduleTimer != null)
					return;

				this.LoadScheduleConfig();
				_scheduleTimer = new Timer(this.ScheduleTick, null,
					TimeSpan.Zero, SchedulePollInterval);
				Log.Info("WoeService: schedule timer started ({0} day(s), {1} for {2} min).",
					_scheduleDays?.Count ?? 0, _scheduleStart, _scheduleDuration.TotalMinutes);
			}
		}

		/// <summary>
		/// Stops the schedule timer. Used in tests / shutdown paths.
		/// </summary>
		public void StopSchedule()
		{
			lock (_scheduleLock)
			{
				_scheduleTimer?.Dispose();
				_scheduleTimer = null;
			}
		}

		private void LoadScheduleConfig()
		{
			_scheduleDays = new List<DayOfWeek>();
			_scheduleStart = TimeSpan.FromHours(20);
			_scheduleDuration = TimeSpan.FromMinutes(120);

			var path = Path.Combine("system", "conf", "world", "woe.conf");
			if (!File.Exists(path))
			{
				Log.Info("WoeService: no woe.conf found at '{0}'; schedule disabled.", path);
				return;
			}

			try
			{
				var conf = new WoeConfFile();
				conf.Load(path);
				_scheduleDays = ParseDays(conf.Days);
				_scheduleStart = ParseTime(conf.StartTime, _scheduleStart);
				_scheduleDuration = TimeSpan.FromMinutes(Math.Max(1, conf.DurationMinutes));
			}
			catch (Exception ex)
			{
				Log.Error("WoeService.LoadScheduleConfig: failed to read '{0}': {1}", path, ex);
			}
		}

		private static List<DayOfWeek> ParseDays(string raw)
		{
			var result = new List<DayOfWeek>();
			if (string.IsNullOrEmpty(raw))
				return result;
			foreach (var token in raw.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var trimmed = token.Trim();
				switch (trimmed.ToLowerInvariant())
				{
					case "sun": case "sunday": result.Add(DayOfWeek.Sunday); break;
					case "mon": case "monday": result.Add(DayOfWeek.Monday); break;
					case "tue": case "tues": case "tuesday": result.Add(DayOfWeek.Tuesday); break;
					case "wed": case "wednesday": result.Add(DayOfWeek.Wednesday); break;
					case "thu": case "thur": case "thursday": result.Add(DayOfWeek.Thursday); break;
					case "fri": case "friday": result.Add(DayOfWeek.Friday); break;
					case "sat": case "saturday": result.Add(DayOfWeek.Saturday); break;
				}
			}
			return result;
		}

		private static TimeSpan ParseTime(string raw, TimeSpan fallback)
		{
			if (string.IsNullOrEmpty(raw))
				return fallback;
			if (TimeSpan.TryParseExact(raw, new[] { @"h\:mm", @"hh\:mm" },
					CultureInfo.InvariantCulture, out var ts))
				return ts;
			if (TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out ts))
				return ts;
			return fallback;
		}

		private void ScheduleTick(object _)
		{
			try
			{
				if (_scheduleDays == null || _scheduleDays.Count == 0)
					return;

				var now = DateTime.UtcNow;

				if (this.IsActive && _autoEndAt.HasValue && now >= _autoEndAt.Value)
				{
					Log.Info("WoeService: scheduled end firing.");
					_autoEndAt = null;
					this.End();
					return;
				}

				if (!this.IsActive)
				{
					if (!_scheduleDays.Contains(now.DayOfWeek))
						return;

					var todayStart = now.Date.Add(_scheduleStart);
					var withinWindow = now >= todayStart && now < todayStart.Add(_scheduleDuration);
					if (!withinWindow)
						return;

					// Avoid double-fire if the timer wakes twice
					// inside the same minute boundary.
					if ((now - _lastAutoStartAt) < TimeSpan.FromMinutes(1))
						return;

					// Boot-recovery diagnostic: if this is the first auto
					// start since the service initialised AND we're already
					// well into the window, the server most likely came up
					// mid-session — log a warning so the operator knows
					// players missed some uptime.
					var intoWindow = now - todayStart;
					if (_lastAutoStartAt == DateTime.MinValue && intoWindow > TimeSpan.FromMinutes(1))
					{
						Log.Warning("WoeService: auto-start firing {0:0} minute(s) into the scheduled window — server likely booted mid-session.",
							intoWindow.TotalMinutes);
					}
					else
					{
						Log.Info("WoeService: scheduled start firing.");
					}

					_lastAutoStartAt = now;
					_autoEndAt = todayStart.Add(_scheduleDuration);
					this.Start();
				}
			}
			catch (Exception ex)
			{
				Log.Error("WoeService.ScheduleTick: {0}", ex);
			}
		}

		private class WoeConfFile : ConfFile
		{
			public string Days { get; private set; }
			public string StartTime { get; private set; }
			public int DurationMinutes { get; private set; }

			public void Load(string filePath)
			{
				this.Require(filePath);
				this.Days = this.GetString("woe_schedule_days", "Tue,Thu,Sat");
				this.StartTime = this.GetString("woe_schedule_start", "20:00");
				this.DurationMinutes = this.GetInt("woe_schedule_duration_minutes", 120);
			}
		}
	}
}
