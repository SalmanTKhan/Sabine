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

			// eAthena bSPrecovRate: per-percent SP regen tick bonus. Cards
			// (Vitata / Coco) and consumables write here through
			// AddBonus(SpRecovRate, ...).
			var ratePct = this.Character.Modifiers.BonusSpRecovRate;
			if (ratePct != 0)
				amount = MathF.Floor(amount * (100 + ratePct) / 100f);

			// SC_MAGNIFICAT (PR_MAGNIFICAT): SP regen rate doubled while
			// active. eAthena status.c applies the multiplier on the
			// final tick amount.
			if (this.Character.StatusEffects?.Has(StatusId.Magnificat) == true)
				amount *= 2;

			this.Character.HealSp((int)amount);
		}
	}
}
