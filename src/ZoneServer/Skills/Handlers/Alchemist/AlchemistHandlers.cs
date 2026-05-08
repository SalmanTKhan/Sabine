using System;
using System.Linq;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Alchemist
{
	internal static class AlchemyStub
	{
		public static void Run(UseSkillParams p, string what)
		{
			var caster = p.Character;
			var skill = p.Skill;
			var level = p.SkillLevel;
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.ServerMessage($"{what} is not yet implemented.");
		}
	}

	[SkillHandler(SkillId.AM_AXEMASTERY)]
	public class AxeMasteryHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.AM_LEARNINGPOTION)]
	public class LearningPotionHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.AM_PHARMACY)]
	public class PharmacyHandler : ITargetedSkillHandler
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

	[SkillHandler(SkillId.AM_DEMONSTRATION)]
	public class DemonstrationHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var ratio = 1.0f + 0.20f * level;
			foreach (var enemy in caster.Map.GetCharactersInRange(pos, 1))
			{
				if (!enemy.IsHostileTo(caster)) continue;
				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Fire,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss) enemy.TakeDamage(result.Damage, caster);
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}

	[SkillHandler(SkillId.AM_ACIDTERROR)]
	public class AcidTerrorHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var ratio = 2.0f * level;
			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				IsLongRange = true,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss) target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	[SkillHandler(SkillId.AM_POTIONPITCHER)]
	public class PotionPitcherHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;

			var heal = 100 + 200 * level;
			target.HealHp(heal);
			if (target is PlayerCharacter pc)
				Send.ZC_RECOVERY(pc, ParameterType.Hp, heal);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, heal, 0, 0, ActionType.Skill);
		}
	}

	internal static class AlchemistSummon
	{
		// eAthena skill_unit_db rows for AM_CANNIBALIZE / AM_SPHEREMINE
		// give the summon a 60s lifetime regardless of skill level. The
		// master_id field on the spawned mob tags it as a friendly
		// summon — Sabine's <see cref="Monster.IsHostileTo"/> reads it
		// to skip the caster (and the caster's party).
		public static readonly TimeSpan PlantLifespan = TimeSpan.FromSeconds(60);

		public static Monster Spawn(PlayerCharacter master, IdentityId mobId, TimeSpan? lifespan)
		{
			if (!ZoneServer.Instance.Data.Monsters.Contains(mobId))
				return null;

			var monster = new Monster(mobId)
			{
				Master = master,
				Lifespan = lifespan,
			};
			monster.Warp(master.MapId, master.Position.GetRandomInRange(2));
			return monster;
		}
	}

	[SkillHandler(SkillId.AM_CANNIBALIZE)]
	public class CannibalizeHandler : ITargetedSkillHandler
	{
		// AM_CANNIBALIZE summons a plant scaled to skill level. eAthena
		// skill.c uses the standard plant family: Flora → Hydra →
		// Mandragora → Geographer (capped, since Sabine doesn't always
		// stock the renewal-only Parasite at lvl 5).
		private static readonly IdentityId[] PlantsByLevel = new[]
		{
			IdentityId.JT_FLORA,
			IdentityId.JT_HYDRA,
			IdentityId.JT_MANDRAGORA,
			IdentityId.JT_GEOGRAPHER,
			IdentityId.JT_GEOGRAPHER,
		};

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			if (caster is not PlayerCharacter pc)
				return;

			var idx = Math.Clamp(level - 1, 0, PlantsByLevel.Length - 1);
			AlchemistSummon.Spawn(pc, PlantsByLevel[idx], AlchemistSummon.PlantLifespan);
		}
	}

	[SkillHandler(SkillId.AM_SPHEREMINE)]
	public class SphereMineHandler : ITargetedSkillHandler
	{
		// AM_SPHEREMINE summons a Marine Sphere (1142) tied to the
		// caster. The mine sits idle until any hostile enters its
		// 2-cell contact range, then triggers a Fire-element magic
		// blast (skill_castend_damage_id in eAthena). If nothing
		// triggers it, it auto-despawns after the standard summon
		// lifespan.
		private const int ContactRadius = 2;

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			if (caster is not PlayerCharacter pc)
				return;

			var mine = AlchemistSummon.Spawn(pc, IdentityId.JT_MARINE_SPHERE, AlchemistSummon.PlantLifespan);
			if (mine == null)
				return;

			mine.SelfDestructRadius = ContactRadius;
			mine.SelfDestructLevel = level;
			mine.SelfDestructSkill = SkillId.AM_SPHEREMINE;
		}
	}

	// Chemical Protection — eAthena skill_cast_db has a flat 30s/level
	// duration on every AM_CP_* row (lvl 1 = 30s, lvl 5 = 150s). Buffs
	// the targeted ally (defaults to self) and shields the matching slot
	// from the corresponding Rogue strip skill while active.
	public abstract class ChemicalProtectionHandler : BuffSkillHandler
	{
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(30 * level);
	}

	[SkillHandler(SkillId.AM_CP_WEAPON)]
	public class ChemicalProtectionWeaponHandler : ChemicalProtectionHandler
	{
		protected override StatusId StatusId => StatusId.CpWeapon;
	}

	[SkillHandler(SkillId.AM_CP_SHIELD)]
	public class ChemicalProtectionShieldHandler : ChemicalProtectionHandler
	{
		protected override StatusId StatusId => StatusId.CpShield;
	}

	[SkillHandler(SkillId.AM_CP_ARMOR)]
	public class ChemicalProtectionArmorHandler : ChemicalProtectionHandler
	{
		protected override StatusId StatusId => StatusId.CpArmor;
	}

	[SkillHandler(SkillId.AM_CP_HELM)]
	public class ChemicalProtectionHelmHandler : ChemicalProtectionHandler
	{
		protected override StatusId StatusId => StatusId.CpHelm;
	}

	[SkillHandler(SkillId.AM_BIOETHICS)]
	public class BioethicsHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.AM_BIOTECHNOLOGY)]
	public class BiotechnologyHandler : ITargetedSkillHandler
	{
		private const int EmbryoItemId = 7142;
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.Inventory.AddItem(new Item(EmbryoItemId, 1));
		}
	}

	[SkillHandler(SkillId.AM_CREATECREATURE)]
	public class CreateCreatureHandler : ITargetedSkillHandler
	{
		private const int EmbryoItemId = 7142;
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not PlayerCharacter pc) return;

			if (pc.Homunculus.HasContract)
			{
				pc.ServerMessage("You already have a homunculus contract.");
				return;
			}

			if (pc.Skills.GetLevel(SkillId.AM_BIOETHICS) < 1)
			{
				pc.ServerMessage("You need to study Bioethics first.");
				return;
			}

			var embryo = pc.Inventory.GetItems(static i => i.ClassId == EmbryoItemId).FirstOrDefault();
			if (embryo == null)
			{
				pc.ServerMessage("You need an Embryo.");
				return;
			}
			pc.Inventory.DecrementItem(embryo, 1);

			var rnd = Yggdrasil.Util.RandomProvider.Get();
			var type = (Sabine.Zone.World.Actors.Components.Characters.HomunculusType)(1 + rnd.Next(4));
			pc.Homunculus.Create(type, type.ToString());
			pc.ServerMessage($"You have entered a contract with a {type} homunculus.");
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.AM_CULTIVATION)]
	public class CultivationHandler : ITargetedSkillHandler
	{
		// AM_CULTIVATION grows a wild plant with no master tag. eAthena
		// classic skill_cast_db: lvl 1 spawns a mushroom (random Black
		// or Red), lvl 2 spawns a Red Plant. The mob is wild — it
		// behaves like a regular spawn and stays around as terrain.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			if (caster is not PlayerCharacter pc)
				return;

			IdentityId mobId;
			if (level <= 1)
				mobId = RandomProvider.Get().Next(2) == 0 ? IdentityId.JT_BLACK_MUSHROOM : IdentityId.JT_RED_MUSHROOM;
			else
				mobId = IdentityId.JT_RED_PLANT;

			AlchemistSummon.Spawn(pc, mobId, lifespan: null);
		}
	}

	// AM_FLAMECONTROL is a passive in classic eAthena (skill_db marks it
	// as a flag-only quest skill that gates higher Alchemist content);
	// no runtime effect, so the handler is intentionally a no-op.
	[SkillHandler(SkillId.AM_FLAMECONTROL)]
	public class FlameControlHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.AM_CALLHOMUN)]
	public class CallHomunculusHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not PlayerCharacter pc) return;
			if (!pc.Homunculus.HasContract) { pc.ServerMessage("You have no homunculus contract."); return; }
			if (Sabine.Zone.Skills.Homunculi.HomunculusService.Spawn(pc))
				pc.ServerMessage($"{pc.Homunculus.Name} answers your call.");
			else
				pc.ServerMessage("Your homunculus cannot be called right now.");
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.AM_REST)]
	public class HomunculusRestHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is PlayerCharacter pc)
			{
				Sabine.Zone.Skills.Homunculi.HomunculusService.Rest(pc);
				pc.ServerMessage("Your homunculus is resting.");
			}
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.AM_DRILLMASTER)]
	public class DrillMasterHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.AM_HEALHOMUN)]
	public class HealHomunculusHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is PlayerCharacter pc && pc.Homunculus.HasContract)
			{
				var heal = 50 + 100 * level;
				pc.Homunculus.Heal(heal);
				pc.ServerMessage($"Healed {pc.Homunculus.Name} for {heal}.");
			}
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.AM_RESURRECTHOMUN)]
	public class ResurrectHomunculusHandler : ITargetedSkillHandler
	{
		private const int EmbryoItemId = 7142;
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not PlayerCharacter pc) return;
			if (!pc.Homunculus.HasContract) { pc.ServerMessage("No homunculus contract."); return; }

			var embryo = pc.Inventory.GetItems(static i => i.ClassId == EmbryoItemId).FirstOrDefault();
			if (embryo == null) { pc.ServerMessage("You need an Embryo."); return; }
			pc.Inventory.DecrementItem(embryo, 1);

			if (pc.Homunculus.Resurrect())
				pc.ServerMessage($"{pc.Homunculus.Name} has been resurrected.");
			else
				pc.ServerMessage("Your homunculus is not dead.");

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}
}
