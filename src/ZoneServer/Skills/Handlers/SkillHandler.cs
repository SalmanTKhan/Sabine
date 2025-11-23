using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers
{
	/// <summary>
	/// Defines the contract for a class that implements the logic for a specific skill.
	/// </summary>
	public interface ISkillHandler
	{
		/// <summary>
		/// Executes the skill's logic.
		/// </summary>
		/// <param name="caster">The character using the skill.</param>
		/// <param name="target">The target of the skill (can be null for self-cast or ground skills).</param>
		/// <param name="skill">A reference to the skill instance, containing its level and data.</param>
		Task HandleAsync(Character caster, IEntity target, Skill skill);
	}

	/// <summary>
	/// Marks a class as a handler for a specific skill, allowing for automatic registration.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class SkillHandlerAttribute : Attribute
	{
		public SkillId SkillId { get; }

		public SkillHandlerAttribute(SkillId skillId)
		{
			this.SkillId = skillId;
		}
	}
}
