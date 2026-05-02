using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Knight
{
	[SkillHandler(SkillId.KN_SPEARMASTERY)]
	public class SpearMasteryHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}

	[SkillHandler(SkillId.KN_CAVALIERMASTERY)]
	public class CavalierMasteryHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}
}
