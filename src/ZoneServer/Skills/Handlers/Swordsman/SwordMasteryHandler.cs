using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_SWORD)]
	public class SwordMasteryHandler : ITargetedSkillHandler
	{
		// Passive — applied during stat recalculation.
		public void Handle(UseSkillParams parameters) { }
	}
}
