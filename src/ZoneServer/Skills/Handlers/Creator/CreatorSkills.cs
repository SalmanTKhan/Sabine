using System;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Creator
{
	/// <summary>
	/// CR_ACIDDEMONSTRATION (Acid Demonstration) — magic-physical hybrid
	/// damage that scales with the target's VIT and the caster's INT.
	/// eAthena battle.c:2766 sets
	/// <c>damage = 7 * targetVit * casterInt^2 / (10 * (targetVit + casterInt))</c>,
	/// halved on PvP targets. Bypasses the rest of the damage pipeline
	/// (BF_MISC, no DEF / element / size). The PvP halving uses the
	/// same <c>tsd</c> check as eAthena; here we mirror via
	/// <c>target is PlayerCharacter</c>.
	/// </summary>
	[SkillHandler(SkillId.CR_ACIDDEMONSTRATION)]
	public class AcidDemonstrationHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;
			if (target == null) return;

			var vit = target.Parameters.Vit;
			var intel = caster.Parameters.Int;
			var sum = vit + intel;
			if (sum <= 0) return;

			long damage = 7L * vit * intel * intel / (10L * sum);
			if (target is PlayerCharacter)
				damage >>= 1;
			if (damage < 0) damage = 0;
			if (damage > int.MaxValue >> 1) damage = int.MaxValue >> 1;

			target.TakeDamage((int)damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, (int)damage, 0, 1, ActionType.Skill);
		}
	}

	/// <summary>
	/// CR_FULLPROTECTION (Full Chemical Protection) — protects all four
	/// of the target's main equip slots from break for the duration.
	/// eAthena gates this through item-flag scripts on each equip slot;
	/// Sabine has no equip-break listener wired (see WS_MELTDOWN), so
	/// this stays marker today. The status marker still lands so once
	/// the listener ships, it can read the flag without a wire-format
	/// change. Duration: 120s + 120s/level (skill_cast_db.txt:807).
	/// </summary>
	[SkillHandler(SkillId.CR_FULLPROTECTION)]
	public class FullProtectionHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.FullProtection;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(120 * level);
	}

	/// <summary>
	/// CR_SLIMPITCHER (Slim Potion Pitcher) — area heal that consumes a
	/// potion item; eAthena reads the heal magnitude from the item's
	/// usage script (heal value of the used potion + a level scalar).
	/// Sabine doesn't expose the consume-and-read pipeline yet, so
	/// today this lands a flat fallback heal on the target only,
	/// scaling with caster INT — the AOE-pitcher arm is deferred.
	/// </summary>
	[SkillHandler(SkillId.CR_SLIMPITCHER)]
	public class SlimPitcherHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target ?? caster;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var heal = (caster.Parameters.BaseLevel + caster.Parameters.Int) / 4 * (1 + level);
			target.HealHp(heal);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, heal, 0, 0, ActionType.Skill);
		}
	}

	/// <summary>
	/// CR_SYNTHESISPOTION (Potion Synthesis) — the Creator-side produce
	/// path that mirrors AM_PHARMACY. Both routes feed the same
	/// <see cref="Sabine.Zone.Skills.Brewing.PharmacyService"/>; the
	/// Creator-only recipe split lives in the recipe table itself, so
	/// the handler is identical to <c>PharmacyHandler</c>.
	/// </summary>
	[SkillHandler(SkillId.CR_SYNTHESISPOTION)]
	public class SynthesisPotionHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (caster is not PlayerCharacter pc)
				return;

			var recipe = Sabine.Zone.Skills.Brewing.PharmacyService.ResolveRecipe(pc, level);
			if (recipe == null)
			{
				pc.ServerMessage("You do not have ingredients for any recipe.");
				return;
			}

			var ok = Sabine.Zone.Skills.Brewing.PharmacyService.Brew(pc, recipe, level);
			pc.ServerMessage(ok
				? $"Brewing succeeded ({recipe.OutputAmount} x item {recipe.OutputId})."
				: "Brewing failed.");
		}
	}

	/// <summary>
	/// CR_ALCHEMY — passive that boosts the success rate of brewing
	/// recipes. eAthena threads this through the produce-success roll
	/// in skill.c. Sabine's <see cref="Sabine.Zone.Skills.Brewing.PharmacyService"/>
	/// doesn't read a per-skill bonus yet; once it does, the read site
	/// reads <c>Skills.GetLevel(SkillId.CR_ALCHEMY)</c>. Stub today —
	/// pure passive, no handler attribute, no cast.
	/// </summary>
	internal static class AlchemyPassive { }

	/// <summary>
	/// CR_CULTIVATION — places a plant monster on the target cell with a
	/// 50% success roll. eAthena skill.c:6451-6463: at level 1 the spawn
	/// rolls between Black Mushroom (1084) and Red Mushroom (1085); at
	/// level 2+ it rolls one of the six standard plants (1078-1083).
	/// The plant is tagged with master_id so the caster's friendly
	/// filter excludes it, and a timed despawn matches
	/// <c>skill_get_time</c> (300s per skill_cast_db.txt:829).
	///
	/// Sabine reuses the AlchemistSummon helper that already drives
	/// AM_CANNIBALIZE / AM_SPHEREMINE — same Master tag, same Lifespan
	/// auto-despawn, same friendly-filter behaviour.
	/// </summary>
	[SkillHandler(SkillId.CR_CULTIVATION)]
	public class CultivationHandler : Skills.Handlers.IGroundSkillHandler
	{
		private const int SuccessRate = 50;
		private static readonly TimeSpan PlantLifespan = TimeSpan.FromSeconds(300);

		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var center = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);

			if (caster is not PlayerCharacter pc) return;
			if (Random.Shared.Next(100) >= SuccessRate) return;

			Sabine.Shared.Const.IdentityId mobId;
			if (level < 2)
			{
				// 1084 (Black Mushroom) or 1085 (Red Mushroom)
				mobId = (Sabine.Shared.Const.IdentityId)(1084 + Random.Shared.Next(2));
			}
			else
			{
				// 1078..1083 — Red / Blue / Green / Yellow / White / Shining Plant
				mobId = (Sabine.Shared.Const.IdentityId)(1078 + Random.Shared.Next(6));
			}

			Sabine.Zone.Skills.Handlers.Alchemist.AlchemistSummon.Spawn(pc, mobId, PlantLifespan);
		}
	}
}
