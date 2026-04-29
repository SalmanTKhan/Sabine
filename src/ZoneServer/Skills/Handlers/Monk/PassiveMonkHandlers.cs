using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Monk
{
	[SkillHandler(SkillId.MO_IRONHAND)]
	public class IronHandHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.MO_SPIRITSRECOVERY)]
	public class SpiritsRecoveryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.MO_DODGE)]
	public class DodgeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}
}
