using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Assassin
{
	[SkillHandler(SkillId.AS_RIGHT)]
	public class RightHandMasteryHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.AS_LEFT)]
	public class LeftHandMasteryHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.AS_KATAR)]
	public class KatarMasteryHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }
}
