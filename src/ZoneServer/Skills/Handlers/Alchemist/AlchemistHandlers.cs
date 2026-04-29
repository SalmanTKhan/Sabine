using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Alchemist
{
	internal static class AlchemyStub
	{
		public static Task Run(Character caster, Skill skill, string what)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.ServerMessage($"{what} is not yet implemented.");
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AM_AXEMASTERY)]
	public class AxeMasteryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.AM_LEARNINGPOTION)]
	public class LearningPotionHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.AM_PHARMACY)]
	public class PharmacyHandler : ISkillHandler
	{
		// eAthena AM_PHARMACY: recipe-driven potion creation. v1
		// auto-picks the highest-level recipe whose ingredients are
		// in the player's inventory; the per-recipe selection UI is
		// a follow-up.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is not PlayerCharacter pc)
				return Task.CompletedTask;

			var recipe = Sabine.Zone.Skills.Brewing.PharmacyService.ResolveRecipe(pc, skill.Level);
			if (recipe == null)
			{
				pc.ServerMessage("You don't have ingredients for any recipe.");
				return Task.CompletedTask;
			}

			var ok = Sabine.Zone.Skills.Brewing.PharmacyService.Brew(pc, recipe, skill.Level);
			pc.ServerMessage(ok
				? $"Brewing succeeded ({recipe.OutputAmount} × item {recipe.OutputId})."
				: "Brewing failed.");

			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AM_DEMONSTRATION)]
	public class DemonstrationHandler : ISkillHandler
	{
		// eAthena AM_DEMONSTRATION: 100% + 20%*lv Fire MATK in a
		// 3x3 area, 3% chance to break the target's weapon.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ratio = 1.0f + 0.20f * skill.Level;
			foreach (var enemy in caster.Map.GetCharactersInRange(target.Position, 1))
			{
				if (!enemy.IsHostileTo(caster)) continue;
				var ctx = new AttackContext(caster, enemy)
				{
					SkillId = skill.Id,
					SkillLevel = skill.Level,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Fire,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss) enemy.TakeDamage(result.Damage, caster);
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AM_ACIDTERROR)]
	public class AcidTerrorHandler : ISkillHandler
	{
		// eAthena AM_ACIDTERROR: 200%*lv ranged physical, ignores
		// armor/shield refine. 30% chance to break shield. Renewal:
		// similar shape with VIT-based denominator.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;

			var ratio = 2.0f * skill.Level;
			var ctx = new AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = skill.Level,
				Kind = AttackKind.Physical,
				SkillRatio = ratio,
				IsLongRange = true,
				WeaponRequired = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss) target.TakeDamage(result.Damage, caster);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 1, result.ActionType);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AM_POTIONPITCHER)]
	public class PotionPitcherHandler : ISkillHandler
	{
		// eAthena AM_POTIONPITCHER: throws a healing potion at an
		// ally. v1 heals a flat amount based on skill level.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character t) return Task.CompletedTask;

			var heal = 100 + 200 * skill.Level;
			t.HealHp(heal);
			if (t is PlayerCharacter pc)
				Send.ZC_RECOVERY(pc, ParameterType.Hp, heal);

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, heal, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AM_CANNIBALIZE)]
	public class CannibalizeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> AlchemyStub.Run(caster, skill, "Plant summoning");
	}

	[SkillHandler(SkillId.AM_SPHEREMINE)]
	public class SphereMineHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> AlchemyStub.Run(caster, skill, "Sphere Mine summoning");
	}

	[SkillHandler(SkillId.AM_CP_WEAPON)]
	public class ChemicalProtectionWeaponHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> AlchemyStub.Run(caster, skill, "Chemical Protection Weapon");
	}
	[SkillHandler(SkillId.AM_CP_SHIELD)]
	public class ChemicalProtectionShieldHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> AlchemyStub.Run(caster, skill, "Chemical Protection Shield");
	}
	[SkillHandler(SkillId.AM_CP_ARMOR)]
	public class ChemicalProtectionArmorHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> AlchemyStub.Run(caster, skill, "Chemical Protection Armor");
	}
	[SkillHandler(SkillId.AM_CP_HELM)]
	public class ChemicalProtectionHelmHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> AlchemyStub.Run(caster, skill, "Chemical Protection Helm");
	}

	[SkillHandler(SkillId.AM_BIOETHICS)]
	public class BioethicsHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.AM_BIOTECHNOLOGY)]
	public class BiotechnologyHandler : ISkillHandler
	{
		// eAthena AM_BIOTECHNOLOGY: prerequisite for the homunculus
		// path. v1: produce one Embryo (item 7142) on use.
		private const int EmbryoItemId = 7142;
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.Inventory.AddItem(new Item(EmbryoItemId, 1));
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AM_CREATECREATURE)]
	public class CreateCreatureHandler : ISkillHandler
	{
		// eAthena AM_CREATECREATURE: consumes one Embryo to seal a
		// homunculus contract. v1: random type pick, status set to
		// Resting; AM_CALLHOMUN actually summons.
		private const int EmbryoItemId = 7142;
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not PlayerCharacter pc) return Task.CompletedTask;

			if (pc.Homunculus.HasContract)
			{
				pc.ServerMessage("You already have a homunculus contract.");
				return Task.CompletedTask;
			}

			if (pc.Skills.GetLevel(SkillId.AM_BIOETHICS) < 1)
			{
				pc.ServerMessage("You need to study Bioethics first.");
				return Task.CompletedTask;
			}

			var embryo = pc.Inventory.GetItems(static i => i.ClassId == EmbryoItemId).FirstOrDefault();
			if (embryo == null)
			{
				pc.ServerMessage("You need an Embryo.");
				return Task.CompletedTask;
			}
			pc.Inventory.DecrementItem(embryo, 1);

			var rnd = Yggdrasil.Util.RandomProvider.Get();
			var type = (Sabine.Zone.World.Actors.Components.Characters.HomunculusType)(1 + rnd.Next(4));
			pc.Homunculus.Create(type, type.ToString());
			pc.ServerMessage($"You have entered a contract with a {type} homunculus.");
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AM_CULTIVATION)]
	public class CultivationHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> AlchemyStub.Run(caster, skill, "Plant cultivation");
	}

	[SkillHandler(SkillId.AM_FLAMECONTROL)]
	public class FlameControlHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> AlchemyStub.Run(caster, skill, "Flame Control");
	}

	[SkillHandler(SkillId.AM_CALLHOMUN)]
	public class CallHomunculusHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not PlayerCharacter pc) return Task.CompletedTask;
			if (!pc.Homunculus.HasContract) { pc.ServerMessage("You have no homunculus contract."); return Task.CompletedTask; }
			if (Sabine.Zone.Skills.Homunculi.HomunculusService.Spawn(pc))
				pc.ServerMessage($"{pc.Homunculus.Name} answers your call.");
			else
				pc.ServerMessage("Your homunculus cannot be called right now.");
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AM_REST)]
	public class HomunculusRestHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is PlayerCharacter pc)
			{
				Sabine.Zone.Skills.Homunculi.HomunculusService.Rest(pc);
				pc.ServerMessage("Your homunculus is resting.");
			}
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AM_DRILLMASTER)]
	public class DrillMasterHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}

	[SkillHandler(SkillId.AM_HEALHOMUN)]
	public class HealHomunculusHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is PlayerCharacter pc && pc.Homunculus.HasContract)
			{
				var heal = 50 + 100 * skill.Level;
				pc.Homunculus.Heal(heal);
				pc.ServerMessage($"Healed {pc.Homunculus.Name} for {heal}.");
			}
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.AM_RESURRECTHOMUN)]
	public class ResurrectHomunculusHandler : ISkillHandler
	{
		// eAthena AM_RESURRECTHOMUN: revives a dead homunculus.
		// Consumes one Embryo.
		private const int EmbryoItemId = 7142;
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (caster is not PlayerCharacter pc) return Task.CompletedTask;
			if (!pc.Homunculus.HasContract) { pc.ServerMessage("No homunculus contract."); return Task.CompletedTask; }

			var embryo = pc.Inventory.GetItems(static i => i.ClassId == EmbryoItemId).FirstOrDefault();
			if (embryo == null) { pc.ServerMessage("You need an Embryo."); return Task.CompletedTask; }
			pc.Inventory.DecrementItem(embryo, 1);

			if (pc.Homunculus.Resurrect())
				pc.ServerMessage($"{pc.Homunculus.Name} has been resurrected.");
			else
				pc.ServerMessage("Your homunculus is not dead.");

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}
}
