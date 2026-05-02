using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Sage
{
	[SkillHandler(SkillId.SA_ADVANCEDBOOK)]
	public class AdvancedBookHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.SA_DRAGONOLOGY)]
	public class DragonologyHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.SA_FREECAST)]
	public class FreeCastHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.FreeCast;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromHours(24);
	}

	[SkillHandler(SkillId.SA_AUTOSPELL)]
	public class AutoSpellHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.AutoSpell;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromMinutes(1);
	}

	[SkillHandler(SkillId.SA_FLAMELAUNCHER)]
	public class EndowFireHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.EndowFire;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60 + 60 * level);
	}

	[SkillHandler(SkillId.SA_FROSTWEAPON)]
	public class EndowWaterHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.EndowWater;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60 + 60 * level);
	}

	[SkillHandler(SkillId.SA_LIGHTNINGLOADER)]
	public class EndowWindHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.EndowWind;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60 + 60 * level);
	}

	[SkillHandler(SkillId.SA_SEISMICWEAPON)]
	public class EndowEarthHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.EndowEarth;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(60 + 60 * level);
	}
}
