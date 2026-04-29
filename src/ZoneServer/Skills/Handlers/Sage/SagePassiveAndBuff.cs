using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Sage
{
	[SkillHandler(SkillId.SA_ADVANCEDBOOK)]
	public class AdvancedBookHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.SA_DRAGONOLOGY)]
	public class DragonologyHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.SA_FREECAST)]
	public class FreeCastHandler : BuffSkillHandler
	{
		// eAthena SA_FREECAST: cast-while-moving toggle. v1 applies
		// a long-lived flag; movement-system check is a follow-up.
		protected override StatusId StatusId => StatusId.FreeCast;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromHours(24);
	}

	[SkillHandler(SkillId.SA_AUTOSPELL)]
	public class AutoSpellHandler : BuffSkillHandler
	{
		// eAthena SA_AUTOSPELL: melee hits trigger the chosen spell.
		// Spell choice UI is a follow-up; v1 stores the level.
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
