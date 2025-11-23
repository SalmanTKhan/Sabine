using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sabine.Shared.Const;
using Sabine.Zone.Skills.Handlers;
using Yggdrasil.Logging;

namespace Sabine.Zone.Skills
{
	/// <summary>
	/// Discovers, registers, and provides access to skill handlers.
	/// </summary>
	public static class SkillHandlerManager
	{
		private static readonly Dictionary<SkillId, ISkillHandler> _handlers = new();

		/// <summary>
		/// Initializes the manager by scanning for and registering all skill handlers.
		/// </summary>
		public static void Initialize()
		{
			var handlerCount = 0;
			var handlerType = typeof(ISkillHandler);
			var assembly = Assembly.GetExecutingAssembly();

			var types = assembly.GetTypes()
				.Where(p => handlerType.IsAssignableFrom(p) && !p.IsInterface);

			foreach (var type in types)
			{
				var attributes = type.GetCustomAttributes<SkillHandlerAttribute>();
				if (!attributes.Any())
					continue;

				var handler = (ISkillHandler)Activator.CreateInstance(type);
				foreach (var attr in attributes)
				{
					if (_handlers.ContainsKey(attr.SkillId))
					{
						Log.Warning($"Duplicate skill handler registration for {attr.SkillId}.");
						continue;
					}

					_handlers.Add(attr.SkillId, handler);
					handlerCount++;
				}
			}

			Log.Info($"Registered {handlerCount} skill handlers.");
		}

		/// <summary>
		/// Gets the handler for a specific skill.
		/// </summary>
		/// <param name="skillId">The ID of the skill.</param>
		/// <returns>The registered handler, or null if not found.</returns>
		public static ISkillHandler GetHandler(SkillId skillId)
		{
			_handlers.TryGetValue(skillId, out var handler);
			return handler;
		}
	}
}
