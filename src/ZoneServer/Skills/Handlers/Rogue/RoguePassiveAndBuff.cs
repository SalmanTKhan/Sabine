using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Rogue
{
	[SkillHandler(SkillId.RG_SNATCHER)]
	public class SnatcherHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.RG_GANGSTER)]
	public class GangsterParadiseHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.RG_COMPULSION)]
	public class CompulsionDiscountHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.RG_PLAGIARISM)]
	public class PlagiarismHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.RG_TUNNELDRIVE)]
	public class TunnelDriveHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.TunnelDrive;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromMinutes(30);
	}
}
