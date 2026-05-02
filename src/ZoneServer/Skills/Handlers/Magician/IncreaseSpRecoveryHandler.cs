using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_SRECOVERY)]
	public class IncreaseSpRecoveryHandler : ITargetedSkillHandler
	{
		// Passive — applied by RecoveryComponent.
		public void Handle(UseSkillParams parameters) { }
	}
}
