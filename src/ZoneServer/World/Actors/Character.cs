using System;
using Sabine.Shared;
using Sabine.Shared.Configuration.Files;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.Skills;
using Sabine.Zone.World.Actors.Components.Characters;
using Sabine.Zone.World.Maps;
using Yggdrasil.Logging;
using Yggdrasil.Scheduling;
using Yggdrasil.Util;
using Yggdrasil.Versioning.ManagedEnum;

namespace Sabine.Zone.World.Actors
{
	/// <summary>
	/// A character that can interact with the world and can be
	/// interacted with, such as a player or an NPC.
	/// </summary>
	public abstract class Character : IStandEntry, IUpdateable
	{
		/// <summary>
		/// Returns the character's unique handle, which it's identified
		/// as during interactions with the world and other characters.
		/// </summary>
		public abstract int Handle { get; protected set; }

		/// <summary>
		/// Returns the character's name.
		/// </summary>
		public virtual string Name { get; set; } = "";

		/// <summary>
		/// Returns the character's username.
		/// </summary>
		public virtual string Username { get; } = "";

		/// <summary>
		/// Returns the character's class id, defining (part of) its
		/// appearance.
		/// </summary>
		public abstract IdentityId IdentityId { get; protected set; }

		public BodyState BodyState { get; protected set; }
		public EffectState EffectState { get; protected set; }
		public HealthState HealthState { get; protected set; }
		public OptionState OptionState { get; protected set; }

		public virtual int OwnerHandle { get; protected set; } = 0;
		public virtual int TargetHandle { get; protected set; } = 0;
		public virtual int GuildId { get; protected set; } = 0;
		public virtual bool IsCasting { get; protected set; } = false;

		/// <summary>
		/// Gets or sets the identity id that the character will appear as.
		/// Falls back to <see cref="IdentityId"/> when not overridden.
		/// </summary>
		public int DisplayClassId
		{
			get => _displayClassId == -1 ? (int)this.IdentityId : _displayClassId;
			set => _displayClassId = value;
		}
		private int _displayClassId = -1;

		/// <summary>
		/// Returns the character's sex.
		/// </summary>
		public virtual Sex Sex { get; }

		/// <summary>
		/// Returns a character's hair id, defining the look of their
		/// head.
		/// </summary>
		public virtual int HairId { get; set; }

		/// <summary>Current palette/look values used by scripting and sprite updates.</summary>
		public virtual int HairColorLook { get; set; }
		public virtual int ClothesColorLook { get; set; }
		public virtual int ShieldLook { get; set; }
		public virtual int BodyLook { get; set; }
		public virtual int RobeLook { get; set; }
		public virtual int Body2Look { get; set; }
		public virtual bool DamageImmune { get; set; }
		public virtual int ScriptGroupId { get; set; }

		/// <summary>
		/// Returns a character's upper headgear look.
		/// </summary>
		public virtual int HeadTopLook { get; set; }

		/// <summary>
		/// Returns a character's middle headgear look.
		/// </summary>
		public virtual int HeadMiddleLook { get; set; }

		/// <summary>
		/// Returns a character's lower headgear look.
		/// </summary>
		public virtual int HeadBottomLook { get; set; }

		/// <summary>
		/// Returns a character's weapon id, defining what weapon they
		/// can be seen holding during combat.
		/// </summary>
		public virtual int WeaponLook { get; set; }

		/// <summary>
		/// Returns a character's current state.
		/// </summary>
		public virtual CharacterState State { get; set; }

		/// <summary>
		/// Gets or sets the id of the map the character is currently on.
		/// </summary>
		public int MapId { get; set; } = 100036;

		/// <summary>
		/// Gets or sets the character's a reference to the map the
		/// character is currently on.
		/// </summary>
		public Map Map
		{
			get => _map;
			internal set => _map = value ?? Map.Limbo;
		}
		private Map _map = Map.Limbo;

		/// <summary>
		/// Gets or sets the character's current position.
		/// </summary>
		public Position Position { get; set; } = new Position(99, 81);

		/// <summary>
		/// Returns the direction the character is turned towards.
		/// </summary>
		public virtual Direction Direction { get; set; } = Direction.North;

		/// <summary>
		/// Gets or sets the head turn direction relative to the
		/// character's direction.
		/// </summary>
		public virtual HeadTurn HeadTurn { get; set; } = HeadTurn.Straight;

		/// <summary>
		/// Returns the character's speed parameter.
		/// </summary>
		public int Speed => this.Parameters.Speed;

		/// <summary>
		/// Returns true if the character's HP have reached 0.
		/// </summary>
		public bool IsDead => this.Parameters.Hp == 0;

		/// <summary>
		/// Gets or sets the time at which the character's stun ends.
		/// </summary>
		public DateTime StunEndTime { get; set; }

		/// <summary>
		/// Returns true if the character is currently stunned.
		/// </summary>
		public bool IsStunned => DateTime.Now < this.StunEndTime;

		/// <summary>
		/// Temporary test property, returning handle of the last attacker.
		/// </summary>
		public int AttackerHandleTest { get; set; }

		/// <summary>
		/// Characters currently linked to this one by CR_DEVOTION. Used to
		/// enforce eAthena's 5-link cap and to drop the bonds on caster
		/// death / disconnect. Maintained by the Devotion status handler.
		/// </summary>
		public System.Collections.Generic.List<Character> DevotionLinkedTargets { get; } = new();

		/// <summary>
		/// Remaining time inside the post-damage regen pause window.
		/// Managed by <c>RegenComponent</c>; written by <c>TakeDamage</c>.
		/// eAthena status.c:status_calc_regen_rate pauses natural regen
		/// for ~3 seconds after the character takes a hit.
		/// </summary>
		public TimeSpan RegenPauseRemaining { get; set; }

		/// <summary>
		/// Returns the character's parameters.
		/// </summary>
		public Parameters Parameters { get; protected set; }

		/// <summary>
		/// Returns the character's movement controller.
		/// </summary>
		public MovementController Controller { get; protected set; }

		/// <summary>
		/// Returns the character's skill manager component.
		/// </summary>
		public SkillComponent Skills { get; protected set; }

		/// <summary>
		/// Returns the character's status effect manager component.
		/// </summary>
		public StatusEffectComponent StatusEffects { get; protected set; }

		/// <summary>
		/// Returns the character's accumulated damage modifiers (race /
		/// element / size add and sub bonuses). Used by the battle
		/// calculator. Populated by the card / equip engine in the
		/// future; all-zero in v1.
		/// </summary>
		public Sabine.Zone.Battle.BonusModifiers Modifiers { get; } = new Sabine.Zone.Battle.BonusModifiers();

		/// <summary>
		/// Adds (or subtracts) a flat bonus to a substat in a way that
		/// survives parameter recalcs. Used by status-effect handlers that
		/// need to bump Attack / Hit / MeleeDefenseBonus / etc. — those
		/// substats are reassigned from base values inside
		/// <see cref="Sabine.Zone.World.Actors.Components.Characters.PlayerCharacterParameters.RecalculateAll"/>,
		/// so a plain <c>Parameters.Modify</c> on them is silently wiped
		/// the next time the player equips an item.
		/// </summary>
		/// <remarks>
		/// Default implementation calls <c>Parameters.Modify</c> directly,
		/// which is correct for monsters (no recalc ever runs on them so
		/// the value sticks). <see cref="PlayerCharacter"/> overrides this
		/// to write into <see cref="Sabine.Zone.Battle.BonusModifiers"/>
		/// and trigger a RecalculateAll, so the bonus is folded into the
		/// recalc result and persists across subsequent equip changes.
		/// Pass the same <paramref name="amount"/> with a flipped sign on
		/// status-effect end to symmetrically revert.
		/// </remarks>
		public virtual void AddBonusStat(ParameterType type, int amount)
		{
			this.Parameters.Modify(type, amount);
		}

		/// <summary>True if Hiding (TF_HIDING) is active on the character.</summary>
		public bool IsHidden => this.StatusEffects?.Has(StatusId.Hiding) == true;

		/// <summary>
		/// True when monster AI must skip this character during aggro
		/// acquisition. Mirrors eAthena mob.c's mob_ai_sub_hard target
		/// filter, which excludes Hiding, Cloaking, and TrickDead. Hides
		/// the character from passive scans only — once aggro is locked
		/// (or damage is taken), TrickDead breaks via the existing
		/// listeners and Hiding/Cloaking are revealed by Sight/Ruwach.
		/// </summary>
		public bool IsAggroInvisible
			=> this.StatusEffects != null && (
				this.StatusEffects.Has(StatusId.Hiding)
				|| this.StatusEffects.Has(StatusId.Cloaking)
				|| this.StatusEffects.Has(StatusId.TrickDead));

		/// <summary>True if the character is petrified, frozen, asleep, or stunned — i.e. cannot act.</summary>
		public bool IsImmobilized
			=> this.StatusEffects != null && (
				this.StatusEffects.Has(StatusId.Stone)
				|| this.StatusEffects.Has(StatusId.Freeze)
				|| this.StatusEffects.Has(StatusId.Sleep)
				|| this.StatusEffects.Has(StatusId.Stun)
				|| this.StatusEffects.Has(StatusId.Gravity));

		/// <summary>True while Silence is active — can't cast skills.</summary>
		public bool IsSilenced => this.StatusEffects?.Has(StatusId.Silence) == true;

		/// <summary>
		/// True when movement is locked but actions still flow — eAthena
		/// SC_STOP. Distinct from <see cref="IsImmobilized"/>, which
		/// blocks action too. CH_TIGERFIST inflicts this; the
		/// MovementController checks it on every MoveTo to short-circuit
		/// pathing while the cast / attack pipelines stay open.
		/// </summary>
		public bool IsRooted => this.StatusEffects != null && (
			this.StatusEffects.Has(StatusId.Stop)
			|| this.StatusEffects.Has(StatusId.AnkleSnare));

		/// <summary>
		/// True while the character owns a live song / dance unit
		/// (SC_DANCING in eAthena). Movement is locked unless
		/// CG_LONGINGFREEDOM is active, mirroring eAthena's gender-paired
		/// performer immobilization. The check walks the current map's
		/// SkillUnit list so re-summoning or partial expiry doesn't drift
		/// from the lock state.
		/// </summary>
		public bool IsPerforming
		{
			get
			{
				if (this.Map == null) return false;
				if (this.StatusEffects?.Has(StatusId.LongingFreedom) == true) return false;

				foreach (var unit in this.Map.GetSkillUnitsByOwner(this))
				{
					if (unit is Sabine.Zone.World.Maps.SkillUnits.SongUnit)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Returns the character's components.
		/// </summary>
		public CharacterComponents Components { get; } = new CharacterComponents();

		/// <summary>
		/// Server-side pacing gates (rA ud.attackabletime / canact_tick /
		/// canmove_tick). Stamped when an action fires; consulted by the
		/// matching packet handler to drop requests that arrive before the
		/// previous action's motion delay has elapsed.
		/// </summary>
		public DateTime NextAttackTime { get; private set; } = DateTime.MinValue;
		public DateTime NextActTime { get; private set; } = DateTime.MinValue;
		public DateTime NextMoveTime { get; private set; } = DateTime.MinValue;

		private static readonly TimeSpan PaceGraceAttack = TimeSpan.FromMilliseconds(50);
		private static readonly TimeSpan PaceGraceMove = TimeSpan.FromMilliseconds(150);

		public bool CanAttackNow(DateTime now)
			=> now + PaceGraceAttack >= this.NextAttackTime
			&& now + PaceGraceAttack >= this.NextActTime;

		public bool CanActNow(DateTime now)
			=> now + PaceGraceAttack >= this.NextActTime;

		public bool CanMoveNow(DateTime now)
			=> now + PaceGraceMove >= this.NextMoveTime;

		internal void NoteAttacked(int attackMotionDelayMs)
		{
			var end = DateTime.UtcNow.AddMilliseconds(attackMotionDelayMs);
			this.NextAttackTime = end;
			if (end > this.NextActTime)
				this.NextActTime = end;
		}

		internal void NoteActed(int actDelayMs)
		{
			if (actDelayMs <= 0) return;
			var end = DateTime.UtcNow.AddMilliseconds(actDelayMs);
			if (end > this.NextActTime)
				this.NextActTime = end;
		}

		internal void NoteMoveStarted(int moveDelayMs)
		{
			if (moveDelayMs <= 0) return;
			var end = DateTime.UtcNow.AddMilliseconds(moveDelayMs);
			if (end > this.NextMoveTime)
				this.NextMoveTime = end;
		}

		/// <summary>
		/// Initializes character.
		/// </summary>
		public Character()
		{
			this.Components.Add(this.Controller = new MovementController(this));
			this.Components.Add(this.Skills = new SkillComponent(this));
			this.Components.Add(this.StatusEffects = new StatusEffectComponent(this));
		}

		/// <summary>
		/// Sets or clears the BodyState (opt1). Body state is
		/// non-stackable (Stone/Freeze/Stun/Sleep are mutually exclusive)
		/// so callers pass <c>BodyState.None</c> to clear. The state is
		/// broadcast via ZC_STATE_CHANGE.
		/// </summary>
		internal void SetBodyState(BodyState state)
		{
			if (this.BodyState == state)
				return;

			this.BodyState = state;
			Send.ZC_STATE_CHANGE(this);
		}

		/// <summary>
		/// Clears the BodyState only if the current state matches the
		/// expected one. Avoids one expiring effect (e.g. Stone) wiping
		/// a different body state set by a later effect.
		/// </summary>
		internal void ClearBodyState(BodyState expected)
		{
			if (this.BodyState != expected)
				return;
			this.SetBodyState(BodyState.None);
		}

		/// <summary>
		/// Toggles a HealthState (opt2) flag and broadcasts the new state
		/// to nearby clients via ZC_STATE_CHANGE.
		/// </summary>
		internal void SetHealthStateFlag(HealthState flag, bool on)
		{
			var newState = on ? (this.HealthState | flag) : (this.HealthState & ~flag);
			if (newState == this.HealthState)
				return;

			this.HealthState = newState;
			Send.ZC_STATE_CHANGE(this);
		}

		/// <summary>
		/// Toggles an EffectState (opt3) flag and broadcasts the new state
		/// to nearby clients via ZC_STATE_CHANGE.
		/// </summary>
		internal void SetEffectStateFlag(EffectState flag, bool on)
		{
			var newState = on ? (this.EffectState | flag) : (this.EffectState & ~flag);
			if (newState == this.EffectState)
				return;

			this.EffectState = newState;
			Send.ZC_STATE_CHANGE(this);
		}

		/// <summary>
		/// Toggles an OptionState (opt0) flag and broadcasts the new state
		/// to nearby clients via ZC_STATE_CHANGE. Used for the falcon /
		/// peco / cart / hide / cloak visuals that the alpha / beta1
		/// client renders from this field. Mirrors eAthena pc_setoption.
		/// </summary>
		internal void SetOptionFlag(OptionState flag, bool on)
		{
			var newState = on ? (this.OptionState | flag) : (this.OptionState & ~flag);
			if (newState == this.OptionState)
				return;

			this.OptionState = newState;
			Send.ZC_STATE_CHANGE(this);
		}

		/// <summary>
		/// Replaces the three state fields exposed by rAthena's script
		/// <c>@option</c> usage and broadcasts the result once.
		/// </summary>
		public void SetScriptOptions(int bodyState, int healthState, int optionState)
		{
			this.BodyState = (BodyState)bodyState;
			this.HealthState = (HealthState)healthState;
			this.OptionState = (OptionState)optionState;
			Send.ZC_STATE_CHANGE(this);
		}

		public void ClearDressOptions()
		{
			this.OptionState &= ~OptionState.Wedding;
			Send.ZC_STATE_CHANGE(this);
		}

		/// <summary>
		/// Updates the character's components.
		/// </summary>
		/// <param name="elapsed"></param>
		public virtual void Update(TimeSpan elapsed)
		{
			this.Components.Update(elapsed);
		}

		/// <summary>
		/// Warps the character to the given location.
		/// </summary>
		/// <param name="mapStringId"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <exception cref="ArgumentException"></exception>
		public void Warp(string mapStringId, int x, int y)
		{
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(mapStringId, out var map))
				throw new ArgumentException($"Map '{mapStringId}' not found.");

			this.Warp(new Location(map.Id, new Position(x, y)));
		}

		/// <summary>
		/// Warps character to given location.
		/// </summary>
		/// <param name="mapId"></param>
		/// <param name="pos"></param>
		public void Warp(int mapId, Position pos)
			=> this.Warp(new Location(mapId, pos));

		/// <summary>
		/// Warps character to the given location.
		/// </summary>
		/// <param name="location"></param>
		public abstract void Warp(Location location);

		/// <summary>
		/// Plays an emote above this character. Mirrors rAthena's
		/// <c>emotion</c> script command.
		/// </summary>
		/// <param name="emotionId"></param>
		public void Emote(EmotionId emotionId)
			=> Send.ZC_EMOTION(this, emotionId);

		/// <summary>
		/// int-id overload, used by auto-converted scripts that pass
		/// raw rAthena ET_* numeric constants.
		/// </summary>
		/// <param name="emotionId"></param>
		public void Emote(int emotionId)
			=> this.Emote((EmotionId)emotionId);

		/// <summary>
		/// Sets character's map id and position.
		/// </summary>
		/// <param name="location"></param>
		public void SetLocation(Location location)
		{
			this.MapId = location.MapId;
			this.Position = location.Position;
		}

		/// <summary>
		/// Returns the character's location.
		/// </summary>
		/// <returns></returns>
		public Location GetLocation()
			=> new(this.MapId, this.Position);

		/// <summary>
		/// Returns the character's current attack range, based on its
		/// state and equipped items.
		/// </summary>
		/// <returns></returns>
		public virtual int GetAttackRange()
		{
			return 3;
		}

		/// <summary>
		/// Returns true if the given character is within this character's
		/// attack range, measured using the Chebyshev (square) metric so
		/// that diagonal adjacency counts as in range.
		/// </summary>
		/// <param name="other"></param>
		/// <param name="range"></param>
		/// <returns></returns>
		public bool InAttackRange(Character other, int range)
			=> this.Position.InSquareRange(other.Position, range);

		/// <summary>
		/// Reduces the character's HP by the given amount, returns the
		/// character's remaining HP.
		/// </summary>
		/// <param name="amount"></param>
		/// <param name="attacker"></param>
		/// <returns></returns>
		public virtual int TakeDamage(int amount, Character attacker)
		{
			if (this.IsDead || this.DamageImmune)
				return 0;

			var remainingHp = this.Parameters.Modify(ParameterType.Hp, -amount);

			// Cancel any in-progress cast on the damaged character —
			// matches eAthena unit.c:unit_skillcastcancel. Free Cast
			// suppresses the cancel only for movement, not damage, so
			// it doesn't gate this branch.
			if (amount > 0 && this is PlayerCharacter pcCast && pcCast.IsCasting)
				pcCast.StopCasting();

			// Natural regen pauses for ~3 seconds after taking damage.
			// Heal skills aren't gated by this — the pause only affects
			// the RegenComponent's tick.
			if (amount > 0)
				this.RegenPauseRemaining = TimeSpan.FromSeconds(3);

			// SM_AUTOBERSERK: while the skill is learned, dropping to
			// 25% HP or below auto-applies Provoke Lv10 on self.
			// eAthena pc.c pc_damage_sp_loss.
			if (amount > 0 && this is PlayerCharacter pcBerserk
				&& pcBerserk.Skills.Has(SkillId.SM_AUTOBERSERK)
				&& this.Parameters.HpMax > 0
				&& remainingHp * 4 <= this.Parameters.HpMax
				&& this.StatusEffects?.Has(StatusId.Provoke) == false)
			{
				this.StatusEffects.Start(StatusId.Provoke, 10, TimeSpan.FromSeconds(30), this, 10);
			}

			// Sleep breaks on the first non-zero hit. Run after the
			// damage is applied so the 1.5x sleep multiplier already
			// took effect on the breaking hit.
			if (amount > 0 && this.StatusEffects?.Has(StatusId.Sleep) == true)
				this.StatusEffects.Stop(StatusId.Sleep);

			// NV_TRICKDEAD breaks on any damage taken.
			if (amount > 0 && this.StatusEffects?.Has(StatusId.TrickDead) == true)
				this.StatusEffects.Stop(StatusId.TrickDead);

			if (remainingHp == 0)
				this.Kill(attacker);

			// Push the HP change to party members on the same map.
			if (this is PlayerCharacter player)
				player.Party?.BroadcastHp(player);

			// Broadcast the damage to anything that wants to mirror the HP
			// delta in a derived UI (mercenary panel, raid frames, etc.).
			Sabine.Zone.Battle.CombatEvents.RaiseCharacterDamaged(this, amount, attacker, remainingHp);

			return remainingHp;
		}

		/// <summary>
		/// Kills the character.
		/// </summary>
		/// <param name="killer"></param>
		public virtual void Kill(Character killer)
		{
			// TODO: Figure out what needs to happen when we kill
			//   different kinds of entities.

			// CR_DEVOTION drops on caster death (eAthena status.c:6567).
			// Iterate over a snapshot — Stop mutates the list via the
			// status handler's OnEnd hook.
			if (this.DevotionLinkedTargets.Count > 0)
			{
				foreach (var linked in this.DevotionLinkedTargets.ToArray())
					linked.StatusEffects?.Stop(StatusId.Devotion);
				this.DevotionLinkedTargets.Clear();
			}

			this.Parameters.Hp = 0;
			this.State = CharacterState.Dead;
		}

		/// <summary>
		/// Drops item in range of the character.
		/// </summary>
		/// <param name="item"></param>
		public void Drop(Item item)
		{
			var pos = this.Position.GetRandomInSquareRange(1);
			item.Drop(this.Map, pos);
		}

		private long _attackCallbackId;
		private bool _cancelAttack;

		/// <summary>
		/// Makes character start attacking the given target, potentially
		/// keeping the attack up until StopAttacking is called.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="autoAttack"></param>
		public virtual void StartAttacking(Character target, bool autoAttack)
		{
			_cancelAttack = false;

			// Don't start a second attack if we're already attacking.
			if (_attackCallbackId != 0)
				return;

			this.Attack(target, autoAttack);
		}

		/// <summary>
		/// Callback for the AutoAttack timer, executes the next attack.
		/// </summary>
		/// <param name="state"></param>
		private void Attack(CallbackState state)
		{
			try
			{
				var target = (Character)state.Arguments[0];
				this.Attack(target, true);
			}
			catch (Exception ex)
			{
				Log.Error("Error during auto attack: " + ex);
			}
		}

		/// <summary>
		/// Makes character attack the given target.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="autoAttack"></param>
		internal void Attack(Character target, bool autoAttack)
		{
			var attacker = this;

			if (_cancelAttack || target.IsDead || target.Map != attacker.Map || attacker.IsImmobilized || attacker.IsDead)
			{
				_attackCallbackId = 0;
				return;
			}

			// Per-swing range re-validation (B4 attack-range fix, see plan
			// Stream A1). The auto-attack timer keeps ticking even if the
			// target walked out of range between scheduled swings, so we
			// must reject the swing server-side rather than trust the
			// request. The metric is Chebyshev (square) so that diagonal
			// adjacency counts as in range, matching the client's melee
			// selection. Default reaction: stop the attack loop. Player
			// overrides OnAttackTargetOutOfRange to resume the chase.
			var attackRange = attacker.GetAttackRange();
			if (!attacker.InAttackRange(target, attackRange))
			{
				this.OnAttackTargetOutOfRange(target, attackRange);
				_attackCallbackId = 0;
				return;
			}

			// Hiding / Cloaking break on the wearer's own basic attack
			// per eAthena status.c — attacking reveals the actor.
			if (attacker.StatusEffects != null)
			{
				if (attacker.StatusEffects.Has(StatusId.Hiding))
					attacker.StatusEffects.Stop(StatusId.Hiding);
				if (attacker.StatusEffects.Has(StatusId.Cloaking))
					attacker.StatusEffects.Stop(StatusId.Cloaking);
			}

			var ctx = new Sabine.Zone.Battle.AttackContext(attacker, target);
			var result = Sabine.Zone.Battle.BattleCalculator.Calc(ctx);

			var damage = result.IsMiss ? 0 : result.Damage;

			if (!result.IsMiss)
			{
				target.TakeDamage(damage, attacker);

				// SC_ENDURE: hits don't apply the dmotion stagger or
				// stop the movement controller. eAthena status.c
				// terminates the buff after 7 hits.
				var endureActive = false;
				if (target.StatusEffects != null && target.StatusEffects.TryGet(StatusId.Endure, out var endure))
				{
					endureActive = true;
					endure.Val3--;
					if (endure.Val3 <= 0)
						target.StatusEffects.Stop(StatusId.Endure);
				}

				if (!endureActive)
				{
					target.StunEndTime = DateTime.Now.AddSeconds(1);
					target.Controller.StopMove();
				}
				target.AttackerHandleTest = attacker.Handle;

				// Update the monster's name if the display HP option
				// was enabled
				if (attacker is PlayerCharacter player && target is Monster)
				{
					if (ZoneServer.Instance.Conf.World.DisplayMonsterHp != DisplayMonsterHpType.No)
						Send.ZC_ACK_REQNAME(player, target);
				}
			}

			attacker.Parameters.RecalculateAll();

			var attackMotionDelay = attacker.Parameters.AttackMotionDelay;
			var damageMotionDelay = target.Parameters.DamageMotionDelay;

			Send.ZC_NOTIFY_ACT.Attack(attacker, attacker.Handle, target.Handle, Game.GetTick(), result.ActionType, damage, attackMotionDelay, damageMotionDelay);

			attacker.NoteAttacked(attackMotionDelay);

			if (target.IsDead)
				autoAttack = false;

			if (autoAttack)
			{
				_attackCallbackId = ZoneServer.Instance.World.Scheduler.Schedule(attackMotionDelay, this.Attack, target);
			}
			else
			{
				_attackCallbackId = 0;
			}
		}

		/// <summary>
		/// Stops character auto attacking its current target.
		/// </summary>
		public virtual void StopAttacking()
		{
			_cancelAttack = true;
			if (_attackCallbackId != 0)
			{
				ZoneServer.Instance.World.Scheduler.Cancel(_attackCallbackId);
				_attackCallbackId = 0;
			}
			if (this is PlayerCharacter player)
				player.StopCasting();
		}

		/// <summary>
		/// Called by <see cref="Attack"/> when a scheduled swing finds the
		/// target outside the Chebyshev attack range. The base class clears
		/// <c>_attackCallbackId</c> after this hook returns, so subclasses
		/// must either cancel or reschedule through their own action
		/// subsystem; they must not re-enter <c>Attack</c> directly or the
		/// loop will desync with the scheduler's notion of "next swing".
		/// <para>
		/// Default implementation: no-op (the caller already cancels the
		/// callback). This is the right behaviour for plain melee monsters
		/// whose combat does not chase; PlayerCharacter overrides this to
		/// feed the target back into the chase path so the attack sequence
		/// resumes automatically when the target steps back into range.
		/// </para>
		/// </summary>
		protected virtual void OnAttackTargetOutOfRange(Character target, int attackRange)
		{
		}

		/// <summary>
		/// Read-only accessor for <c>_attackCallbackId</c>, exposed for
		/// tests in <c>Tests.Shared</c> (the assembly is marked
		/// <c>InternalsVisibleTo</c> by <c>Properties/AssemblyInfo.cs</c>).
		/// Lets the headless test fixture observe that the per-swing range
		/// recheck cleared the auto-attack callback after rejecting a swing.
		/// </summary>
		internal long AttackCallbackIdForTests => this._attackCallbackId;

		/// <summary>
		/// Read-only accessor for <c>_cancelAttack</c>. Same rationale as
		/// <see cref="AttackCallbackIdForTests"/>: tests must observe that
		/// <c>StopAttacking</c> actually flips the cancellation flag.
		/// </summary>
		internal bool CancelAttackFlagForTests => this._cancelAttack;


		/// <summary>
		/// Restores the specified amount of health points (HP) to the entity.
		/// </summary>
		/// <remarks>This method increases the entity's current HP by the specified amount.  If the resulting HP
		/// exceeds the maximum allowed, it may be capped at the maximum value.</remarks>
		/// <param name="healAmount">The amount of health points to restore. Must be a positive integer.</param>
		public void HealHp(int healAmount)
		{
			// SC_BERSERK blocks all incoming heals (regen tick, item
			// use, AL_HEAL, party heal — every caller funnels through
			// here, so guarding once at the entry point keeps every
			// source consistent with eAthena status_calc_heal).
			if (this.StatusEffects?.Has(StatusId.Berserk) == true)
				return;

			this.Parameters.Modify(ParameterType.Hp, healAmount);

			Sabine.Zone.Battle.CombatEvents.RaiseCharacterHealed(this, healAmount);
		}

		internal virtual bool IsHostileTo(Character target)
		{
			return (this is Monster && target is PlayerCharacter) || (this is PlayerCharacter && target is Monster);
		}

		/// <summary>
		/// Tries to reduce the SP by the given amount. If the character
		/// doesn't have enough SP, the method returns false without
		/// modifying the SP.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public bool TrySpendSp(int amount)
		{
			if (this.Parameters.Sp < amount)
				return false;

			this.Parameters.Modify(ParameterType.Sp, -amount);
			return true;
		}

		/// <summary>
		/// Returns true if the character is in range to use the skill on
		/// the given position.
		/// </summary>
		/// <param name="skill"></param>
		/// <param name="targetPos"></param>
		/// <returns></returns>
		public bool InUseRange(Skill skill, Position targetPos)
		{
			var pos = this.Position;
			var range = skill.Range;

			return pos.InRange(targetPos, range);
		}

		/// <summary>
		/// Plays a special effect at the character's position, visible
		/// to all players in sight. Mirrors rAthena's <c>specialeffect</c>.
		/// </summary>
		/// <param name="effectId">Numeric effect id (raw rAthena EF_* constant).</param>
		public void SendSpecialEffect(int effectId)
		{
			Send.ZC_NOTIFY_EFFECT(this, effectId);
		}

		/// <summary>
		/// Plays a special effect at the character's position using the
		/// later, more general effect packet. Mirrors rAthena's
		/// <c>specialeffect2</c>.
		/// </summary>
		public void SendSpecialEffect2(EffectId effectId)
		{
			Send.ZC_NOTIFY_EFFECT2(this, effectId);
		}

		/// <summary>
		/// Plays a sound effect at this character's position. Mirrors
		/// rAthena's <c>soundeffect</c>. Alpha-era clients have no
		/// sound packet — call is logged and skipped.
		/// </summary>
		/// <param name="wav">WAV file name.</param>
		/// <param name="type">0 = play once, 1 = stop.</param>
		public void PlaySound(string wav, int type)
		{
			if (Game.Version >= Versions.S500)
			{
				Network.Send.ZC_SOUND(this, wav, (byte)type, 0);
				return;
			}

			Log.Debug("Character.PlaySound: stubbed for alpha client (character='{0}', wav='{1}', type={2}).", this.Name, wav, type);
		}

		/// <summary>
		/// Sends a chat-bubble message above the character to nearby
		/// players. Covers both rAthena's <c>npctalk</c> (when this is an
		/// <see cref="Npc"/>) and <c>unittalk</c> (any character).
		/// </summary>
		/// <param name="message">The message to display.</param>
		public void Talk(string message)
		{
			if (this.Map == null || this.Map == Sabine.Zone.World.Maps.Map.Limbo)
				return;

			Send.ZC_NOTIFY_CHAT(this, message);
		}
	}
}
