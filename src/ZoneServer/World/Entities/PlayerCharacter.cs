﻿using System;
using Sabine.Shared.Const;
using Sabine.Shared.Util;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.World.Maps;

namespace Sabine.Zone.World.Entities
{
	/// <summary>
	/// Represents a player character.
	/// </summary>
	public class PlayerCharacter
	{
		/// <summary>
		/// Gets or sets the connection that controls this player.
		/// </summary>
		public ZoneConnection Connection { get; set; } = new DummyConnection();

		/// <summary>
		/// Returns this character's username.
		/// </summary>
		public string Username => this.Connection.Account.Username;

		/// <summary>
		/// Returns this character's session id.
		/// </summary>
		public int SessionId => this.Connection.Account.SessionId;

		/// <summary>
		/// Gets or sets this character's id.
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets this character's name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Returns this character's/account's sex.
		/// </summary>
		public Sex Sex => this.Connection.Account.Sex;

		/// <summary>
		/// Gets or sets this character's job.
		/// </summary>
		public JobId JobId { get; set; } = JobId.Novice;

		/// <summary>
		/// Gets or sets the name of the map the character is on.
		/// </summary>
		public string MapName { get; set; } = "prt_vilg01";

		/// <summary>
		/// Gets or sets the character's current position.
		/// </summary>
		public Position Position { get; set; } = new Position(100, 80);

		/// <summary>
		/// Gets or sets the character's speed.
		/// </summary>
		public int Speed { get; set; } = 200;

		/// <summary>
		/// Gets or sets the character's hair.
		/// </summary>
		public int HairId { get; set; }

		/// <summary>
		/// Gets or sets the character's weapon.
		/// </summary>
		public int WeaponId { get; set; }

		/// <summary>
		/// Gets or sets how many HP the character currently has.
		/// </summary>
		public int Hp { get; set; } = 40;

		/// <summary>
		/// Gets or sets the character's maximum amount of HP.
		/// </summary>
		public int HpMax { get; set; } = 40;

		/// <summary>
		/// Gets or sets how many SP the character currently has.
		/// </summary>
		public int Sp { get; set; } = 40;

		/// <summary>
		/// Gets or sets the character's maximum amount of SP.
		/// </summary>
		public int SpMax { get; set; } = 40;

		/// <summary>
		/// Gets or sets the character's STR stat.
		/// </summary>
		public int Str { get; set; } = 1;

		/// <summary>
		/// Gets or sets the character's AGI stat.
		/// </summary>
		public int Agi { get; set; } = 1;

		/// <summary>
		/// Gets or sets the character's VIT stat.
		/// </summary>
		public int Vit { get; set; } = 1;

		/// <summary>
		/// Gets or sets the character's INT stat.
		/// </summary>
		public int Int { get; set; } = 1;

		/// <summary>
		/// Gets or sets the character's DEX stat.
		/// </summary>
		public int Dex { get; set; } = 1;

		/// <summary>
		/// Gets or sets the character's LUK stat.
		/// </summary>
		public int Luk { get; set; } = 1;

		/// <summary>
		/// Gets or sets the character's stat points.
		/// </summary>
		public int StatPoints { get; set; } = 10;

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the STR stat by one.
		/// </summary>
		public int StrNeeded => (1 + (this.Str + 9) / 10);

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the AGI stat by one.
		/// </summary>
		public int AgiNeeded => (1 + (this.Agi + 9) / 10);

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the VIT stat by one.
		/// </summary>
		public int VitNeeded => (1 + (this.Vit + 9) / 10);

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the INT stat by one.
		/// </summary>
		public int IntNeeded => (1 + (this.Int + 9) / 10);

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the DEX stat by one.
		/// </summary>
		public int DexNeeded => (1 + (this.Dex + 9) / 10);

		/// <summary>
		/// Returns the amount of stat points necessary to increase
		/// the LUK stat by one.
		/// </summary>
		public int LukNeeded => (1 + (this.Luk + 9) / 10);

		/// <summary>
		/// Returns the character's current min attack.
		/// </summary>
		public int AtkMin { get; set; } = 10;

		/// <summary>
		/// Returns the character's current max attack.
		/// </summary>
		public int AtkMax { get; set; } = 15;

		/// <summary>
		/// Returns the character's current magic attack.
		/// </summary>
		public int Matk { get; set; } = 1;

		/// <summary>
		/// Returns the character's current defense.
		/// </summary>
		public int Defense { get; set; } = 7;

		/// <summary>
		/// Returns the character's current weight.
		/// </summary>
		public int Weight { get; set; } = 10;

		/// <summary>
		/// Returns the character's current max weight.
		/// </summary>
		public int WeightMax { get; set; } = 8000;

		/// <summary>
		/// Returns the number of skill points the character has to assign.
		/// </summary>
		public int SkillPoints { get; set; } = 10;

		/// <summary>
		/// Gets or sets the character's base experience points.
		/// </summary>
		public int BaseExp { get; set; } = 10;

		/// <summary>
		/// Gets or sets the character's job experience points.
		/// </summary>
		public int JobExp { get; set; } = 5;

		/// <summary>
		/// Returns the amount of experience points necessary to reach
		/// the next base level.
		/// </summary>
		public int BaseExpNeeded { get; set; } = 150;

		/// <summary>
		/// Returns the amount of experience points necessary to reach
		/// the next job level.
		/// </summary>
		public int JobExpNeeded { get; set; } = 75;

		/// <summary>
		/// Gets or sets the character's current base level.
		/// </summary>
		public int BaseLevel { get; set; } = 1;

		/// <summary>
		/// Gets or sets the character's current base level.
		/// </summary>
		public int JobLevel { get; set; } = 1;

		/// <summary>
		/// Gets or sets how many Zeny the character has.
		/// </summary>
		public int Zeny { get; set; }

		/// <summary>
		/// Returns a reference to the map the character is currently on.
		/// </summary>
		public Map Map { get; set; } = Map.Limbo;

		/// <summary>
		/// Sends a server message to the character's client that is
		/// displayed in the chat log.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public void ServerMessage(string format, params object[] args)
		{
			if (args.Length > 0)
				format = string.Format(format, args);

			var message = string.Format(Localization.Get("[Server] : {0}"), format);

			Send.ZC_NOTIFY_CHAT(this, 0, message);
		}

		/// <summary>
		/// Returns the value for the given stat.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public int GetStat(ParameterType type)
		{
			switch (type)
			{
				case ParameterType.Str: return this.Str;
				case ParameterType.Agi: return this.Agi;
				case ParameterType.Vit: return this.Vit;
				case ParameterType.Int: return this.Int;
				case ParameterType.Dex: return this.Dex;
				case ParameterType.Luk: return this.Luk;

				default:
					throw new ArgumentException($"Invalid stat type '{type}'.");
			}
		}

		/// <summary>
		/// Modifies the given stat and updates the client.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="modifier"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public int ModifyStat(ParameterType type, int modifier)
		{
			if (type < ParameterType.Str || type > ParameterType.Luk)
				throw new ArgumentException($"Invalid stat type '{type}'.");

			var newValue = 0;
			switch (type)
			{
				case ParameterType.Str: newValue = this.Str += modifier; break;
				case ParameterType.Agi: newValue = this.Agi += modifier; break;
				case ParameterType.Vit: newValue = this.Vit += modifier; break;
				case ParameterType.Int: newValue = this.Int += modifier; break;
				case ParameterType.Dex: newValue = this.Dex += modifier; break;
				case ParameterType.Luk: newValue = this.Luk += modifier; break;
			}

			Send.ZC_PAR_CHANGE(this, type, newValue);

			return newValue;
		}

		/// <summary>
		/// Returns the amount of stat points needed to increase the
		/// given stat by one.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public int GetStatPointsNeeded(ParameterType type)
		{
			switch (type)
			{
				case ParameterType.Str: return this.StrNeeded;
				case ParameterType.Agi: return this.AgiNeeded;
				case ParameterType.Vit: return this.VitNeeded;
				case ParameterType.Int: return this.IntNeeded;
				case ParameterType.Dex: return this.DexNeeded;
				case ParameterType.Luk: return this.LukNeeded;

				default:
					throw new ArgumentException($"Invalid stat type '{type}'.");
			}
		}

		/// <summary>
		/// Modifies the character's stat points and updates the client.
		/// </summary>
		/// <param name="modifier"></param>
		public void ModifyStatPoints(int modifier)
		{
			this.StatPoints += modifier;

			Send.ZC_PAR_CHANGE(this, ParameterType.StatPoints, this.StatPoints);
		}
	}
}
