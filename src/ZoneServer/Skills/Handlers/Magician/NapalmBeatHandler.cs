using System.Linq;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_NAPALMBEAT)]
	public class NapalmBeatHandler : ITargetedSkillHandler
	{
		// eAthena MG_NAPALMBEAT: splits the rolled damage across up to
		// 3 enemies in a 1-cell radius of the primary target (primary
		// included). Per-victim damage = totalDamage / N, where N is
		// the actual victim count (1..3). See ref/eAthena/src/map/skill.c
		// (search MG_NAPALMBEAT).
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Magic,
				SkillRatio = 0.6f + 0.1f * level,
				AttackElement = ElementType.Ghost,
			};
			var result = BattleCalculator.Calc(ctx);
			var totalDamage = result.IsMiss ? 0 : result.Damage;

			// Collect primary + up to 2 hostile neighbours within 1
			// cell of the primary target. Cap victims at 3 per eAthena.
			var victims = new System.Collections.Generic.List<Sabine.Zone.World.Actors.Character> { target };
			foreach (var other in caster.Map.GetCharactersInRange(target.Position, 1))
			{
				if (victims.Count >= 3) break;
				if (other == target || other == caster) continue;
				if (!other.IsHostileTo(caster)) continue;
				victims.Add(other);
			}

			var perVictim = victims.Count > 0 ? totalDamage / victims.Count : 0;
			foreach (var v in victims)
			{
				if (perVictim > 0)
					v.TakeDamage(perVictim, caster);
			}

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, perVictim, 0, 0, result.ActionType);
		}
	}
}
