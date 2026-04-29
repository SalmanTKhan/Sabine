using System;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects
{
	/// <summary>
	/// Defines the contract for a class that implements the logic for a
	/// specific status effect (buff or debuff).
	/// </summary>
	public interface IStatusEffectHandler
	{
		/// <summary>
		/// Called when the effect is first applied to the target.
		/// </summary>
		void OnStart(Character target, StatusEffect effect);

		/// <summary>
		/// Called when the effect ends (expires, is dispelled, or is
		/// replaced by a refresh).
		/// </summary>
		void OnEnd(Character target, StatusEffect effect);

		/// <summary>
		/// Called every world tick while the effect is active. Default is
		/// a no-op; override only for DOT/HOT or recurring effects.
		/// </summary>
		void OnTick(Character target, StatusEffect effect, TimeSpan elapsed) { }
	}

	/// <summary>
	/// Marks a class as a handler for a specific status effect, allowing
	/// for automatic registration.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class StatusEffectHandlerAttribute : Attribute
	{
		public StatusId StatusId { get; }

		public StatusEffectHandlerAttribute(StatusId statusId)
		{
			this.StatusId = statusId;
		}
	}
}
