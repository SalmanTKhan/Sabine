using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_DEMONBANE)]
	public class DemonBaneHandler : ITargetedSkillHandler
	{
		// Passive — applied during stat recalculation.
		public void Handle(UseSkillParams parameters) { }
	}
}
