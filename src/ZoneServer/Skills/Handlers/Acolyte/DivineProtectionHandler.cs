using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_DP)]
	public class DivineProtectionHandler : ITargetedSkillHandler
	{
		// Passive — applied during stat recalculation.
		public void Handle(UseSkillParams parameters) { }
	}
}
