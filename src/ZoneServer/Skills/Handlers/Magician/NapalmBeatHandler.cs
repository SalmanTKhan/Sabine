using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	[SkillHandler(SkillId.MG_NAPALMBEAT)]
	public class NapalmBeatHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, IEntity target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return Task.CompletedTask;
			}

			// Damage formula: MATK * (0.6 + (0.1 * skill.Level))
			var damageMultiplier = 0.6f + (0.1f * skill.Level);
			var damage = (int)(caster.Parameters.MagicAttack * damageMultiplier);

			// TODO: Element should be Ghost/Psychokinesis.
			targetCharacter.TakeDamage(damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, damage, 0, 0, ActionType.Skill);

			return Task.CompletedTask;
		}
	}
}