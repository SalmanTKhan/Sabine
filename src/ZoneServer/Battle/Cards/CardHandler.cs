using System;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Cards
{
	/// <summary>
	/// Defines the contract for a class implementing the bonuses a single
	/// card grants when slotted into an equipped item. Mirrors the way
	/// rAthena/eAthena attaches an item-script <c>{ bonus2 ...; }</c> to
	/// each card row.
	/// </summary>
	public interface ICardHandler
	{
		/// <summary>
		/// Called when an item containing this card has just been
		/// equipped. Implementations write into
		/// <c>character.Modifiers</c> to grant bonuses.
		/// </summary>
		void OnEquip(Character character, World.Actors.Item item);

		/// <summary>
		/// Called when an item containing this card has just been
		/// unequipped. Implementations must reverse exactly what
		/// <see cref="OnEquip"/> applied so equip+unequip is symmetric.
		/// </summary>
		void OnUnequip(Character character, World.Actors.Item item);
	}

	/// <summary>
	/// Marks a class as the handler for a specific card item id.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class CardHandlerAttribute : Attribute
	{
		public int CardItemId { get; }

		public CardHandlerAttribute(int cardItemId)
		{
			this.CardItemId = cardItemId;
		}
	}
}
