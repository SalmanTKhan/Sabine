using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_INCCARRY)]
	public class EnlargeWeightLimitHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}

	[SkillHandler(SkillId.MC_DISCOUNT)]
	public class DiscountHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}

	[SkillHandler(SkillId.MC_OVERCHARGE)]
	public class OverchargeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}

	[SkillHandler(SkillId.MC_PUSHCART)]
	public class PushcartHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters) { }
	}
}
