using System;
using Sabine.Shared.Const;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.World.Entities.Components.Characters
{
	/// <summary>
	/// Component that handles a character's natural HP and SP recovery
	/// over time.
	/// </summary>
	public class RecoveryComponent : ICharacterComponent
	{
		private TimeSpan _hpRecoveryTime;
		private TimeSpan _spRecoveryTime;

		/// <summary>
		/// Returns the character this component belongs to.
		/// </summary>
		public Character Character { get; }

		/// <summary>
		/// Creates a new recovery component.
		/// </summary>
		/// <param name="character"></param>
		public RecoveryComponent(Character character)
		{
			this.Character = character;

			this.ResetHpTimer();
			this.ResetSpTimer();
		}

		/// <summary>
		/// Updates the character's HP and SP recovery timers.
		/// </summary>
		/// <param name="elapsed"></param>
		public void Update(TimeSpan elapsed)
		{
			if (this.Character.IsDead || this.Character.Controller.IsMoving)
			{
				// No recovery while dead or moving.
				return;
			}

			this.UpdateHp(elapsed);
			this.UpdateSp(elapsed);
		}

		/// <summary>
		/// Updates HP recovery.
		/// </summary>
		/// <param name="elapsed"></param>
		private void UpdateHp(TimeSpan elapsed)
		{
			_hpRecoveryTime -= elapsed;

			if (_hpRecoveryTime <= TimeSpan.Zero)
			{
				this.RecoverHp();
				this.ResetHpTimer();
			}
		}

		/// <summary>
		/// Updates SP recovery.
		/// </summary>
		/// <param name="elapsed"></param>
		private void UpdateSp(TimeSpan elapsed)
		{
			_spRecoveryTime -= elapsed;

			if (_spRecoveryTime <= TimeSpan.Zero)
			{
				this.RecoverSp();
				this.ResetSpTimer();
			}
		}

		/// <summary>
		/// Resets the HP recovery timer based on character state.
		/// </summary>
		private void ResetHpTimer()
		{
			// Standard HP recovery every 6 seconds.
			var interval = 6000;

			// Sitting doubles recovery speed.
			if (this.Character.State == CharacterState.Sitting)
				interval /= 2;

			_hpRecoveryTime = TimeSpan.FromMilliseconds(interval);
		}

		/// <summary>
		/// Resets the SP recovery timer based on character state.
		/// </summary>
		private void ResetSpTimer()
		{
			// Standard SP recovery every 8 seconds.
			var interval = 8000;

			// Sitting doubles recovery speed.
			if (this.Character.State == CharacterState.Sitting)
				interval /= 2;

			_spRecoveryTime = TimeSpan.FromMilliseconds(interval);
		}

		/// <summary>
		/// Recovers a portion of the character's HP.
		/// </summary>
		private void RecoverHp()
		{
			var param = this.Character.Parameters;
			if (param.Hp >= param.HpMax)
				return;

			// Simplified RO-like HP recovery formula.
			var recoveryAmount = 1 + (param.Vit / 5) + (param.HpMax / 500);

			// Ensure at least 1 HP is recovered.
			recoveryAmount = Math.Max(1, recoveryAmount);

			param.Modify(ParameterType.Hp, recoveryAmount);
		}

		/// <summary>
		/// Recovers a portion of the character's SP.
		/// </summary>
		private void RecoverSp()
		{
			var param = this.Character.Parameters;
			if (param.Sp >= param.SpMax)
				return;

			// Simplified RO-like SP recovery formula.
			var recoveryAmount = 1 + (param.Int / 6) + (param.SpMax / 100);

			// Ensure at least 1 SP is recovered.
			recoveryAmount = Math.Max(1, recoveryAmount);

			param.Modify(ParameterType.Sp, recoveryAmount);
		}
	}
}
