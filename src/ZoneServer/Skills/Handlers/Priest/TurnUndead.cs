using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Priest
{
	[SkillHandler(SkillId.PR_TURNUNDEAD)]
	public class TurnUndeadHandler : ISkillHandler
	{
		// eAthena PR_TURNUNDEAD: Holy magic, undead-only. Has a chance
		// to instantly kill (chance = 20% + 2*BaseLv difference); if it
		// doesn't kill, deals (BaseLv+INT) Holy damage scaled by skill
		// level. Pre-renewal and renewal share the formula.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			if (!IsUndead(target))
			{
				if (caster is PlayerCharacter pc)
					pc.ServerMessage("Target is not Undead.");
				return Task.CompletedTask;
			}

			var rnd = RandomProvider.Get();
			var killChance = 20 + 2 * (caster.Parameters.BaseLevel - target.Parameters.BaseLevel) + 2 * skill.Level;
			killChance = System.Math.Clamp(killChance, 0, 100);

			if (rnd.Next(100) < killChance)
			{
				target.TakeDamage(target.Parameters.Hp, caster);
				Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, target.Parameters.Hp, 0, 1, ActionType.Skill);
				return Task.CompletedTask;
			}

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Magic,
				SkillRatio = 0.2f * skill.Level,
				AttackElement = ElementType.Holy,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
				target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 1, result.ActionType);
			return Task.CompletedTask;
		}

		private static bool IsUndead(Character target)
		{
			if (target is not Monster monster) return false;
			return monster.Data.Element == ElementType.Undead || monster.Data.Race == RaceType.Undead;
		}
	}
}
