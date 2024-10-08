﻿using System;
using Sabine.Shared;
using Sabine.Shared.Configuration.Files;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities.Components.Characters;
using Sabine.Zone.World.Maps;
using Shared.Const;
using Yggdrasil.Scheduling;
using Yggdrasil.Util;

namespace Sabine.Zone.World.Entities
{
	/// <summary>
	/// A character that can interact with the world and can be
	/// interacted with, such as a player or an NPC.
	/// </summary>
	public abstract class Character : IEntryCharacter, IUpdateable
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
		public abstract int ClassId { get; protected set; }

		/// <summary>
		/// Gets or sets the class id that the character will appear as.
		/// </summary>
		public int DisplayClassId
		{
			get => _displayClassId == -1 ? this.ClassId : _displayClassId;
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
		/// Returns a character's weapon id, defining what weapon they
		/// can be seen holding during combat.
		/// </summary>
		public virtual int WeaponId { get; set; }

		/// <summary>
		/// Returns a character's top headgear look, specifying what headgear
		/// they're wearing.
		/// </summary>
		public virtual int HeadTopId { get; set; }

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
		/// Returns the character's speed parameter.
		/// </summary>
		public int Speed => this.Parameters.Speed;

		/// <summary>
		/// Returns the character's movement controller.
		/// </summary>
		public MovementController Controller { get; protected set; }

		/// <summary>
		/// Returns the character's parameters.
		/// </summary>
		public Parameters Parameters { get; protected set; }

		/// <summary>
		/// Returns the character's components.
		/// </summary>
		public CharacterComponents Components { get; } = new CharacterComponents();

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
		/// Initializes character.
		/// </summary>
		public Character()
		{
			this.Components.Add(this.Controller = new MovementController(this));
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
		/// Warps character to the given location.
		/// </summary>
		/// <param name="location"></param>
		public abstract void Warp(Location location);

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
		public void StartAttacking(Character target, bool autoAttack)
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
			var target = (Character)state.Arguments[0];
			this.Attack(target, true);
		}

		/// <summary>
		/// Makes character attack the given target.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="autoAttack"></param>
		private void Attack(Character target, bool autoAttack)
		{
			var attacker = this;

			if (_cancelAttack || target.IsDead || target.Map != attacker.Map)
			{
				_attackCallbackId = 0;
				return;
			}

			var rnd = RandomProvider.Get();

			var hitChance = Math.Max(5, 80 + attacker.Parameters.Hit - target.Parameters.Flee);
			var damage = 0; // Miss

			if (rnd.Next(100) < hitChance)
			{
				damage = rnd.Next(attacker.Parameters.AttackMin, attacker.Parameters.AttackMax + 1);

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

			Send.ZC_NOTIFY_ACT.Attack(attacker, attacker.Handle, target.Handle, Game.GetTick(), ActionType.Attack, damage, attackMotionDelay, damageMotionDelay);

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
		public void StopAttacking()
		{
			_cancelAttack = true;
		}
	}
}
