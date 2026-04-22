using System.Threading.Tasks;
using Sabine.Shared.Const;
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
			{
				return;
			}

			var hitCount = (skill.Level + 1) / 2;
			var damage = caster.Parameters.MagicAttack;

			// TODO: Add bonus damage vs Undead property monsters.
			// if (targetCharacter.Property == Property.Undead) { damage *= 1.5f; }

			Send.ZC_USE_SKILL(caster as PlayerCharacter, skill.Id, skill.Level, target.Handle, true);

			for (var i = 0; i < hitCount; i++)
			{
				targetCharacter.TakeDamage(damage, caster);
				await Task.Delay(100);
			}
		}
	}
}