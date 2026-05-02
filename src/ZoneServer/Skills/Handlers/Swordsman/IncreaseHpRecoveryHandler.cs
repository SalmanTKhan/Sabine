using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	[SkillHandler(SkillId.SM_RECOVERY)]
	public class IncreaseHpRecoveryHandler : ITargetedSkillHandler
	{
		// Passive — effects applied by RecoveryComponent.
		public void Handle(UseSkillParams parameters) { }
	}
}
