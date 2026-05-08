using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// A component that manages a character's parameter regeneration.
	/// </summary>
	public class RegenComponent : ICharacterComponent
	{
		private readonly static TimeSpan HpRegenDelayNormal = TimeSpan.FromSeconds(6);
		private readonly static TimeSpan HpRegenDelaySitting = TimeSpan.FromSeconds(3);
		private readonly static TimeSpan SpRegenDelayNormal = TimeSpan.FromSeconds(8);
		private readonly static TimeSpan SpRegenDelaySitting = TimeSpan.FromSeconds(4);

		private TimeSpan _hpTimer, _spTimer;

		/// <summary>
		/// Returns the character this component belongs to.
		/// </summary>
		public PlayerCharacter Character { get; }

		/// <summary>
		/// Returns the character this component belongs to.
		/// </summary>
		Character ICharacterComponent.Character => this.Character;

		/// <summary>
		/// Creates new instance.
		/// </summary>
		/// <param name="character"></param>
		public RegenComponent(PlayerCharacter character)
		{
			this.Character = character;
		}

		/// <summary>
		/// Updates the component, regenerating the character's parameters
		/// if necessary.
		/// </summary>
		/// <param name="elapsed"></param>
		public void Update(TimeSpan elapsed)
		{
			if (this.Character.IsDead)
				return;

			var hpDelay = this.Character.State == CharacterState.Sitting ? HpRegenDelaySitting : HpRegenDelayNormal;
			var spDelay = this.Character.State == CharacterState.Sitting ? SpRegenDelaySitting : SpRegenDelayNormal;

			_hpTimer += elapsed;
			if (_hpTimer >= hpDelay)
			{
				_hpTimer = TimeSpan.Zero;
				this.RegenHp();
			}

			_spTimer += elapsed;
			if (_spTimer >= spDelay)
			{
				_spTimer = TimeSpan.Zero;
				this.RegenSp();
			}
		}

		/// <summary>
		/// Executes an HP regen tick.
		/// </summary>
		public void RegenHp()
		{
			var hpMax = this.Character.Parameters.HpMax;
			var vit = this.Character.Parameters.Vit;

			var amount = MathF.Max(1, MathF.Floor(hpMax / 200f));
			amount += MathF.Floor(vit / 5f);

			// eAthena bHPrecovRate (item / card scripts): adds a flat percent
			// to the natural HP regen tick. Magnificat / Increase HP Recovery
			// don't go through this bucket — they're applied as separate
			// multipliers below — so a +5% card stacks additively with them.
			var ratePct = this.Character.Modifiers.BonusHpRecovRate;
			if (ratePct != 0)
				amount = MathF.Floor(amount * (100 + ratePct) / 100f);

			// SM_RECOVERY (Increase HP Recovery) — flat additive bonus that
			// rides outside of bHPrecovRate, mirroring eAthena status.c:2616
			// where it's seeded into the separate skill-regen bucket.
			//   per tick = level*5 + level * MaxHp / 500
			var hpRecov = this.Character.Skills?.GetLevel(SkillId.SM_RECOVERY) ?? 0;
			if (hpRecov > 0)
				amount += hpRecov * 5 + hpRecov * hpMax / 500f;

			this.Character.HealHp((int)amount);
		}

		/// <summary>
		/// Executes an SP regen tick.
		/// </summary>
		public void RegenSp()
		{
			var spMax = this.Character.Parameters.SpMax;
			var int_ = this.Character.Parameters.Int;

			var amount = 1f;
			amount += MathF.Floor(spMax / 100f);
			amount += MathF.Floor(int_ / 6f);

			if (int_ >= 120)
			{
				amount += MathF.Floor(int_ / 2f - 56f);
			}

			// HP_MEDITATIO (Meditatio): +3% per level multiplicative bump
			// on the natural SP regen tick. eAthena status.c:2607 applies
			// it before any flat skill add and before bSPrecovRate, so
			// pre-bake it into the natural amount.
			var meditatio = this.Character.Skills?.GetLevel(SkillId.HP_MEDITATIO) ?? 0;
			if (meditatio > 0)
				amount = MathF.Floor(amount * (100 + 3 * meditatio) / 100f);

			// eAthena bSPrecovRate: per-percent SP regen tick bonus. Cards
			// (Vitata / Coco) and consumables write here through
			// AddBonus(SpRecovRate, ...).
			var ratePct = this.Character.Modifiers.BonusSpRecovRate;
			if (ratePct != 0)
				amount = MathF.Floor(amount * (100 + ratePct) / 100f);

			// MG_SRECOVERY (Increase SP Recovery) — flat additive bonus
			// independent of bSPrecovRate. Mirrors eAthena status.c:2621.
			//   per tick = level*3 + level * MaxSp / 500
			var spRecov = this.Character.Skills?.GetLevel(SkillId.MG_SRECOVERY) ?? 0;
			if (spRecov > 0)
				amount += spRecov * 3 + spRecov * spMax / 500f;

			// SC_MAGNIFICAT (PR_MAGNIFICAT): SP regen rate doubled while
			// active. eAthena status.c applies the multiplier on the
			// final tick amount.
			if (this.Character.StatusEffects?.Has(StatusId.Magnificat) == true)
				amount *= 2;

			this.Character.HealSp((int)amount);
		}
	}
}
