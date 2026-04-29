using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Hunter
{
	[SkillHandler(SkillId.HT_BLITZBEAT)]
	public class BlitzBeatHandler : ISkillHandler
	{
		// eAthena HT_BLITZBEAT: ranged Wind magic via the falcon.
		// Damage = ((DEX/10)^2 + DEX/2 + 40 + 20*lv) * splashFactor.
		// 3x3 splash centered on the target. Renewal mostly the same.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			if (!caster.StatusEffects.Has(StatusId.FalconFly))
			{
				if (caster is PlayerCharacter pc)
					pc.ServerMessage("You need a Falcon.");
				return Task.CompletedTask;
			}

			var dex = caster.Parameters.Dex;
			var damage = (dex / 10) * (dex / 10) + dex / 2 + 40 + 20 * skill.Level;

			foreach (var enemy in caster.Map.GetCharactersInRange(target.Position, 1))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Magic,
					SkillRatio = 1.0f,
					AttackElement = ElementType.Wind,
					AlwaysHits = true,
					IsLongRange = true,
				};
				var result = BattleCalculator.Calc(ctx);
				var dmg = result.IsMiss ? 0 : System.Math.Max(damage, result.Damage);
				if (dmg > 0) enemy.TakeDamage(dmg, caster);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 1, ActionType.Skill);
			return Task.CompletedTask;
		}
	}
}
