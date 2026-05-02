using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_DOUBLE)]
	public class DoubleAttackHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}

	[SkillHandler(SkillId.TF_MISS)]
	public class DodgeIncreaseHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}
}
