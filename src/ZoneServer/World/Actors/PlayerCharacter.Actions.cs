using System.Threading;
using Sabine.Shared.Const;
using Sabine.Zone.Network;

namespace Sabine.Zone.World.Actors
{
	public partial class PlayerCharacter : Character
	{
		private Character _attackTarget;
		private bool _isAutoAttacking;
		private CancellationTokenSource _castCts;
		private ActionState _currentAction = ActionState.Idle;

		/// <summary>
		/// Represents the type of action the character is currently performing.
		/// </summary>
		public enum ActionState
		{
			Idle,
			Moving,
			Attacking,
			Casting,
			Sitting
		}

		/// <summary>
		/// Gets the current action state of the character.
		/// </summary>
		public ActionState CurrentAction => _currentAction;

		/// <summary>
		/// Returns true if the character is currently casting a skill.
		/// </summary>
		public override bool IsCasting => _castCts != null;

		/// <summary>
		/// Returns true if the character is currently performing any action that should prevent other actions.
		/// </summary>
		public bool IsBusy => _currentAction != ActionState.Idle && _currentAction != ActionState.Moving;

		/// <summary>
		/// Cancels all current actions: Movement, Attacking, and Casting.
		/// </summary>
		public void CancelAction()
		{
			this.Controller.StopMove();
			this.CancelAttack();
			this.StopCasting();

			if (_currentAction == ActionState.Sitting)
				return; // Don't change state if sitting

			_currentAction = ActionState.Idle;
		}

		/// <summary>
		/// Stops the current cast if one is in progress.
		/// </summary>
		public void StopCasting()
		{
			if (_castCts != null)
			{
				_castCts.Cancel();
				_castCts.Dispose();
				_castCts = null;

				if (_currentAction == ActionState.Casting)
					_currentAction = ActionState.Idle;

				// Visual cancellation on client
				this.ChangeLook(SpriteType.Base, this.DisplayClassId);
			}
		}

		/// <summary>
		/// Registers a cancellation token for a new cast action.
		/// </summary>
		public CancellationToken RegisterCast()
		{
			// Cancel any existing actions before starting a cast
			this.CancelAction();

			_castCts = new CancellationTokenSource();
			_currentAction = ActionState.Casting;

			return _castCts.Token;
		}

		/// <summary>
		/// Clears the casting state (called when cast completes successfully).
		/// </summary>
		public void FinishCasting()
		{
			_castCts?.Dispose();
			_castCts = null;

			if (_currentAction == ActionState.Casting)
				_currentAction = ActionState.Idle;
		}

		/// <summary>
		/// Overrides the base attack logic to handle moving into range first.
		/// </summary>
		public override void StartAttacking(Character target, bool autoAttack)
		{
			// Don't allow attacking while casting or sitting
			if (_currentAction == ActionState.Casting || _currentAction == ActionState.Sitting)
				return;

			if (target == null || target == this || target.IsDead)
				return;

			this.StopCasting();
			this.StopAttacking();

			if (this.Position.InRange(target.Position, this.GetAttackRange()))
			{
				// Already in range: attack immediately via the base character loop.
				_currentAction = ActionState.Attacking;
				base.StartAttacking(target, autoAttack);
			}
			else
			{
				// Out of range: walk toward target first (UpdateAttackAction handles
				// the transition once in range).
				this.InitiateAttack(target, autoAttack);
			}
		}

		/// <summary>
		/// Initiates an attack on a target. For players, this will handle
		/// moving into range before attacking.
		/// </summary>
		private void InitiateAttack(Character target, bool autoAttack)
		{
			if (target == null || target == this || target.IsDead)
				return;

			// Stop any current actions except movement (we'll move to target)
			this.StopCasting();
			this.StopAttacking();

			_attackTarget = target;
			_isAutoAttacking = autoAttack;
			_currentAction = ActionState.Attacking;
		}

		/// <summary>
		/// Cancels the current attack action, including moving towards a target.
		/// </summary>
		public void CancelAttack()
		{
			_attackTarget = null;
			this.StopAttacking();
		}

		/// <summary>
		/// Stops the character's attack animation and clears the attack target.
		/// </summary>
		public override void StopAttacking()
		{
			_attackTarget = null;
			base.StopAttacking();

			if (_currentAction == ActionState.Attacking)
				_currentAction = ActionState.Idle;
		}

		/// <summary>
		/// Manages the state of a player-initiated attack, such as moving into range.
		/// This is called on every update tick.
		/// </summary>
		private void UpdateAttackAction()
		{
			if (_attackTarget == null)
				return;

			// Cancel if conditions are no longer valid
			if (_attackTarget.IsDead || _attackTarget.Map != this.Map)
			{
				this.CancelAction();
				return;
			}

			if (this.State == CharacterState.Sitting)
			{
				this.CancelAction();
				return;
			}

			// Can't attack while casting
			if (_currentAction == ActionState.Casting)
			{
				this.CancelAttack();
				return;
			}

			var attackRange = this.GetAttackRange();

			if (!this.Position.InRange(_attackTarget.Position, attackRange))
			{
				_currentAction = ActionState.Moving;
				this.Controller.MoveTo(_attackTarget.Position);
				return;
			}

			// We are in range. Stop moving and start the base attack loop.
			this.Controller.StopMove();
			_currentAction = ActionState.Attacking;
			base.StartAttacking(_attackTarget, _isAutoAttacking);

			// The attack request is now handled by the base class's attack loop. 
			// Clear the target to prevent this method from re-triggering the attack.
			_attackTarget = null;
		}

		/// <summary>
		/// Makes character sit down.
		/// </summary>
		public void SitDown()
		{
			if (this.State != CharacterState.Standing)
				return;

			this.CancelAction();
			this.State = CharacterState.Sitting;
			_currentAction = ActionState.Sitting;

			Send.ZC_NOTIFY_ACT.Simple(this, this.Handle, ActionType.SitDown);
		}

		/// <summary>
		/// Makes character stand up.
		/// </summary>
		public void StandUp()
		{
			if (this.State != CharacterState.Sitting)
				return;

			this.State = CharacterState.Standing;
			_currentAction = ActionState.Idle;

			Send.ZC_NOTIFY_ACT.Simple(this, this.Handle, ActionType.StandUp);
		}
	}
}
