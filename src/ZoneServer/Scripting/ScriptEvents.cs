using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Yggdrasil.Logging;

namespace Sabine.Zone.Scripting
{
	/// <summary>
	/// Pub/sub registry for script-level events. Covers rAthena's
	/// <c>donpcevent "NpcName::OnLabel"</c> pattern: NPC scripts call
	/// <see cref="Subscribe"/> at load time with their label; other
	/// scripts use <see cref="RaiseAsync"/> to fire all handlers for
	/// that label.
	/// </summary>
	/// <remarks>
	/// Event names follow the <c>NpcName::OnLabel</c> convention but are
	/// treated as opaque strings — case-sensitive. Handlers run
	/// sequentially in registration order; exceptions are caught and
	/// logged so one bad handler can't break a fan-out.
	/// </remarks>
	public static class ScriptEvents
	{
		private static readonly ConcurrentDictionary<string, List<ScriptEventHandler>> _handlers = new();

		// Matches an instance-suffixed NPC name in an event id —
		// `"#1F Controller#5::OnInstanceInit"` → captures the bare
		// portion (everything before the trailing `#<digits>` *that
		// directly precedes* `::`). The trailing `#5` is the suffix
		// emitted by `Shortcuts.GetInstanceNpcName`; the leading `#`
		// in `#1F Controller` is part of the NPC's own name.
		private static readonly Regex _InstanceSuffixRe =
			new(@"^(?<base>.+?)#(?<id>\d+)(?<rest>::.*)$", RegexOptions.Compiled);

		/// <summary>
		/// Registers a handler for the given event name.
		/// </summary>
		public static void Subscribe(string eventName, ScriptEventHandler handler)
		{
			if (string.IsNullOrEmpty(eventName))
				throw new ArgumentException("Event name must not be empty.", nameof(eventName));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			var list = _handlers.GetOrAdd(eventName, _ => new List<ScriptEventHandler>());
			lock (list)
				list.Add(handler);
		}

		/// <summary>
		/// Removes a previously-registered handler. Returns true if a
		/// handler was removed.
		/// </summary>
		public static bool Unsubscribe(string eventName, ScriptEventHandler handler)
		{
			if (!_handlers.TryGetValue(eventName, out var list))
				return false;

			lock (list)
				return list.Remove(handler);
		}

		/// <summary>
		/// Removes all handlers for the given event name. Useful when
		/// reloading scripts.
		/// </summary>
		public static void Clear(string eventName)
		{
			_handlers.TryRemove(eventName, out _);
		}

		/// <summary>
		/// Removes all registered handlers across all event names.
		/// </summary>
		public static void ClearAll()
		{
			_handlers.Clear();
		}

		/// <summary>
		/// Fires all handlers registered for the given event name,
		/// passing the supplied arguments. Handlers run sequentially.
		/// Exceptions are caught and logged so one failure doesn't
		/// abort the rest of the fan-out.
		/// </summary>
		public static async Task RaiseAsync(string eventName, params object[] args)
		{
			if (!_handlers.TryGetValue(eventName, out var list))
			{
				// Per-instance scoping: scripts subscribe to bare
				// names at load time (`"NpcName::OnEvent"`) but raise
				// with `Shortcuts.GetInstanceNpcName(...)` which
				// appends a `#<id>` suffix to the NPC portion. When
				// the exact suffixed name has no subscribers, fall
				// back to the bare name and inject the instance id as
				// the first arg so the handler can scope its
				// per-instance state if needed.
				var match = _InstanceSuffixRe.Match(eventName);
				if (!match.Success)
					return;
				var baseName = match.Groups["base"].Value + match.Groups["rest"].Value;
				if (!_handlers.TryGetValue(baseName, out list))
					return;
				if (!int.TryParse(match.Groups["id"].Value, out var instanceId))
					instanceId = 0;
				var newArgs = new object[args.Length + 1];
				newArgs[0] = instanceId;
				Array.Copy(args, 0, newArgs, 1, args.Length);
				args = newArgs;
				eventName = baseName;
			}

			ScriptEventHandler[] snapshot;
			lock (list)
				snapshot = list.ToArray();

			foreach (var handler in snapshot)
			{
				try
				{
					await handler(args);
				}
				catch (Exception ex)
				{
					Log.Error("ScriptEvents.RaiseAsync: handler for '{0}' threw: {1}", eventName, ex);
				}
			}
		}

		/// <summary>
		/// Returns the number of handlers registered for the given
		/// event name.
		/// </summary>
		public static int CountHandlers(string eventName)
		{
			if (!_handlers.TryGetValue(eventName, out var list))
				return 0;
			lock (list)
				return list.Count;
		}
	}

	/// <summary>
	/// Async handler signature for <see cref="ScriptEvents"/>. Argument
	/// array is passed verbatim from <c>RaiseAsync</c>; converted scripts
	/// can read by index.
	/// </summary>
	public delegate Task ScriptEventHandler(object[] args);
}
