using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Hunter
{
	[SkillHandler(SkillId.HT_BLITZBEAT)]
	public class BlitzBeatHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;
			if (!caster.StatusEffects.Has(StatusId.FalconFly))
			{
				if (caster is PlayerCharacter pc)
					pc.ServerMessage("You need a Falcon.");
				return;
			}

			var dex = caster.Parameters.Dex;
			var damage = (dex / 10) * (dex / 10) + dex / 2 + 40 + 20 * level;

			foreach (var enemy in caster.Map.GetCharactersInRange(target.Position, 1))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
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

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, damage, 0, 1, ActionType.Skill);
		}
	}
}
