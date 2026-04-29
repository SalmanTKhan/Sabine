using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Skills.Handlers.Sage
{
	[SkillHandler(SkillId.SA_ABRACADABRA)]
	public class AbracadabraHandler : ISkillHandler
	{
		// eAthena SA_ABRACADABRA (Hocus Pocus): rolls a random outcome
		// from the SA_* outcome list. v1 prints the chosen effect;
		// the per-effect implementations live below.
		private static readonly SkillId[] Outcomes = new[]
		{
			SkillId.SA_MONOCELL, SkillId.SA_CLASSCHANGE, SkillId.SA_SUMMONMONSTER,
			SkillId.SA_REVERSEORCISH, SkillId.SA_DEATH, SkillId.SA_FORTUNE,
			SkillId.SA_TAMINGMONSTER, SkillId.SA_QUESTION, SkillId.SA_GRAVITY,
			SkillId.SA_LEVELUP, SkillId.SA_INSTANTDEATH, SkillId.SA_FULLRECOVERY,
			SkillId.SA_COMA,
		};

		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			var pick = Outcomes[RandomProvider.Get().Next(Outcomes.Length)];
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, pick, skill.Level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.ServerMessage($"Abracadabra rolled: {pick}");
			return Task.CompletedTask;
		}
	}

	// All Hocus Pocus outcomes are stubs in v1; they get a registered
	// handler so the skill DB can reference them, but the effect is
	// printed only.

	internal static class HocusPocusStub
	{
		public static Task Run(Character caster, Skill skill, string effect)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			if (caster is PlayerCharacter pc)
				pc.ServerMessage($"Hocus Pocus: {effect} (not yet implemented).");
			return Task.CompletedTask;
		}
	}

	[SkillHandler(SkillId.SA_MONOCELL)]
	public class MonocellHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Monocell");
	}
	[SkillHandler(SkillId.SA_CLASSCHANGE)]
	public class ClassChangeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Class Change");
	}
	[SkillHandler(SkillId.SA_SUMMONMONSTER)]
	public class SummonMonsterHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Summon Monster");
	}
	[SkillHandler(SkillId.SA_REVERSEORCISH)]
	public class ReverseOrcishHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Reverse Orcish");
	}
	[SkillHandler(SkillId.SA_DEATH)]
	public class DeathHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Death");
	}
	[SkillHandler(SkillId.SA_FORTUNE)]
	public class FortuneHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Fortune");
	}
	[SkillHandler(SkillId.SA_TAMINGMONSTER)]
	public class TamingMonsterHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Taming Monster");
	}
	[SkillHandler(SkillId.SA_QUESTION)]
	public class QuestionHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Question");
	}
	[SkillHandler(SkillId.SA_GRAVITY)]
	public class GravityHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Gravity");
	}
	[SkillHandler(SkillId.SA_LEVELUP)]
	public class LevelUpHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Level Up");
	}
	[SkillHandler(SkillId.SA_INSTANTDEATH)]
	public class InstantDeathHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Instant Death");
	}
	[SkillHandler(SkillId.SA_FULLRECOVERY)]
	public class FullRecoveryHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is Character t)
			{
				t.HealHp(t.Parameters.HpMax);
				t.Parameters.Modify(ParameterType.Sp, t.Parameters.SpMax);
			}
			Send.ZC_NOTIFY_SKILL(caster, target?.Handle ?? caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}
	[SkillHandler(SkillId.SA_COMA)]
	public class ComaHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => HocusPocusStub.Run(caster, skill, "Coma");
	}
}
