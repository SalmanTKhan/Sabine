using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_TWOHAND)]
	public class TwoHandSwordMasteryHandler : ITargetedSkillHandler
	{
		// Passive — applied during stat recalculation.
		public void Handle(UseSkillParams parameters) { }
	}
}
