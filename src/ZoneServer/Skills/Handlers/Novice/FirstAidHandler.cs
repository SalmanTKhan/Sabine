using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Novice
{
	[SkillHandler(SkillId.NV_FIRSTAID)]
	public class FirstAidHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var level = parameters.SkillLevel;

			// Sabine: 5 HP per level. eAthena classic is flat 5 (skill.c
			// status_heal(bl,5,0,0)); Sabine intentionally diverges here.
			var healAmount = 5 * level;

			caster.HealHp(healAmount);
		}
	}
}
