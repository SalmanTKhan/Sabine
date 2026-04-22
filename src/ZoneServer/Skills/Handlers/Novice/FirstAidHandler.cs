using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Novice
{
	[SkillHandler(SkillId.NV_FIRSTAID)]
	public class FirstAidHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			// The base 'UseSkill' method already deducted the SP cost.
			// This skill heals 5 HP per level.
			var healAmount = 5 * skill.Level;

			caster.HealHp(healAmount);
			// In a more complex system, you might show a skill animation or effect here.

			return Task.CompletedTask;
		}
	}
}
