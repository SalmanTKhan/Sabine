using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Merchant
{
	[SkillHandler(SkillId.MC_MAMMONITE)]
	public class MammoniteHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return Task.CompletedTask;

			var zenyCost = skill.Level * 100;
			if (caster.Parameters.Zeny < zenyCost)
			{
				if (caster is PlayerCharacter pc)
					pc.ServerMessage("Not enough Zeny to use Mammonite.");
				return Task.CompletedTask;
			}

			caster.Parameters.Modify(ParameterType.Zeny, -zenyCost);

			var ctx = new AttackContext(caster, targetCharacter)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.0f + 0.5f * skill.Level,
			};
			var result = BattleCalculator.Calc(ctx);
			var damage = result.IsMiss ? 0 : result.Damage;

			if (!result.IsMiss)
				targetCharacter.TakeDamage(damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, result.ActionType);

			return Task.CompletedTask;
		}
	}
}
