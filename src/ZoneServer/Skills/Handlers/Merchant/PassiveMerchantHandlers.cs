using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_INCCARRY)]
	public class EnlargeWeightLimitHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// This is a passive skill. Its effects are applied during character stat recalculation.
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MC_DISCOUNT)]
	public class DiscountHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// This is a passive skill. Its effects are applied when buying from NPCs.
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MC_OVERCHARGE)]
	public class OverchargeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// This is a passive skill. Its effects are applied when selling to NPCs.
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.MC_PUSHCART)]
	public class PushcartHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			// This is a passive skill that enables cart usage. Logic is handled elsewhere.
			return Task.CompletedTask;
		}
	}
}