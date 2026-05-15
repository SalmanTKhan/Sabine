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

		/// <summary>
		/// Ensemble (BD_*) adjacency check. eAthena requires a Bard and
		/// Dancer (or trans equivalents Clown / Gypsy) standing within
		/// 1 cell of each other before the ensemble skill resolves;
		/// failure cancels the cast with a fail packet. Returns true
		/// when the partner is found, false otherwise. The eA gender-
		/// specific gating (Bard ↔ Dancer only, no Bard ↔ Bard) is
		/// preserved here.
		/// </summary>
		public static bool HasAdjacentPartner(Sabine.Zone.World.Actors.PlayerCharacter caster)
		{
			if (caster?.Map == null) return false;

			var isBardSide = caster.JobId == Sabine.Shared.Const.JobId.Bard
				|| caster.JobId == Sabine.Shared.Const.JobId.Clown
				|| caster.JobId == Sabine.Shared.Const.JobId.BabyBard;
			var isDancerSide = caster.JobId == Sabine.Shared.Const.JobId.Dancer
				|| caster.JobId == Sabine.Shared.Const.JobId.Gypsy
				|| caster.JobId == Sabine.Shared.Const.JobId.BabyDancer;

			if (!isBardSide && !isDancerSide) return false;

			foreach (var c in caster.Map.GetCharactersInRange(caster.Position, 1))
			{
				if (c == caster) continue;
				if (c is not Sabine.Zone.World.Actors.PlayerCharacter other) continue;

				var partnerIsBard = other.JobId == Sabine.Shared.Const.JobId.Bard
					|| other.JobId == Sabine.Shared.Const.JobId.Clown
					|| other.JobId == Sabine.Shared.Const.JobId.BabyBard;
				var partnerIsDancer = other.JobId == Sabine.Shared.Const.JobId.Dancer
					|| other.JobId == Sabine.Shared.Const.JobId.Gypsy
					|| other.JobId == Sabine.Shared.Const.JobId.BabyDancer;

				if (isBardSide && partnerIsDancer) return true;
				if (isDancerSide && partnerIsBard) return true;
			}
			return false;
		}

		/// <summary>
		/// Wrapper used by BD_* ensemble handlers — places the aura iff
		/// a valid partner is adjacent. Otherwise emits a server-side
		/// fail message (no client fail packet on alpha-era) and burns
		/// no SP — the SP-deduction site already charged the cost,
		/// so on a real failure this would refund. Today we just log;
		/// the refund lands when a "post-handler refund" hook ships.
		/// </summary>
		public static bool PlaceEnsembleAura(UseSkillParams p, StatusId applied, bool affectsEnemies = false)
		{
			if (p.Character is not Sabine.Zone.World.Actors.PlayerCharacter pc)
				return false;

			if (!HasAdjacentPartner(pc))
			{
				pc.ServerMessage(Sabine.Shared.Util.Localization.Get("Need an opposite-gender performer adjacent."));
				return false;
			}

			PlaceAura(p, applied, affectsEnemies);
			return true;
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
		// eAthena BD_ADAPTATION: ends the SC_DANCING status, which on
		// renewal also tears down any active song/dance unit. Sabine's
		// song state lives entirely on the SongUnit, so the equivalent
		// is removing the caster's own SongUnit(s) from the map.
		public void Handle(UseSkillParams parameters)
		{
			var caster = parameters.Character;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			Send.ZC_NOTIFY_SKILL(caster, caster.Handle, skill.Id, level, 0, 0, 0, ActionType.Skill);

			if (caster.Map == null)
				return;

			foreach (var unit in caster.Map.GetSkillUnitsByOwner(caster))
			{
				if (unit is SongUnit)
					caster.Map.RemoveSkillUnit(unit);
			}
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
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.Dissonance, affectsEnemies: true);
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
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.Whistle);
	}

	[SkillHandler(SkillId.BA_ASSASSINCROSS)]
	public class AssassinCrossOfSunsetHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.AssassinCross);
	}

	[SkillHandler(SkillId.BA_POEMBRAGI)]
	public class PoemOfBragiHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.PoemBragi);
	}

	[SkillHandler(SkillId.BA_APPLEIDUN)]
	public class AppleOfIdunHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.AppleIdun);
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
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.UglyDance, affectsEnemies: true);
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
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.Humming);
	}

	[SkillHandler(SkillId.DC_DONTFORGETME)]
	public class DontForgetMeHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceAura(p, StatusId.DontForgetMe, affectsEnemies: true);
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

	// Ensemble (BD_*) — require Bard + Dancer adjacency. PlaceEnsembleAura
	// gates each cast on a HasAdjacentPartner check; on failure the cast
	// fizzles with a server-side message (the alpha-era profile lacks a
	// proper fail packet for these).
	[SkillHandler(SkillId.BD_LULLABY)]
	public class LullabyHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceEnsembleAura(p, StatusId.Lullaby, affectsEnemies: true);
	}

	[SkillHandler(SkillId.BD_RICHMANKIM)]
	public class MrKimAMillionaireHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceEnsembleAura(p, StatusId.RichmanKim);
	}

	[SkillHandler(SkillId.BD_ETERNALCHAOS)]
	public class EternalChaosHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceEnsembleAura(p, StatusId.BardSong, affectsEnemies: true);
	}

	[SkillHandler(SkillId.BD_DRUMBATTLEFIELD)]
	public class DrumOnTheBattlefieldHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceEnsembleAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BD_RINGNIBELUNGEN)]
	public class RingOfNibelungenHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceEnsembleAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BD_ROKISWEIL)]
	public class LokisVeilHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceEnsembleAura(p, StatusId.MentalSensing);
	}

	[SkillHandler(SkillId.BD_INTOABYSS)]
	public class IntoTheAbyssHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceEnsembleAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BD_SIEGFRIED)]
	public class InvulnerableSiegfriedHandler : ITargetedSkillHandler
	{
		public void Handle(UseSkillParams p) => SongPlacement.PlaceEnsembleAura(p, StatusId.BardSong);
	}

	[SkillHandler(SkillId.BD_RAGNAROK)]
	public class RagnarokHandler : ITargetedSkillHandler
	{
		// BD_RAGNAROK: "Ragnarok Online" ensemble strips every dispellable
		// buff from every character (ally or enemy) inside the aura
		// footprint. eAthena fires status_change_clear_buffs on touch.
		public void Handle(UseSkillParams p)
		{
			SongPlacement.PlaceEnsembleAura(p, StatusId.BardSong);

			var caster = p.Character;
			foreach (var c in caster.Map.GetCharactersInRange(caster.Position, 3))
			{
				if (c == caster) continue;
				c.StatusEffects?.DispelCategory(Sabine.Zone.Skills.StatusEffects.StatusCategory.Buff);
			}
		}
	}
}
