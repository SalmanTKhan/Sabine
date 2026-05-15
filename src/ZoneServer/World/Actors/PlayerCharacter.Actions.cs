using System;
using System.Threading;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.Skills;
using Sabine.Zone.Skills.Handlers;
using Sabine.Zone.World.Groups;

namespace Sabine.Zone.World.Actors
{
	public partial class PlayerCharacter : Character
	{
		private Character _attackTarget;
		private bool _isAutoAttacking;
		private CancellationTokenSource _castCts;
		private ActionState _currentAction = ActionState.Idle;
		private PendingSkillCast _pendingSkillCast;

		/// <summary>
		/// Targeted skill cast that's currently waiting for the caster to
		/// walk into range. SP/items are deducted at fire-time, not when
		/// the cast is queued, so a cancelled chase doesn't burn resources.
		/// </summary>
		private sealed class PendingSkillCast
		{
			public Character Target;
			public Skill Skill;
			public int Level;
			public ITargetedSkillHandler Handler;
			public Position LastMoveDestination;
		}

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
			_pendingSkillCast = null;

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
		/// Begins a delayed skill cast. Broadcasts ZC_USESKILL_ACK with
		/// the effective cast time so the client renders its cast bar,
		/// then schedules <paramref name="onComplete"/> via the world
		/// scheduler. The cast is cancellable through <see cref="StopCasting"/>;
		/// the scheduled callback short-circuits if the cancellation
		/// token has been signalled by then. PR_SUFFRAGIUM is consumed
		/// at schedule time so chained casts can't share the buff.
		/// </summary>
		public void ScheduleSkillCast(Skill skill, int level, int targetHandle, Sabine.Shared.World.Position groundPos, Action onComplete)
		{
			if (skill == null || onComplete == null) return;

			var castTimeMs = skill.GetEffectiveCastTime(this);

			// Suffragium burns even if cast time was already zero —
			// matches eAthena, where the buff is single-use.
			this.StatusEffects?.Stop(StatusId.Suffragium);

			if (castTimeMs <= 0)
			{
				onComplete();
				return;
			}

			var token = this.RegisterCast();
			Send.ZC_USESKILL_ACK(this, this.Handle, targetHandle, skill.Id, level, castTimeMs);

			ZoneServer.Instance.World.Scheduler.Schedule(TimeSpan.FromMilliseconds(castTimeMs), _ =>
			{
				if (token.IsCancellationRequested) return;
				if (this.IsDead) return;
				this.FinishCasting();
				onComplete();
			}, null);
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

			// PvP gate: a player can only basic-attack another player
			// when they share a duel. Without this, every player was a
			// valid melee target on every map. Skill targeting is
			// gated separately by the IsHostileTo-aware handlers
			// (Provoke / AoE filters); this just covers the basic
			// attack request which has no hostility check of its own.
			if (target is PlayerCharacter && !this.IsHostileTo(target))
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

			// Stop any current actions except movement (we'll move to target).
			// Order matters: MoveTo internally clears StopAttacking, so set
			// the pending-attack state *after* the walk request is issued.
			this.StopCasting();
			this.StopAttacking();

			this.Controller.MoveTo(target.Position);

			_attackTarget = target;
			_isAutoAttacking = autoAttack;
			_currentAction = ActionState.Moving;
		}

		/// <summary>
		/// Queues a targeted-skill cast. If the caster is already within the
		/// skill's effective range, the skill fires immediately. Otherwise
		/// the caster walks toward <paramref name="target"/> and the handler
		/// is dispatched once the range check passes (see
		/// <see cref="UpdateSkillCastAction"/>). SP and item costs are
		/// deducted at fire-time only, so a cancelled chase doesn't waste
		/// resources.
		/// </summary>
		public void BeginSkillCast(Character target, Skill skill, int level, ITargetedSkillHandler handler)
		{
			if (target == null || skill == null || handler == null)
				return;

			var range = skill.GetEffectiveRange(this);

			if (this.Position.InRange(target.Position, range))
			{
				if (!Sabine.Zone.Network.PacketHandler.TryDeductSkillCost(this, skill, level))
					return;

				_pendingSkillCast = null;
				this.FireOrScheduleTargetedSkill(target, skill, level, handler);
				this.Skills.NoteSkillUsed(skill);
				return;
			}

			// Out of range: chase the target. Issue MoveTo first because it
			// clears StopAttacking / casting state internally, then stash the
			// pending cast so the update tick can finish the job.
			this.StopCasting();
			this.StopAttacking();

			var chaseDest = this.GetSkillChaseDestination(target.Position, range);
			this.Controller.MoveTo(chaseDest);

			_pendingSkillCast = new PendingSkillCast
			{
				Target = target,
				Skill = skill,
				Level = level,
				Handler = handler,
				LastMoveDestination = chaseDest,
			};
			_currentAction = ActionState.Moving;
		}

		/// <summary>
		/// Returns a walkable destination tile that sits within
		/// <paramref name="range"/> of <paramref name="targetPos"/>, so the
		/// caster stops at the skill's range boundary instead of walking
		/// onto the target's tile. Mirrors eAthena's <c>unit_walktobl</c>
		/// behavior: prefer the cell along the caster→target line that is
		/// exactly <c>range</c> away from the target, then ring-search
		/// outward for a passable cell if that one is blocked.
		/// </summary>
		private Position GetSkillChaseDestination(Position targetPos, int range)
		{
			var fromPos = this.Position;

			if (range <= 0 || fromPos == targetPos)
				return targetPos;

			var dx = targetPos.X - fromPos.X;
			var dy = targetPos.Y - fromPos.Y;
			var dist = Math.Sqrt(dx * dx + dy * dy);

			if (dist <= range)
				return fromPos;

			var stopDist = dist - range;
			var preferredX = (int)Math.Round(fromPos.X + dx / dist * stopDist);
			var preferredY = (int)Math.Round(fromPos.Y + dy / dist * stopDist);
			var preferred = new Position(preferredX, preferredY);

			var map = this.Map;

			if (preferred.InRange(targetPos, range) && map.IsPassable(preferred))
				return preferred;

			// Preferred cell is blocked or just-outside; ring-search for the
			// nearest passable tile that's still inside the range circle.
			for (var r = 1; r <= range + 1; r++)
			{
				for (var ox = -r; ox <= r; ox++)
				{
					for (var oy = -r; oy <= r; oy++)
					{
						if (Math.Max(Math.Abs(ox), Math.Abs(oy)) != r)
							continue;

						var cand = new Position(preferred.X + ox, preferred.Y + oy);
						if (!cand.InRange(targetPos, range))
							continue;
						if (!map.IsPassable(cand))
							continue;

						return cand;
					}
				}
			}

			// No reachable in-range tile; fall back to the legacy behavior
			// so the caller's chase loop can re-evaluate next tick.
			return targetPos;
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
		/// Per-tick driver for player-driven actions: walk-into-range for
		/// both basic attacks and queued skill casts.
		/// </summary>
		public override void Update(TimeSpan elapsed)
		{
			base.Update(elapsed);

			this.UpdateAttackAction();
			this.UpdateSkillCastAction();
		}

		/// <summary>
		/// Drives a pending skill cast: while the caster is still walking
		/// toward the target, this method waits; once in range, it deducts
		/// the cast cost and invokes the handler. Cleans up on death,
		/// map change, sit, immobilization, or a new cast request.
		/// </summary>
		private void UpdateSkillCastAction()
		{
			var cast = _pendingSkillCast;
			if (cast == null)
				return;

			if (cast.Target.IsDead || cast.Target.Map != this.Map)
			{
				_pendingSkillCast = null;
				this.Controller.StopMove();
				if (_currentAction == ActionState.Moving)
					_currentAction = ActionState.Idle;
				return;
			}

			if (this.State == CharacterState.Sitting
				|| this.IsImmobilized
				|| this.IsSilenced
				|| this.SkillBlocked
				|| this.IsDead)
			{
				_pendingSkillCast = null;
				this.Controller.StopMove();
				if (_currentAction == ActionState.Moving)
					_currentAction = ActionState.Idle;
				return;
			}

			// A new cast has started in the meantime; the existing cast was
			// already swapped out by BeginSkillCast (or StopCasting), so
			// nothing more to do here.
			if (_currentAction == ActionState.Casting)
				return;

			var range = cast.Skill.GetEffectiveRange(this);

			if (!this.Position.InRange(cast.Target.Position, range))
			{
				// Target moved out of the previously-chosen destination's
				// range — recompute a tile within `range` of the new target
				// position. We only re-issue when the prior destination is
				// no longer adequate, otherwise the path keeps thrashing.
				if (!cast.LastMoveDestination.InRange(cast.Target.Position, range))
				{
					var chaseDest = this.GetSkillChaseDestination(cast.Target.Position, range);
					this.Controller.MoveTo(chaseDest);
					cast.LastMoveDestination = chaseDest;
				}
				return;
			}

			// In range: stop, charge the cost, fire the handler.
			this.Controller.StopMove();
			_pendingSkillCast = null;

			if (_currentAction == ActionState.Moving)
				_currentAction = ActionState.Idle;

			if (!Sabine.Zone.Network.PacketHandler.TryDeductSkillCost(this, cast.Skill, cast.Level))
				return;

			this.FireOrScheduleTargetedSkill(cast.Target, cast.Skill, cast.Level, cast.Handler);
			this.Skills.NoteSkillUsed(cast.Skill);
		}

		/// <summary>
		/// If the skill has a non-zero effective cast time, defer the
		/// handler invocation through <see cref="ScheduleSkillCast"/>;
		/// otherwise fire it immediately. Centralises the cast-time
		/// branch for both the in-range and chase-completion paths.
		/// </summary>
		private void FireOrScheduleTargetedSkill(Character target, Skill skill, int level, ITargetedSkillHandler handler)
		{
			void Fire() => handler.Handle(new UseSkillParams(this, target, skill, level));

			if (skill.GetEffectiveCastTime(this) > 0)
				this.ScheduleSkillCast(skill, level, target.Handle, target.Position, Fire);
			else
				Fire();
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

		/// <summary>
		/// Sends the storage item list and count info to the client, marking
		/// it open for subsequent move requests. Idempotent.
		/// </summary>
		public void OpenStorage()
		{
			if (this.Storage.IsOpen)
				return;

			// Vending and trade hold inventory state that the storage
			// window mustn't race against; eAthena rejects in both
			// cases. Reject silently to mirror the existing pattern.
			if (this.VendingShop?.IsOpen == true)
				return;

			if (ZoneServer.Instance.World.Trades.TryGetTrade(this, out _))
				return;

			var items = this.Storage.GetItems();
			Send.ZC_STORE_NORMAL_ITEMLIST(this, items);
			Send.ZC_STORE_EQUIPMENT_ITEMLIST(this, items);
			Send.ZC_NOTIFY_STOREITEM_COUNTINFO(this, this.Storage.ItemCount, Sabine.Zone.World.Actors.Components.Characters.Storage.MaxSlots);

			this.Storage.IsOpen = true;
			this.ActiveStorage = StorageType.Storage;
		}

		/// <summary>
		/// Opens the shared guild storage window. Reuses the personal
		/// storage packets but routes mutations to <see cref="Guild.Storage"/>
		/// via <see cref="ActiveStorage"/>. Caller must have verified
		/// `Guild != null` and the master-only permission gate.
		/// </summary>
		public void OpenGuildStorage()
		{
			if (this.Guild == null || this.Guild.Storage == null)
				return;

			// Same anti-race guards as personal storage. Also refuse
			// if the personal storage window is already open -- the
			// client only renders one storage window at a time.
			if (this.Storage.IsOpen)
				return;

			if (this.VendingShop?.IsOpen == true)
				return;

			if (ZoneServer.Instance.World.Trades.TryGetTrade(this, out _))
				return;

			var items = this.Guild.Storage.GetItems();
			Send.ZC_STORE_NORMAL_ITEMLIST(this, items);
			Send.ZC_STORE_EQUIPMENT_ITEMLIST(this, items);
			Send.ZC_NOTIFY_STOREITEM_COUNTINFO(this, this.Guild.Storage.ItemCount, GuildStorage.MaxSlots);

			this.ActiveStorage = StorageType.GuildStorage;
		}

		/// <summary>
		/// Closes whichever storage window is currently open and resets
		/// <see cref="ActiveStorage"/> to the personal-storage default
		/// so the next open routes correctly.
		/// </summary>
		public void CloseStorage()
		{
			if (this.ActiveStorage == StorageType.GuildStorage)
			{
				// Flush guild storage on close so a member's deposit
				// survives a crash before the next roster mutation.
				ZoneServer.Instance.World.Guilds.SaveStorage(this.Guild);

				this.ActiveStorage = StorageType.Storage;
				Send.ZC_CLOSE_STORE(this);
				return;
			}

			if (!this.Storage.IsOpen)
				return;

			this.Storage.IsOpen = false;
			this.ActiveStorage = StorageType.Storage;
			Send.ZC_CLOSE_STORE(this);
		}
	}
}
