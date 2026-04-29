using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_SOULSTRIKE)]
	public class SoulStrikeHandler : ISkillHandler
	{
		public async Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
				return;

			var hitCount = (skill.Level + 1) / 2;

			Send.ZC_USE_SKILL(caster as PlayerCharacter, skill.Id, skill.Level, target.Handle, true);

			for (var i = 0; i < hitCount; i++)
			{
				var ctx = new AttackContext(caster, targetCharacter)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Magic,
					SkillRatio = 1.0f,
					AttackElement = ElementType.Ghost,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					targetCharacter.TakeDamage(result.Damage, caster);

				await Task.Delay(100);
			}
		}
	}
}
