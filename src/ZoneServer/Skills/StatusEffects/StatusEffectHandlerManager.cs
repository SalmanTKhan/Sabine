using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sabine.Shared.Const;
using Yggdrasil.Logging;

namespace Sabine.Zone.Skills.StatusEffects
{
	/// <summary>
	/// Discovers, registers, and provides access to status effect handlers.
	/// </summary>
	public static class StatusEffectHandlerManager
	{
		private static readonly Dictionary<StatusId, IStatusEffectHandler> _handlers = new();

		/// <summary>
		/// Initializes the manager by scanning for and registering all status
		/// effect handlers.
		/// </summary>
		public static void Initialize()
		{
			var handlerCount = 0;
			var handlerType = typeof(IStatusEffectHandler);
			var assembly = Assembly.GetExecutingAssembly();

			var types = assembly.GetTypes()
				.Where(p => handlerType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

			foreach (var type in types)
			{
				var attributes = type.GetCustomAttributes<StatusEffectHandlerAttribute>();
				if (!attributes.Any())
					continue;

				var handler = (IStatusEffectHandler)Activator.CreateInstance(type);
				foreach (var attr in attributes)
				{
					if (_handlers.ContainsKey(attr.StatusId))
					{
						Log.Warning($"Duplicate status effect handler registration for {attr.StatusId}.");
						continue;
					}

					_handlers.Add(attr.StatusId, handler);
					handlerCount++;
				}
			}

			Log.Info($"Registered {handlerCount} status effect handlers.");
		}

		/// <summary>
		/// Returns the handler for a status effect, or null if none is
		/// registered.
		/// </summary>
		public static IStatusEffectHandler GetHandler(StatusId statusId)
		{
			_handlers.TryGetValue(statusId, out var handler);
			return handler;
		}
	}
}
