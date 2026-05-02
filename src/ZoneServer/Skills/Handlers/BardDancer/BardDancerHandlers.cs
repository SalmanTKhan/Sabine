using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.BardDancer
{
	internal static class SongPlacement
	{
		public static void PlaceAura(UseSkillParams p, StatusId applied, bool affectsEnemies = false)
		{
			var caster = p.Character;
			var skill = p.Skill;
			var level = p.SkillLevel;
			var center = caster.Position;
			for (var dx = -3; dx <= 3; dx++)
			{
				for (var dy = -3; dy <= 3; dy++)
				{
					var pos = new Sabine.Shared.World.Position((short)(center.X + dx), (short)(center.Y + dy));
					caster.Map.AddSkillUnit(new SongUnit(caster, pos, skill.Id, level, applied, affectsEnemies));
				}
			}
			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);
		}
	}

	// Mastery passives.
	[SkillHandler(SkillId.BA_MUSICALLESSON)]
	public class MusicalLessonHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.DC_DANCINGLESSON)]
	public class DancingLessonHandler : ITargetedSkillHandler { public void Handle(UseSkillParams parameters) { } }

	[SkillHandler(SkillId.BD_ADAPTATION)]
	public class AdaptationHandler : ITargetedSkillHandler
	{
		// TODO: cancel any active song / dance the caster is performing.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	[SkillHandler(SkillId.BD_ENCORE)]
	public class EncoreHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
		}
	}

	// Bard solos.
	[SkillHandler(SkillId.BA_MUSICALSTRIKE)]
	public class MusicalStrikeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;
			var ratio = 1.0f + 0.40f * level;
			var ctx = new Sabine.Zone.Battle.AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = Sabine.Zone.Battle.AttackKind.Physical,
				SkillRatio = ratio,
				IsLongRange = true,
				WeaponRequired = true,
			};
			var result = Sabine.Zone.Battle.BattleCalculator.Calc(ctx);
			if (!result.IsMiss) target.TakeDamage(result.Damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	[SkillHandler(SkillId.BA_DISSONANCE)]
	public class DissonanceHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong, affectsEnemies: true);
	}

	[SkillHandler(SkillId.BA_FROSTJOKER)]
	public class FrostJokerHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, 7))
			{
				if (!enemy.IsHostileTo(caster)) continue;
				if (Yggdrasil.Util.RandomProvider.Get().Next(100) < (50 + 5 * level))
					enemy.StatusEffects.Start(StatusId.Freeze, level, System.TimeSpan.FromSeconds(3 + level), caster);
			}
		}
	}

	[SkillHandler(SkillId.BA_WHISTLE)]
	public class WhistleHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BA_ASSASSINCROSS)]
	public class AssassinCrossOfSunsetHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.PowerChord);
	}

	[SkillHandler(SkillId.BA_POEMBRAGI)]
	public class PoemOfBragiHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BA_APPLEIDUN)]
	public class AppleOfIdunHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong);
	}

	// Dancer solos.
	[SkillHandler(SkillId.DC_THROWARROW)]
	public class ArrowVulcanHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var target = parameters.Target;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (target == null) return;
			var ratio = 2.0f + 1.0f * level;
			var ctx = new Sabine.Zone.Battle.AttackContext(caster, target)
			{
				SkillId = skill.Id,
				SkillLevel = level,
				Kind = Sabine.Zone.Battle.AttackKind.Physical,
				SkillRatio = ratio,
				IsLongRange = true,
				WeaponRequired = true,
			};
			var result = Sabine.Zone.Battle.BattleCalculator.Calc(ctx);
			if (!result.IsMiss) target.TakeDamage(result.Damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, level, result.Damage, 0, 1, result.ActionType);
		}
	}

	[SkillHandler(SkillId.DC_UGLYDANCE)]
	public class UglyDanceHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.DancerDance, affectsEnemies: true);
	}

	[SkillHandler(SkillId.DC_SCREAM)]
	public class ScreamHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);
			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, 7))
			{
				if (!enemy.IsHostileTo(caster)) continue;
				if (Yggdrasil.Util.RandomProvider.Get().Next(100) < (25 + 5 * level))
					enemy.StatusEffects.Start(StatusId.Stun, level, System.TimeSpan.FromSeconds(3 + level), caster);
			}
		}
	}

	[SkillHandler(SkillId.DC_HUMMING)]
	public class HummingHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.DancerDance);
	}

	[SkillHandler(SkillId.DC_DONTFORGETME)]
	public class DontForgetMeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.DancerDance, affectsEnemies: true);
	}

	[SkillHandler(SkillId.DC_FORTUNEKISS)]
	public class FortuneKissHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.FortuneKiss);
	}

	[SkillHandler(SkillId.DC_SERVICEFORYOU)]
	public class ServiceForYouHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.ServiceForYou);
	}

	// Ensemble (BD_*) — require Bard + Dancer adjacency. v1 just
	// places the aura; the adjacency check is a follow-up.
	[SkillHandler(SkillId.BD_LULLABY)]
	public class LullabyHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.Lullaby, affectsEnemies: true);
	}

	[SkillHandler(SkillId.BD_RICHMANKIM)]
	public class MrKimAMillionaireHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BD_ETERNALCHAOS)]
	public class EternalChaosHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong, affectsEnemies: true);
	}

	[SkillHandler(SkillId.BD_DRUMBATTLEFIELD)]
	public class DrumOnTheBattlefieldHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BD_RINGNIBELUNGEN)]
	public class RingOfNibelungenHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BD_ROKISWEIL)]
	public class LokisVeilHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.MentalSensing);
	}

	[SkillHandler(SkillId.BD_INTOABYSS)]
	public class IntoTheAbyssHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BD_SIEGFRIED)]
	public class InvulnerableSiegfriedHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BD_RAGNAROK)]
	public class RagnarokHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.BardSong);
	}
}
