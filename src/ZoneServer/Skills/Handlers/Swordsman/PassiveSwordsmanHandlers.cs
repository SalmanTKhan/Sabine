using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Swordsman
{
	// All three are passives. Their effects are applied where the
	// passive surfaces in the runtime: SM_MOVINGRECOVERY through the
	// regen ticker, SM_FATALBLOW through Bash's stun roll, and
	// SM_AUTOBERSERK through the HP-watch hook.

	[SkillHandler(SkillId.SM_MOVINGRECOVERY)]
	public class MovingRecoveryHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}

	[SkillHandler(SkillId.SM_FATALBLOW)]
	public class FatalBlowHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}

	[SkillHandler(SkillId.SM_AUTOBERSERK)]
	public class AutoBerserkHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}
}
