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

		/// <summary>True if Hiding (TF_HIDING) is active on the character.</summary>
		public bool IsHidden => this.StatusEffects?.Has(StatusId.Hiding) == true;

		/// <summary>True if the character is petrified, frozen, asleep, or stunned — i.e. cannot act.</summary>
		public bool IsImmobilized
			=> this.StatusEffects != null && (
				this.StatusEffects.Has(StatusId.Stone)
				|| this.StatusEffects.Has(StatusId.Freeze)
				|| this.StatusEffects.Has(StatusId.Sleep)
				|| this.StatusEffects.Has(StatusId.Stun));

		/// <summary>True while Silence is active — can't cast skills.</summary>
		public bool IsSilenced => this.StatusEffects?.Has(StatusId.Silence) == true;

		/// <summary>
		/// Returns the character's components.
		/// </summary>
		public CharacterComponents Components { get; } = new CharacterComponents();

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
		/// Reduces the character's HP by the given amount, returns the
		/// character's remaining HP.
		/// </summary>
		/// <param name="amount"></param>
		/// <param name="attacker"></param>
		/// <returns></returns>
		public virtual int TakeDamage(int amount, Character attacker)
		{
			if (this.IsDead)
				return 0;

			var remainingHp = this.Parameters.Modify(ParameterType.Hp, -amount);

			// Sleep breaks on the first non-zero hit. Run after the
			// damage is applied so the 1.5x sleep multiplier already
			// took effect on the breaking hit.
			if (amount > 0 && this.StatusEffects?.Has(StatusId.Sleep) == true)
				this.StatusEffects.Stop(StatusId.Sleep);

			if (remainingHp == 0)
				this.Kill(attacker);

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
		private void Attack(Character target, bool autoAttack)
		{
			var attacker = this;

			if (_cancelAttack || target.IsDead || target.Map != attacker.Map || attacker.IsImmobilized || attacker.IsDead)
			{
				_attackCallbackId = 0;
				return;
			}

			var ctx = new Sabine.Zone.Battle.AttackContext(attacker, target);
			var result = Sabine.Zone.Battle.BattleCalculator.Calc(ctx);

			var damage = result.IsMiss ? 0 : result.Damage;

			if (!result.IsMiss)
			{
				target.TakeDamage(damage, attacker);
				target.StunEndTime = DateTime.Now.AddSeconds(1);
				target.Controller.StopMove();
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
		/// Restores the specified amount of health points (HP) to the entity.
		/// </summary>
		/// <remarks>This method increases the entity's current HP by the specified amount.  If the resulting HP
		/// exceeds the maximum allowed, it may be capped at the maximum value.</remarks>
		/// <param name="healAmount">The amount of health points to restore. Must be a positive integer.</param>
		public void HealHp(int healAmount)
		{
			this.Parameters.Modify(ParameterType.Hp, healAmount);
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
		/// Plays a sound effect at this character's position. Stub:
		/// alpha-era clients have no sound effect packet, so this is a
		/// no-op + debug log. Mirrors rAthena's <c>soundeffect</c>.
		/// </summary>
		/// <param name="wav">WAV file name.</param>
		/// <param name="type">0 = play once, 1 = stop.</param>
		public void PlaySound(string wav, int type)
		{
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
