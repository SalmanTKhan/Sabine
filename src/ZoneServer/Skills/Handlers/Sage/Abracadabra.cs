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
	[SkillHandler(SkillId.SA_SUMMONMONSTER)] public class SummonMonsterHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Summon Monster"); }
	[SkillHandler(SkillId.SA_REVERSEORCISH)] public class ReverseOrcishHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Reverse Orcish"); }
	[SkillHandler(SkillId.SA_DEATH)] public class DeathHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Death"); }
	[SkillHandler(SkillId.SA_FORTUNE)] public class FortuneHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Fortune"); }
	[SkillHandler(SkillId.SA_TAMINGMONSTER)] public class TamingMonsterHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Taming Monster"); }
	[SkillHandler(SkillId.SA_QUESTION)] public class QuestionHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Question"); }
	[SkillHandler(SkillId.SA_GRAVITY)] public class GravityHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Gravity"); }
	[SkillHandler(SkillId.SA_LEVELUP)] public class LevelUpHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Level Up"); }
	[SkillHandler(SkillId.SA_INSTANTDEATH)] public class InstantDeathHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Instant Death"); }

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

	[SkillHandler(SkillId.SA_COMA)] public class ComaHandler : ITargetedSkillHandler { public void Handle(UseSkillParams p) => HocusPocusStub.Run(p, "Coma"); }
}
