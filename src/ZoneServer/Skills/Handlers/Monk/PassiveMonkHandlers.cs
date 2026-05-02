using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Monk
{
	[SkillHandler(SkillId.MO_IRONHAND)]
	public class IronHandHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.MO_SPIRITSRECOVERY)]
	public class SpiritsRecoveryHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.MO_DODGE)]
	public class DodgeHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }
}
