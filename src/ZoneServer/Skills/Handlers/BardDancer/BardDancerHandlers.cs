using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.BardDancer
{
	internal static class SongPlacement
	{
		public static Task PlaceAura(Character caster, Skill skill, StatusId applied, bool affectsEnemies = false)
		{
			var center = caster.Position;
			for (var dx = -3; dx <= 3; dx++)
			{
				for (var dy = -3; dy <= 3; dy++)
				{
					var pos = new Sabine.Shared.World.Position((short)(center.X + dx), (short)(center.Y + dy));
					caster.Map.AddSkillUnit(new SongUnit(caster, pos, skill.Id, skill.Level, applied, affectsEnemies));
				}
			}
			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, center.X, center.Y, 0);
			return Task.CompletedTask;
		}
	}

	// Mastery passives.
	[SkillHandler(SkillId.BA_MUSICALLESSON)]
	public class MusicalLessonHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}
	[SkillHandler(SkillId.DC_DANCINGLESSON)]
	public class DancingLessonHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill) => Task.CompletedTask;
	}
	[SkillHandler(SkillId.BD_ADAPTATION)]
	public class AdaptationHandler : ISkillHandler
	{
		// eAthena BD_ADAPTATION (Adaptation to Circumstances): cancels
		// any active song / dance the caster is performing. v1: stub.
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}
	[SkillHandler(SkillId.BD_ENCORE)]
	public class EncoreHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			return Task.CompletedTask;
		}
	}

	// Bard solos.
	[SkillHandler(SkillId.BA_MUSICALSTRIKE)]
	public class MusicalStrikeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			var ratio = 1.0f + 0.40f * skill.Level;
			var ctx = new Sabine.Zone.Battle.AttackContext(caster, target)
			{
				SkillId = skill.Id, SkillLevel = skill.Level,
				Kind = Sabine.Zone.Battle.AttackKind.Physical,
				SkillRatio = ratio, IsLongRange = true, WeaponRequired = true,
			};
			var result = Sabine.Zone.Battle.BattleCalculator.Calc(ctx);
			if (!result.IsMiss) target.TakeDamage(result.Damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 1, result.ActionType);
			return Task.CompletedTask;
		}
	}
	[SkillHandler(SkillId.BA_DISSONANCE)]
	public class DissonanceHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong, affectsEnemies: true);
	}
	[SkillHandler(SkillId.BA_FROSTJOKER)]
	public class FrostJokerHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, 7))
			{
				if (!enemy.IsHostileTo(caster)) continue;
				if (Yggdrasil.Util.RandomProvider.Get().Next(100) < (50 + 5 * skill.Level))
					enemy.StatusEffects.Start(StatusId.Freeze, skill.Level, System.TimeSpan.FromSeconds(3 + skill.Level), caster);
			}
			return Task.CompletedTask;
		}
	}
	[SkillHandler(SkillId.BA_WHISTLE)]
	public class WhistleHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong);
	}
	[SkillHandler(SkillId.BA_ASSASSINCROSS)]
	public class AssassinCrossOfSunsetHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.PowerChord);
	}
	[SkillHandler(SkillId.BA_POEMBRAGI)]
	public class PoemOfBragiHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong);
	}
	[SkillHandler(SkillId.BA_APPLEIDUN)]
	public class AppleOfIdunHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong);
	}

	// Dancer solos.
	[SkillHandler(SkillId.DC_THROWARROW)]
	public class ArrowVulcanHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			var ratio = 2.0f + 1.0f * skill.Level;
			var ctx = new Sabine.Zone.Battle.AttackContext(caster, target)
			{
				SkillId = skill.Id, SkillLevel = skill.Level,
				Kind = Sabine.Zone.Battle.AttackKind.Physical,
				SkillRatio = ratio, IsLongRange = true, WeaponRequired = true,
			};
			var result = Sabine.Zone.Battle.BattleCalculator.Calc(ctx);
			if (!result.IsMiss) target.TakeDamage(result.Damage, caster);
			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, result.Damage, 0, 1, result.ActionType);
			return Task.CompletedTask;
		}
	}
	[SkillHandler(SkillId.DC_UGLYDANCE)]
	public class UglyDanceHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.DancerDance, affectsEnemies: true);
	}
	[SkillHandler(SkillId.DC_SCREAM)]
	public class ScreamHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);
			foreach (var enemy in caster.Map.GetCharactersInRange(caster.Position, 7))
			{
				if (!enemy.IsHostileTo(caster)) continue;
				if (Yggdrasil.Util.RandomProvider.Get().Next(100) < (25 + 5 * skill.Level))
					enemy.StatusEffects.Start(StatusId.Stun, skill.Level, System.TimeSpan.FromSeconds(3 + skill.Level), caster);
			}
			return Task.CompletedTask;
		}
	}
	[SkillHandler(SkillId.DC_HUMMING)]
	public class HummingHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.DancerDance);
	}
	[SkillHandler(SkillId.DC_DONTFORGETME)]
	public class DontForgetMeHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.DancerDance, affectsEnemies: true);
	}
	[SkillHandler(SkillId.DC_FORTUNEKISS)]
	public class FortuneKissHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.FortuneKiss);
	}
	[SkillHandler(SkillId.DC_SERVICEFORYOU)]
	public class ServiceForYouHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.ServiceForYou);
	}

	// Ensemble (BD_*) — require Bard + Dancer adjacency. v1 just
	// places the aura; the adjacency check is a follow-up.
	[SkillHandler(SkillId.BD_LULLABY)]
	public class LullabyHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.Lullaby, affectsEnemies: true);
	}
	[SkillHandler(SkillId.BD_RICHMANKIM)]
	public class MrKimAMillionaireHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong);
	}
	[SkillHandler(SkillId.BD_ETERNALCHAOS)]
	public class EternalChaosHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong, affectsEnemies: true);
	}
	[SkillHandler(SkillId.BD_DRUMBATTLEFIELD)]
	public class DrumOnTheBattlefieldHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong);
	}
	[SkillHandler(SkillId.BD_RINGNIBELUNGEN)]
	public class RingOfNibelungenHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong);
	}
	[SkillHandler(SkillId.BD_ROKISWEIL)]
	public class LokisVeilHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.MentalSensing);
	}
	[SkillHandler(SkillId.BD_INTOABYSS)]
	public class IntoTheAbyssHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong);
	}
	[SkillHandler(SkillId.BD_SIEGFRIED)]
	public class InvulnerableSiegfriedHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong);
	}
	[SkillHandler(SkillId.BD_RAGNAROK)]
	public class RagnarokHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
			=> SongPlacement.PlaceAura(caster, skill, StatusId.BardSong);
	}
}
