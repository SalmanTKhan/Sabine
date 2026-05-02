using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_SOULSTRIKE)]
	public class SoulStrikeHandler : ITargetedSkillHandler
	{
		// TODO: re-introduce ~100ms inter-hit delay via a scheduler now
		// that Handle is sync void.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			var hitCount = (level + 1) / 2;

			Send.ZC_USE_SKILL(caster as PlayerCharacter, skill.Id, level, target.Handle, true);

			for (var i = 0; i < hitCount; i++)
			{
				var ctx = new AttackContext(caster, target)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = 1.0f,
					AttackElement = ElementType.Ghost,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					target.TakeDamage(result.Damage, caster);
			}
		}
	}
}
