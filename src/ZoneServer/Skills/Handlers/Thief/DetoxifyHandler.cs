using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_DETOXIFY)]
	public class DetoxifyHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null)
				return;

			if (target.StatusEffects.Has(StatusId.Poison))
				target.StatusEffects.Stop(StatusId.Poison);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}
}
