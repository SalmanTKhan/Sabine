using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Rogue
{
	[SkillHandler(SkillId.RG_SNATCHER)]
	public class SnatcherHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.RG_GANGSTER)]
	public class GangsterParadiseHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.RG_COMPULSION)]
	public class CompulsionDiscountHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.RG_PLAGIARISM)]
	public class PlagiarismHandler : ISkillHandler
	{
		// eAthena RG_PLAGIARISM: passive enabling skill copy. The
		// hook itself runs in the combat event listener; the skill
		// is "active" only in that it confirms the copied slot.
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.RG_TUNNELDRIVE)]
	public class TunnelDriveHandler : BuffSkillHandler
	{
		// eAthena RG_TUNNELDRIVE: move-while-Hidden status.
		protected override StatusId StatusId => StatusId.TunnelDrive;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromMinutes(30);
	}
}
