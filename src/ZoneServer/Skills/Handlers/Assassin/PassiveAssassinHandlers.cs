using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Assassin
{
	[SkillHandler(SkillId.AS_RIGHT)]
	public class RightHandMasteryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.AS_LEFT)]
	public class LeftHandMasteryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.AS_KATAR)]
	public class KatarMasteryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}
}
