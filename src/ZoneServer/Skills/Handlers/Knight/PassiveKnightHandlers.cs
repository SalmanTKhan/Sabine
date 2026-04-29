using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Knight
{
	[SkillHandler(SkillId.KN_SPEARMASTERY)]
	public class SpearMasteryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.KN_CAVALIERMASTERY)]
	public class CavalierMasteryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}
}
