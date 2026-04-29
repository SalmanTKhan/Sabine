using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sabine.Zone.World.Actors;
using Yggdrasil.Logging;

namespace Sabine.Zone.Battle.Cards
{
	/// <summary>
	/// Discovers, registers, and dispatches card handlers. Mirrors the
	/// reflection-driven pattern used by <c>SkillHandlerManager</c> and
	/// <c>StatusEffectHandlerManager</c>.
	/// </summary>
	public static class CardHandlerManager
	{
		private static readonly Dictionary<int, ICardHandler> _handlers = new();

		public static void Initialize()
		{
			var handlerCount = 0;
			var handlerType = typeof(ICardHandler);
			var assembly = Assembly.GetExecutingAssembly();

			var types = assembly.GetTypes()
				.Where(p => handlerType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

			foreach (var type in types)
			{
				var attributes = type.GetCustomAttributes<CardHandlerAttribute>();
				if (!attributes.Any())
					continue;

				var handler = (ICardHandler)Activator.CreateInstance(type);
				foreach (var attr in attributes)
				{
					if (_handlers.ContainsKey(attr.CardItemId))
					{
						Log.Warning($"Duplicate card handler registration for item id {attr.CardItemId}.");
						continue;
					}

					_handlers.Add(attr.CardItemId, handler);
					handlerCount++;
				}
			}

			Log.Info($"Registered {handlerCount} card handlers.");
		}

		public static ICardHandler GetHandler(int cardItemId)
		{
			_handlers.TryGetValue(cardItemId, out var handler);
			return handler;
		}

		/// <summary>
		/// Applies all card bonuses on an item that's just been equipped.
		/// </summary>
		public static void ApplyEquipped(Character character, World.Actors.Item item)
		{
			if (item.Cards == null || item.Cards.Length == 0)
				return;

			foreach (var cardId in item.Cards)
			{
				if (cardId == 0)
					continue;

				var handler = GetHandler(cardId);
				handler?.OnEquip(character, item);
			}
		}

		/// <summary>
		/// Reverses all card bonuses on an item that's just been unequipped.
		/// </summary>
		public static void ApplyUnequipped(Character character, World.Actors.Item item)
		{
			if (item.Cards == null || item.Cards.Length == 0)
				return;

			foreach (var cardId in item.Cards)
			{
				if (cardId == 0)
					continue;

				var handler = GetHandler(cardId);
				handler?.OnUnequip(character, item);
			}
		}
	}
}
