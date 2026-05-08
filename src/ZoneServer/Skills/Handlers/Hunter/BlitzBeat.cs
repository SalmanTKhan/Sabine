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

			Cast(caster, target, level, autocast: false);
		}

		/// <summary>
		/// Resolves a Blitz Beat hit on <paramref name="target"/>, splashing
		/// to adjacent hostiles. Shared with the auto-Blitz listener so the
		/// damage path stays single-source. Mirrors eAthena battle.c:2728:
		/// damage = (dex/10 + int/2 + steelcrow*3 + 40) * 2.
		/// </summary>
		public static void Cast(Character caster, Character target, int level, bool autocast)
		{
			var dex = caster.Parameters.Dex;
			var int_ = caster.Parameters.Int;
			var steelcrow = caster.Skills?.GetLevel(SkillId.HT_STEELCROW) ?? 0;
			var damage = (dex / 10 + int_ / 2 + steelcrow * 3 + 40) * 2;

			foreach (var enemy in caster.Map.GetCharactersInRange(target.Position, 1))
			{
				if (!enemy.IsHostileTo(caster)) continue;

				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = SkillId.HT_BLITZBEAT,
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

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, SkillId.HT_BLITZBEAT, level, damage, 0, 1, ActionType.Skill);
		}
	}
}
