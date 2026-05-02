using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Blacksmith
{
	[SkillHandler(SkillId.BS_IRON)]
	public class IronTemperingHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.BS_STEEL)]
	public class SteelTemperingHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.BS_ENCHANTEDSTONE)]
	public class EnchantedStoneHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.BS_HILTBINDING)]
	public class HiltBindingHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.BS_FINDINGORE)]
	public class FindingOreHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.BS_WEAPONRESEARCH)]
	public class WeaponResearchHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.BS_SKINTEMPER)]
	public class SkinTemperingHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }
}
