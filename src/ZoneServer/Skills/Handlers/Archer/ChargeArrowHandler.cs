using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Archer
{
	[SkillHandler(SkillId.AC_CHARGEARROW)]
	public class ChargeArrowHandler : ISkillHandler
	{
		// eAthena AC_CHARGEARROW (Arrow Repel): single-level ranged
		// attack at 150% damage that knocks the target back 6 cells.
		// Works only with a bow equipped.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = 1.5f,
				IsLongRange = true,
				WeaponRequired = true,
			};

			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				target.TakeDamage(result.Damage, caster);
				target.Controller?.Knockback(caster.Position, 6);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, result.HitCount, result.ActionType);
			return Task.CompletedTask;
		}
	}
}
