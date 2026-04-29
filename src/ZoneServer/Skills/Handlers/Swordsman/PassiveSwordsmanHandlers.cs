using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	// All three are passives. Their effects are applied where the
	// passive surfaces in the runtime: SM_MOVINGRECOVERY through the
	// regen ticker, SM_FATALBLOW through Bash's stun roll, and
	// SM_AUTOBERSERK through the HP-watch hook.

	[SkillHandler(SkillId.SM_MOVINGRECOVERY)]
	public class MovingRecoveryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.SM_FATALBLOW)]
	public class FatalBlowHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.SM_AUTOBERSERK)]
	public class AutoBerserkHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}
}
