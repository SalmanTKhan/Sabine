using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_OWL)]
	public class OwlsEyeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}

	[SkillHandler(SkillId.AC_VULTURE)]
	public class VulturesEyeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}
}
