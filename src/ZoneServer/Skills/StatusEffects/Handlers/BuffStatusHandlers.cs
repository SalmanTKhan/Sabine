using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_ENDURE — survives stagger from physical damage; flag-only.
	/// Combat code queries <c>StatusEffects.Has(Endure)</c> when deciding
	/// whether to apply stun on hit. eAthena: Val1 = level, no stat
	/// modification beyond +MDEF (Val2 = level) which we skip in v1.
	/// </summary>
	[StatusEffectHandler(StatusId.Endure)]
	public class EndureStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_CONCENTRATION (AC_CONCENTRATION classic) — +HIT and +arrow
	/// damage. eAthena: Val2 = 3 * level (HIT). Arrow-damage component
	/// is folded into the shared bonus modifiers.
	/// </summary>
	[StatusEffectHandler(StatusId.Concentration)]
	public class ConcentrationStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			var hitBonus = 3 * effect.Level;
			effect.Val2 = hitBonus;
			target.AddBonusStat(ParameterType.Hit, hitBonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Hit, -effect.Val2);
		}
	}

	/// <summary>
	/// SC_LOUD (MC_LOUD) — +4 to all stats. eAthena: Val1 = 4 (flat
	/// stat bonus regardless of skill level).
	/// </summary>
	[StatusEffectHandler(StatusId.LoudExclamation)]
	public class LoudExclamationStatusHandler : IStatusEffectHandler
	{
		private const int StatBonus = 4;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = StatBonus;
			target.Parameters.Modify(ParameterType.Str, StatBonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			target.Parameters.Modify(ParameterType.Str, -effect.Val2);
		}
	}

	/// <summary>
	/// SC_SIGHT (MG_SIGHT) — flag-only buff: while active the caster
	/// reveals nearby Hiding/Cloaking enemies. The reveal sweep happens
	/// at skill cast (handled by the SightHandler skill); the status
	/// itself is just a duration marker.
	/// </summary>
	[StatusEffectHandler(StatusId.Sight)]
	public class SightStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_HIDING (TF_HIDING) — flips the OPTION_HIDE bit so the client
	/// renders the hide silhouette. eAthena status.c flips
	/// <c>sd->sc.option |= OPTION_HIDE</c> on start and clears it on end;
	/// AI consumers use <see cref="Character.IsHidden"/> for the gameplay
	/// check, but the visible "hidden" sprite swap is driven by the
	/// option bit being broadcast in actor spawn / state-change packets.
	/// </summary>
	[StatusEffectHandler(StatusId.Hiding)]
	public class HidingStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> target.SetOptionFlag(OptionState.Hide, true);

		public void OnEnd(Character target, StatusEffect effect)
			=> target.SetOptionFlag(OptionState.Hide, false);
	}

	/// <summary>
	/// SC_CLOAKING (AS_CLOAKING) — flips the OPTION_CLOAK bit so the
	/// client renders the Assassin's cloak shimmer. Mirrors eAthena
	/// status.c — gameplay logic (movement-while-cloaked next to walls,
	/// reveal on attack) is owned by the skill handler and combat
	/// listeners; this just owns the visual flag so equip / unequip is
	/// symmetric with <see cref="HidingStatusHandler"/>.
	/// </summary>
	[StatusEffectHandler(StatusId.Cloaking)]
	public class CloakingStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
			=> target.SetOptionFlag(OptionState.Cloak, true);

		public void OnEnd(Character target, StatusEffect effect)
			=> target.SetOptionFlag(OptionState.Cloak, false);
	}

	/// <summary>
	/// SC_TRICKDEAD (NV_TRICKDEAD) — marker registration. The lay-down
	/// pose isn't an opt1 / opt2 / option bit on the alpha-era client
	/// (eAthena status.h has no OPT1_LAYDOWN), so the visual is driven
	/// elsewhere (movement controller, AI hostility check, hp-gain
	/// auto-cancel in <see cref="Character.Heal"/>). Registering an
	/// explicit handler closes the dispatch table and gives future
	/// pose-broadcast work a clear hook.
	/// </summary>
	[StatusEffectHandler(StatusId.TrickDead)]
	public class TrickDeadStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_FORTUNE — Sage Hocus Pocus "Fortune" outcome grants the caster
	/// a flat LUK aura. eAthena status.c uses SCB_LUK with val2 = 20 for
	/// a flat 60s window (skill.c SA_FORTUNE).
	/// </summary>
	[StatusEffectHandler(StatusId.Fortune)]
	public class FortuneStatusHandler : IStatusEffectHandler
	{
		private const int LukBonus = 20;

		public void OnStart(Character target, StatusEffect effect)
		{
			effect.Val2 = LukBonus;
			target.AddBonusStat(ParameterType.Luk, LukBonus);
		}

		public void OnEnd(Character target, StatusEffect effect)
		{
			if (effect.Val2 != 0)
				target.AddBonusStat(ParameterType.Luk, -effect.Val2);
		}
	}

	/// <summary>
	/// SC_GRAVITATION — Sage Hocus Pocus "Gravity" outcome anchors the
	/// target so it can't move or act. <see cref="Character.IsImmobilized"/>
	/// reads the flag — no body-state opt1 entry exists for gravity, so
	/// no <c>SetBodyState</c> call here. eAthena status.c SC_GRAVITATION.
	/// </summary>
	[StatusEffectHandler(StatusId.Gravity)]
	public class GravityStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}

	/// <summary>
	/// SC_STOP — flag-only handler. The actual movement root is read by
	/// <see cref="Sabine.Zone.World.Actors.Character.IsRooted"/> and
	/// short-circuited in <c>MovementController.MoveTo</c>; the status
	/// handler exists so the standard sc lifecycle (start / expire /
	/// dispel) drives the flag on and off through the same pipeline as
	/// every other status. Inflicted by CH_TIGERFIST and the Tarot
	/// Wheel of Fortune outcome today.
	/// </summary>
	[StatusEffectHandler(StatusId.Stop)]
	public class StopStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect)
		{
			target.StopAttacking();
		}

		public void OnEnd(Character target, StatusEffect effect) { }
	}
}
