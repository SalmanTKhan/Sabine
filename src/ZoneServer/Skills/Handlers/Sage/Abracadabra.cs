using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Sage
{
	[SkillHandler(SkillId.SA_ABRACADABRA)]
	public class AbracadabraHandler : ITargetedSkillHandler
	{
		// eAthena SA_ABRACADABRA: rolls a random outcome.
		private static readonly SkillId[] Outcomes = new[]
		{
			SkillId.SA_MONOCELL, SkillId.SA_CLASSCHANGE, SkillId.SA_SUMMONMONSTER,
			SkillId.SA_REVERSEORCISH, SkillId.SA_DEATH, SkillId.SA_FORTUNE,
			SkillId.SA_TAMINGMONSTER, SkillId.SA_QUESTION, SkillId.SA_GRAVITY,
			SkillId.SA_LEVELUP, SkillId.SA_INSTANTDEATH, SkillId.SA_FULLRECOVERY,
			SkillId.SA_COMA,
		};

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			var pick = Outcomes[RandomProvider.Get().Next(Outcomes.Length)];
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, pick, level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.ServerMessage($"Abracadabra rolled: {pick}");
		}
	}

	internal static class HocusPocusStub
	{
		// SA_MONOCELL / SA_CLASSCHANGE / SA_REVERSEORCISH / SA_QUESTION /
		// SA_GRAVITY still need name-override or job-swap infra Sabine
		// doesn't expose yet, so they only emit the cast frame and tell
		// the caster what was rolled.
		public static void Run(UseSkillParams parameters, string effect)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.ServerMessage($"Hocus Pocus: {effect} (not yet implemented).");
		}
	}

	[SkillHandler(SkillId.SA_MONOCELL)] public class MonocellHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Monocell"); }
	[SkillHandler(SkillId.SA_CLASSCHANGE)] public class ClassChangeHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Class Change"); }
	[SkillHandler(SkillId.SA_REVERSEORCISH)] public class ReverseOrcishHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Reverse Orcish"); }
	[SkillHandler(SkillId.SA_QUESTION)] public class QuestionHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Question"); }
	[SkillHandler(SkillId.SA_GRAVITY)] public class GravityHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Gravity"); }

	/// <summary>SA_DEATH — instakill on a non-MVP target.</summary>
	[SkillHandler(SkillId.SA_DEATH)]
	public class DeathHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, target?.Handle ?? caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			// eAthena skill.c SA_DEATH: skill_castend_nodamage_id
			// calls status_kill — boss-class mobs ignore it.
			if (target == null || target.IsDead) return;
			if (target is Monster m && m.Data.MvpExp > 0) return;

			target.Kill(caster);
		}
	}

	/// <summary>SA_INSTANTDEATH — kills the caster.</summary>
	[SkillHandler(SkillId.SA_INSTANTDEATH)]
	public class InstantDeathHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			caster.Kill(caster);
		}
	}

	/// <summary>SA_COMA — drops a non-MVP target's HP to 1.</summary>
	[SkillHandler(SkillId.SA_COMA)]
	public class ComaHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, target?.Handle ?? caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (target == null || target.IsDead) return;
			if (target is Monster m && m.Data.MvpExp > 0) return;

			var damage = target.Parameters.Hp - 1;
			if (damage > 0)
				target.TakeDamage(damage, caster);
		}
	}

	/// <summary>SA_FORTUNE — caster gains +20 LUK for 60s.</summary>
	[SkillHandler(SkillId.SA_FORTUNE)]
	public class FortuneHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			caster.StatusEffects.Start(StatusId.Fortune, level, TimeSpan.FromSeconds(60), caster);
		}
	}

	/// <summary>
	/// SA_LEVELUP — eAthena rolls a fake level-up effect; the visible
	/// player benefit is a full HP/SP refill on the caster, which we
	/// keep parity-minimal here.
	/// </summary>
	[SkillHandler(SkillId.SA_LEVELUP)]
	public class LevelUpHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			caster.HealHp(caster.Parameters.HpMax);
			caster.Parameters.Modify(ParameterType.Sp, caster.Parameters.SpMax);
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.SA_FULLRECOVERY)]
	public class FullRecoveryHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target != null)
			{
				target.HealHp(target.Parameters.HpMax);
				target.Parameters.Modify(ParameterType.Sp, target.Parameters.SpMax);
			}
			Send.ZC_NOTIFY_SKILL(caster, target?.Handle ?? caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	/// <summary>SA_TAMINGMONSTER — drops a Pet Food (item 537) at caster.</summary>
	[SkillHandler(SkillId.SA_TAMINGMONSTER)]
	public class TamingMonsterHandler : ITargetedSkillHandler
	{
		private const int PetFoodItemId = 537;

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.Inventory.AddItem(new Item(PetFoodItemId, 1));
		}
	}

	/// <summary>SA_SUMMONMONSTER — spawns one low-tier mob next to the caster.</summary>
	[SkillHandler(SkillId.SA_SUMMONMONSTER)]
	public class SummonMonsterHandler : ITargetedSkillHandler
	{
		private static readonly IdentityId[] Roster = new[]
		{
			IdentityId.JT_PORING, IdentityId.JT_LUNATIC, IdentityId.JT_FABRE,
			IdentityId.JT_CHONCHON, IdentityId.JT_PUPA, IdentityId.JT_DROPS,
		};

		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var pick = Roster[RandomProvider.Get().Next(Roster.Length)];
			if (!ZoneServer.Instance.Data.Monsters.Contains(pick))
				return;

			var monster = new Monster(pick);
			monster.Warp(caster.MapId, caster.Position.GetRandomInRange(2));
		}
	}
}
