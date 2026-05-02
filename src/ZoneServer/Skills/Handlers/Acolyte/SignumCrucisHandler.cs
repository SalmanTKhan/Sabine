using System;
using System.Linq;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_CRUCIS)]
	public class SignumCrucisHandler : ITargetedSkillHandler
	{
		// eAthena classic AL_CRUCIS: 14% + 4%/level DEF reduction on
		// every Undead/Demon enemy in screen range. Models the debuff
		// as a Provoke-flavoured status: piggybacks on the existing
		// Provoke handler so the DEF reduction reverts cleanly when
		// the duration expires.
		// TODO: dedicated StatusId.SignumCrucis instead of Provoke.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			var radius = 10;
			var targets = caster.Map.GetCharactersInRange(caster.Position, radius)
				.Where(c => c.IsHostileTo(caster) && IsUndeadOrDemon(c));

			var duration = TimeSpan.FromSeconds(35);
			foreach (var tgt in targets)
				tgt.StatusEffects.Start(StatusId.Provoke, level, duration, caster, level);
		}

		private static bool IsUndeadOrDemon(Character character)
		{
			if (character is not Monster monster) return false;
			return monster.Data.Race == RaceType.Undead || monster.Data.Race == RaceType.Demon
				|| monster.Data.Element == ElementType.Undead;
		}
	}
}
