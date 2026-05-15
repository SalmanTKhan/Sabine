using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using Sabine.Shared;
using Sabine.Shared.Configuration.Files;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Shared.Network;
using Sabine.Shared.Network.Helpers;
using Sabine.Shared.World;
using Sabine.Zone.Network.Helpers;
using Sabine.Zone.Skills;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;
using Sabine.Zone.World.Chats;
using Sabine.Zone.World.Shops;
using Yggdrasil.Logging;
using Yggdrasil.Util;

namespace Sabine.Zone.Network
{
	public enum EnterError : byte
	{
		AccessDenied = 0
	}

	public enum PurchaseResult : byte
	{
		Success = 0,
		NotEnoughZeny = 1,
		Overweight = 2,
		ItemCountOver = 3,
		Unknown = 4,
	}

	public enum SellResult : byte
	{
		Success = 0,
		Overweight = 1,
		CannotSell = 2,
	}

	/// <summary>
	/// Packet senders.
	/// </summary>
	public static partial class Send
	{
		/// <summary>
		/// Sends account id to client.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_AID(ZoneConnection conn)
		{
			// Starting some time after beta 1, the client expected the
			// account id to be sent upon connection, or it won't react to
			// any packets...? Any only several years later they upgraded
			// to an actual packet :+1:

			if (Game.Version < Versions.S2500)
			{
				var buffer = ArrayPool<byte>.Shared.Rent(sizeof(int));
				BinaryPrimitives.WriteInt32LittleEndian(buffer, conn.Account.Id);

				conn.Send(buffer, sizeof(int), static (data, len, type) => ArrayPool<byte>.Shared.Return(data));
			}
			else
			{
				using var packet = Packet.Rent(Op.ZC_AID);
				packet.PutInt(conn.Account.Id);

				conn.Send(packet);
			}
		}

		/// <summary>
		/// Accepts connection request, makes client load map.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="character"></param>
		public static void ZC_ACCEPT_ENTER(ZoneConnection conn, PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_ACCEPT_ENTER);

			if (Game.Version >= Versions.Sabine)
			{
				// rA PACKET_ZC_ACCEPT_ENTER 0xa18 at PACKETVER >= 20141022
				// (except the 20141022-20160329 window which uses 0x2eb
				// without sex) — see packets.hpp:573, map/clif.cpp
				// clif_authok. 12-byte body: startTime + posDir + xSize +
				// ySize + font + sex.
				packet.PutInt(Game.GetTick());                                     // startTime
				packet.AddPackedPosition(character.Position, character.Direction); // posDir
				packet.PutByte(5);                                                 // xSize
				packet.PutByte(5);                                                 // ySize
				packet.PutShort(0);                                                // font
				packet.PutByte((byte)character.Sex);                               // sex
			}
			else
			{
				packet.PutInt(character.Id);
				packet.AddPackedPosition(character.Position, character.Direction);
				packet.PutShort(0);

				if (Game.Version >= Versions.S2500)
					packet.PutShort(0);
			}

			conn.Send(packet);
		}

		/// <summary>
		/// Refuses connection request.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="reason"></param>
		public static void ZC_REFUSE_ENTER(ZoneConnection conn, EnterError reason)
		{
			using var packet = Packet.Rent(Op.ZC_REFUSE_ENTER);

			packet.PutByte((byte)reason);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends server time to client.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="tick"></param>
		public static void ZC_NOTIFY_TIME(ZoneConnection conn, int tick)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_TIME);
			packet.PutInt(tick);

			conn.Send(packet);
		}

		/// <summary>
		/// Makes character appear on the player's client.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_NOTIFY_STANDENTRY(PlayerCharacter player, IStandEntry character)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_STANDENTRY);
			packet.AddStandEntry(character);

			player.Connection.Send(packet);
		}

		/// <summary>
		/// Makes character appear on the clients around it.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_NOTIFY_STANDENTRY(IStandEntry character)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_STANDENTRY);
			packet.AddStandEntry(character);

			character.Map.Broadcast(packet, character, BroadcastTargets.AllButSource);
		}

		/// <summary>
		/// Makes NPC appear on clients of players  around it.
		/// </summary>
		/// <remarks>
		/// Currently the only known difference to ZC_NOTIFY_STANDENTRY
		/// is that this packet won't spawn character classes, as it
		/// fails to find the respective sprite's ACT file.
		/// ZC_NOTIFY_STANDENTRY meanwhile has a check for whether the
		/// class id is < 32, where the NPC sprites begin, and it seems
		/// to be handling both players and NPCs well.
		/// </remarks>
		/// <param name="character"></param>
		public static void ZC_NOTIFY_STANDENTRY_NPC(IStandEntry character)
		{
			// In the alpha, class id 32 is a warp, in the form of a round
			// shadow sprite, and 63 seems to be an effect, that uses the
			// speed field as the effect id.

			using var packet = Packet.Rent(Op.ZC_NOTIFY_STANDENTRY_NPC);
			packet.AddStandEntryNpc(character);

			character.Map.Broadcast(packet, character, BroadcastTargets.AllButSource);
		}

		/// <summary>
		/// Makes NPC appear on the player's client.
		/// </summary>
		/// <param name="player"></param>
		public static void ZC_NOTIFY_STANDENTRY_NPC(PlayerCharacter player, IStandEntry character)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_STANDENTRY_NPC);
			packet.AddStandEntryNpc(character);

			player.Connection.Send(packet);
		}

		/// <summary>
		/// Makes character appear on clients of players around it with
		/// a spawn effect.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_NOTIFY_NEWENTRY(IStandEntry character)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_NEWENTRY);
			packet.AddNewEntry(character);

			character.Map.Broadcast(packet, character, BroadcastTargets.AllButSource);
		}

		/// <summary>
		/// Makes character who is currently moving appear on clients of
		/// players around it, moving between the given positions.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static void ZC_NOTIFY_MOVEENTRY(IStandEntry character, Position from, Position to)
		{
			// Same as the others, but with a packed move instead of the
			// character's packed position.

			using var packet = Packet.Rent(Op.ZC_NOTIFY_MOVEENTRY);
			packet.AddMoveEntry(character, from, to);

			character.Map.Broadcast(packet, character, BroadcastTargets.AllButSource);
		}

		/// <summary>
		/// Makes character who is currently moving appear on player's
		/// client, moving between the given positions.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="character"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static void ZC_NOTIFY_MOVEENTRY(PlayerCharacter player, IStandEntry character, Position from, Position to)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_MOVEENTRY);
			packet.AddMoveEntry(character, from, to);

			player.Connection.Send(packet);
		}

		/// <summary>
		/// Removes character with the given handle from the player's client.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="handle"></param>
		/// <param name="type"></param>
		public static void ZC_NOTIFY_VANISH(PlayerCharacter player, int handle, DisappearType type)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_VANISH);
			packet.PutInt(handle);
			packet.PutByte((byte)type);

			player.Connection.Send(packet);
		}

		/// <summary>
		/// ZC_PET_ACT — pet action / emote echo. Wire (EP4): petHandle
		/// (4) + actionId (4) + a 2-byte pad slot reserved by the
		/// PacketTable for length 10. Routed to the owner only — wild
		/// player-character bystanders don't see other players'
		/// pet emotes, mirroring eAthena's clif_pet_performance scope.
		/// </summary>
		public static void ZC_PET_ACT(PlayerCharacter owner, int petHandle, int actionId)
		{
			using var packet = Packet.Rent(Op.ZC_PET_ACT);
			packet.PutInt(petHandle);
			packet.PutInt(actionId);
			packet.PutShort(0);
			owner.Connection.Send(packet);
		}

		/// <summary>
		/// Removes character from clients around it.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="type"></param>
		public static void ZC_NOTIFY_VANISH(Character character, DisappearType type)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_VANISH);
			packet.PutInt(character.Handle);
			packet.PutByte((byte)type);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Accepts a request to quit the game.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_ACCEPT_QUIT(ZoneConnection conn)
		{
			using var packet = Packet.Rent(Op.ZC_ACCEPT_QUIT);
			conn.Send(packet);
		}

		/// <summary>
		/// Refuses a request to quit the game.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_REFUSE_QUIT(ZoneConnection conn)
		{
			using var packet = Packet.Rent(Op.ZC_REFUSE_QUIT);
			conn.Send(packet);
		}

		/// <summary>
		/// Makes character move from one position to the other on clients
		/// of players around it.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static void ZC_NOTIFY_MOVE(Character character, Position from, Position to)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_MOVE);

			packet.PutInt(character.Handle);
			packet.AddPackedMove(from, to, 8, 8);
			packet.PutInt(Game.GetTick());

			character.Map.Broadcast(packet, character, BroadcastTargets.AllButSource);
		}

		/// <summary>
		/// Makes player's character move, as response to a move request.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static void ZC_NOTIFY_PLAYERMOVE(PlayerCharacter character, Position from, Position to)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_PLAYERMOVE);

			packet.PutInt(Game.GetTick());
			packet.AddPackedMove(from, to, 8, 8);

			character.Connection.Send(packet);

			// Update party minimap arrows on every move start. Throttling
			// is left to the client; eAthena does the same.
			character.Party?.BroadcastPosition(character);
		}

		/// <summary>
		/// Stops character's movement on the clients of players nearby.
		/// </summary>
		/// <remarks>
		/// The (alpha) client doesn't react to this packet for its
		/// controlled character, only others.
		/// </remarks>
		/// <param name="character"></param>
		/// <param name="stopPos"></param>
		public static void ZC_STOPMOVE(Character character, Position stopPos)
		{
			using var packet = Packet.Rent(Op.ZC_STOPMOVE);

			packet.PutInt(character.Handle);
			packet.PutShort((short)stopPos.X);
			packet.PutShort((short)stopPos.Y);

			character.Map.Broadcast(packet, character, BroadcastTargets.AllButSource);
		}

		/// <summary>
		/// Sends the target's name to the character's client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="target"></param>
		public static void ZC_ACK_REQNAME(PlayerCharacter character, Character target)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REQNAME);

			packet.PutInt(target.Handle);

			// The first string is displayed in parantheses after the
			// character name. It seems like it's intended for the account
			// name, because that's what the client displays for the
			// player character itself. This might indicate that they had
			// planned a "team name" kind of feature, similar to ToS, to
			// have a common identifier between characters on one account.
			// Not a terrible idea, but sending the account names of other
			// players is not exactly ideal, so... maybe let's not do
			// that. If we leave it empty, the client will just not
			// display it. However, maybe we could add a display name for
			// the accounts, which could be used here. Also: 16-24 free
			// bytes for monster HP!

			var secondaryName = ""; // target.Username
			var mainName = target.Name;

			if (target is Monster)
			{
				var hpDisplayType = ZoneServer.Instance.Conf.World.DisplayMonsterHp;

				switch (hpDisplayType)
				{
					case DisplayMonsterHpType.Percentage:
					{
						// Round to ceiling in case percentage falls below
						// 1% and clamp it to 0~100, in case the calculation
						// result ends up above 100. (Floats ftw, am I right?)
						secondaryName = string.Format("{0}%", Math2.Clamp(0, 100, Math.Ceiling(100f / target.Parameters.HpMax * target.Parameters.Hp)));
						break;
					}
					case DisplayMonsterHpType.Actual:
					{
						secondaryName = string.Format("{0}/{1}", target.Parameters.Hp, target.Parameters.HpMax);
						break;
					}
				}
			}

			// Append secName to targetName if the client doesn't support
			// a secondary name anymore.
			if (Game.Version > Versions.Alpha && !string.IsNullOrWhiteSpace(secondaryName))
				mainName = string.Format("{0} ({1})", mainName, secondaryName);

			// This is still sent in Beta1, but the client doesn't display
			// it anymore.
			if (Game.Version < Versions.Beta2)
				packet.PutString(secondaryName, Sizes.CharacterNames);

			packet.PutString(mainName, Sizes.CharacterNames);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the given parameter on the client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="type"></param>
		public static void ZC_PAR_CHANGE(PlayerCharacter character, ParameterType type)
		{
			var value = character.Parameters.Get(type);
			ZC_PAR_CHANGE(character, type, value);
		}

		public static void ZC_PAR_CHANGE(PlayerCharacter character, ParameterType type, int value)
		{
			if (type.IsLong())
				throw new ArgumentException($"Parameter type '{type}' should be sent using ZC_LONGPAR_CHANGE.");

			if (type == ParameterType.Weight || type == ParameterType.WeightMax)
				value /= 10;

			using var packet = Packet.Rent(Op.ZC_PAR_CHANGE);

			packet.PutShort((short)type);

			// Apparently Gravity thought it was a good idea to upgrade
			// ZC_PAR_CHANGE to integers, but keep two separate packets
			// for some reason. The two packets still handle specific
			// parameters, you can't use one for both.
			if (Game.Version < Versions.Beta2)
				packet.PutShort((short)value);
			else
				packet.PutInt(value);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the given parameter on the client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="type"></param>
		public static void ZC_LONGPAR_CHANGE(PlayerCharacter character, ParameterType type)
		{
			var value = character.Parameters.Get(type);

			// Always display job level and EXP as 0 if the feature
			// isn't enabled
			if (type == ParameterType.JobExp || type == ParameterType.JobExpNeeded)
			{
				if (!ZoneServer.Instance.Data.Features.IsEnabled(FeatureId.JobLevels))
					value = 0;
			}

			ZC_LONGPAR_CHANGE(character, type, value);
		}

		/// <summary>
		/// Updates the given parameter on the client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="type"></param>
		/// <param name="value"></param>
		public static void ZC_LONGPAR_CHANGE(PlayerCharacter character, ParameterType type, int value)
		{
			if (!type.IsLong())
				throw new ArgumentException($"Parameter type '{type}' should be sent using ZC_PAR_CHANGE.");

			using var packet = Packet.Rent(Op.ZC_LONGPAR_CHANGE);

			packet.PutShort((short)type);
			packet.PutInt(value);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Response to stat change request.
		/// </summary>
		/// <remarks>
		/// Stat is only updated with the new value if the assigment
		/// was successful.
		/// </remarks>
		/// <param name="character"></param>
		/// <param name="type"></param>
		/// <param name="success"></param>
		/// <param name="value"></param>
		public static void ZC_STATUS_CHANGE_ACK(PlayerCharacter character, ParameterType type, bool success, int value)
		{
			using var packet = Packet.Rent(Op.ZC_STATUS_CHANGE_ACK);

			packet.PutShort((short)type);
			packet.PutByte(success);
			packet.PutByte((byte)value);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates several stats and sub-stats on the character's client.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_STATUS(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_STATUS);

			packet.PutShort((short)character.Parameters.StatPoints);
			packet.PutByte((byte)character.Parameters.Str);
			packet.PutByte((byte)character.Parameters.StrNeeded);
			packet.PutByte((byte)character.Parameters.Agi);
			packet.PutByte((byte)character.Parameters.AgiNeeded);
			packet.PutByte((byte)character.Parameters.Vit);
			packet.PutByte((byte)character.Parameters.VitNeeded);
			packet.PutByte((byte)character.Parameters.Int);
			packet.PutByte((byte)character.Parameters.IntNeeded);
			packet.PutByte((byte)character.Parameters.Dex);
			packet.PutByte((byte)character.Parameters.DexNeeded);
			packet.PutByte((byte)character.Parameters.Luk);
			packet.PutByte((byte)character.Parameters.LukNeeded);

			if (Game.Version < Versions.Beta1)
			{
				packet.PutByte((byte)character.Parameters.AttackMin);
				packet.PutByte((byte)character.Parameters.AttackMax);
				packet.PutByte((byte)character.Parameters.Defense);
				packet.PutByte((byte)character.Parameters.MagicAttack);
			}
			else
			{
				packet.PutShort((short)character.Parameters.Attack);
				packet.PutShort((short)character.Parameters.AttackBonus);
				packet.PutShort((short)character.Parameters.MagicAttackMax);
				packet.PutShort((short)character.Parameters.MagicAttackMin);
				packet.PutShort((short)character.Parameters.MeleeDefense);
				packet.PutShort((short)character.Parameters.MeleeDefenseBonus);
				packet.PutShort((short)character.Parameters.MagicDefense);
				packet.PutShort((short)character.Parameters.MagicDefenseBonus);
				packet.PutShort((short)character.Parameters.Hit);
				packet.PutShort((short)character.Parameters.Flee);
				packet.PutShort((short)character.Parameters.FleeBonus);
				packet.PutShort((short)character.Parameters.Critical);
				packet.PutShort((short)character.Parameters.Aspd);
				packet.PutShort((short)character.Parameters.AspdBonus);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends public chat packet to players around character.
		/// </summary>
		/// <param name="character">The character who is the source of the chat message.</param>
		/// <param name="message">The chat message to send.</param>
		public static void ZC_NOTIFY_CHAT(Character character, string message)
			=> ZC_NOTIFY_CHAT(new SightBroadcastSender(character, BroadcastTargets.AllButSource), character.Handle, message);

		/// <summary>
		/// Sends chat packet to character's client.
		/// </summary>
		/// <param name="character">The character to send the packet to.</param>
		/// <param name="authorHandle">The handle of the author of the chat message.</param>
		/// <param name="message">The chat message to send.</param>
		public static void ZC_NOTIFY_CHAT(PlayerCharacter character, int authorHandle, string message)
			=> ZC_NOTIFY_CHAT(new SingleConnectionSender(character), authorHandle, message);

		/// <summary>
		/// Sends public chat packet for the author's message via sender.
		/// </summary>
		/// <param name="sender">The sender to use for sending the packet.</param>
		/// <param name="authorHandle">The source of the chat message.</param>
		/// <param name="message">The chat message to send.</param>
		public static void ZC_NOTIFY_CHAT<TSender>(TSender sender, int authorHandle, string message) where TSender : ISender
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_CHAT);

			packet.PutInt(authorHandle);
			packet.PutString(message);

			sender.Send(packet);
		}

		/// <summary>
		/// Displays message to the character's client as their own
		/// message, displaying it above their head and/or in a special
		/// color inside a chat.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="message"></param>
		public static void ZC_NOTIFY_PLAYERCHAT(PlayerCharacter character, string message)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_PLAYERCHAT);
			packet.PutString(message);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends a whisper chat message to character's client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="fromName"></param>
		/// <param name="message"></param>
		public static void ZC_WHISPER(PlayerCharacter character, string fromName, string message)
		{
			using var packet = Packet.Rent(Op.ZC_WHISPER);

			packet.PutString(fromName, 16);
			packet.PutString(message);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends a whisper chat message to character's client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="result"></param>
		public static void ZC_ACK_WHISPER(PlayerCharacter character, WhisperResult result)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_WHISPER);
			packet.PutByte((byte)result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts message on all maps, displayed in the center
		/// of the game screen.
		/// </summary>
		/// <param name="message"></param>
		public static void ZC_BROADCAST(string message)
		{
			using var packet = Packet.Rent(Op.ZC_BROADCAST);
			packet.PutString(message);

			var maps = ZoneServer.Instance.World.Maps.GetAll();
			foreach (var map in maps)
				map.Broadcast(packet);
		}

		/// <summary>
		/// Updates the character's sprites for all players near them.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="type"></param>
		/// <param name="value"></param>
		public static void ZC_SPRITE_CHANGE(Character character, SpriteType type, int value)
		{
			using var packet = Packet.Rent(Op.ZC_SPRITE_CHANGE);

			packet.PutInt(character.Handle);
			packet.PutByte((byte)type);

			if (Game.Version >= Versions.Sabine)
			{
				// rA PACKET_ZC_SPRITE_CHANGE at PACKETVER_MAIN_NUM >=
				// 20181121 widens val to uint32 and adds a second uint32
				// val2 (used for the weapon+shield combined update).
				// Sabine routes single-value changes with val2 = 0.
				// Ref: rathena packets_struct.hpp:2591.
				packet.PutInt(value);
				packet.PutInt(0);
			}
			else
			{
				packet.PutByte((byte)value);
			}

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Updates the character's sprites for all players near them.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="type"></param>
		/// <param name="value1"></param>
		/// <param name="value2"></param>
		public static void ZC_SPRITE_CHANGE2(Character character, SpriteType type, int value1, int value2)
		{
			using var packet = Packet.Rent(Op.ZC_SPRITE_CHANGE2);

			packet.PutInt(character.Handle);
			packet.PutByte((byte)type);
			packet.PutShort((short)value1);
			packet.PutShort((short)value2);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Sends the number of players who are online to the client.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="count"></param>
		public static void ZC_USER_COUNT(ZoneConnection conn, int count)
		{
			using var packet = Packet.Rent(Op.ZC_USER_COUNT);
			packet.PutInt(count);

			conn.Send(packet);
		}

		/// <summary>
		/// Makes character use the given emotion.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="emotionId"></param>
		public static void ZC_EMOTION(Character character, EmotionId emotionId)
		{
			using var packet = Packet.Rent(Op.ZC_EMOTION);

			packet.PutInt(character.Handle);
			packet.PutByte((byte)emotionId);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Sends item description to the character's client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="name"></param>
		/// <param name="description"></param>
		public static void ZC_REQ_ITEM_EXPLANATION_ACK(PlayerCharacter character, string name, string description)
		{
			using var packet = Packet.Rent(Op.ZC_REQ_ITEM_EXPLANATION_ACK);

			packet.PutString(name, Sizes.ItemNames);
			packet.PutString(description);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the character's direction on other players' clients.
		/// </summary>
		/// <remarks>
		/// Doesn't affect the character's direction on their own client.
		/// </remarks>
		/// <param name="character"></param>
		/// <param name="direction"></param>
		/// <param name="headTurn"></param>
		public static void ZC_CHANGE_DIRECTION(Character character, Direction direction, HeadTurn headTurn)
		{
			using var packet = Packet.Rent(Op.ZC_CHANGE_DIRECTION);

			packet.PutInt(character.Handle);

			if (Game.Version >= Versions.Beta2)
			{
				packet.PutByte((byte)headTurn);
				packet.PutByte(0);
			}

			packet.PutByte((byte)direction);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Warps character to the given location.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="mapStringId"></param>
		/// <param name="pos"></param>
		public static void ZC_NPCACK_MAPMOVE(PlayerCharacter character, string mapStringId, Position pos)
		{
			var mapFileName = mapStringId + ".gat";

			using var packet = Packet.Rent(Op.ZC_NPCACK_MAPMOVE);

			packet.PutString(mapFileName, 16);
			packet.PutShort((short)pos.X);
			packet.PutShort((short)pos.Y);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Warps character to the given location and makes the client
		/// connect to another zone server to continue.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="mapStringId"></param>
		/// <param name="pos"></param>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		public static void ZC_NPCACK_SERVERMOVE(PlayerCharacter character, string mapStringId, Position pos, string ip, int port)
		{
			var mapFileName = mapStringId + ".gat";

			using var packet = Packet.Rent(Op.ZC_NPCACK_SERVERMOVE);

			packet.PutString(mapFileName, 16);
			packet.PutShort((short)pos.X);
			packet.PutShort((short)pos.Y);
			packet.PutInt(IPAddress.Parse(ip));
			packet.PutShort((short)port);

			if (Game.Version >= Versions.Sabine)
			{
				// rA PACKET_ZC_NPCACK_SERVERMOVE 0xac7 at PACKETVER >= 20170315
				// adds a 128-byte domain field for hostname-based zone
				// routing. Sabine leaves it empty — IP-only routing.
				// Ref: rathena packets.hpp:706, map/clif.cpp clif_changemapserver.
				packet.PutString("", 128);
			}

			character.Connection.Send(packet);
		}

		public static class ZC_NOTIFY_ACT
		{
			/// <summary>
			/// Makes character do an action.
			/// </summary>
			/// <param name="character"></param>
			/// <param name="handleSource"></param>
			/// <param name="handleTarget"></param>
			/// <param name="tick"></param>
			/// <param name="damage"></param>
			/// <param name="type"></param>
			/// <param name="delay1"></param>
			/// <param name="delay2"></param>
			public static void Attack(Character character, int handleSource, int handleTarget, int tick, ActionType type, int damage, int delay1, int delay2)
			{
				// Cap the damage, as the alpha client crashes if the damage
				// is greater than 999. 0 is displayed as "Miss", while
				// negative numbers become 0 damage.
				// Beta1 doesn't crash, regardless of the damage value, but
				// caps the displayed damage at 9999.
				if (Game.Version < Versions.Beta1)
					damage = Math2.Clamp(-1, 999, damage);

				Raw(character, handleSource, handleTarget, tick, type, damage, delay1, delay2, 0, 0);
			}

			/// <summary>
			/// Makes character do an action.
			/// </summary>
			/// <param name="character"></param>
			/// <param name="handleSource"></param>
			/// <param name="type"></param>
			public static void Simple(Character character, int handleSource, ActionType type)
				=> Raw(character, handleSource, 0, 0, type, 0, 0, 0, 0, 0);

			/// <summary>
			/// Makes character do an action.
			/// </summary>
			/// <param name="character"></param>
			/// <param name="handleSource"></param>
			/// <param name="handleTarget"></param>
			/// <param name="tick"></param>
			/// <param name="type"></param>
			/// <param name="arg1"></param>
			/// <param name="arg2"></param>
			/// <param name="arg3"></param>
			/// <param name="arg4"></param>
			public static void Raw(Character character, int handleSource, int handleTarget, int tick, ActionType type, int arg1, int arg2, int arg3, int arg4, int arg5)
			{
				using var packet = Packet.Rent(Op.ZC_NOTIFY_ACT);

				if (Game.Version >= Versions.Sabine)
				{
					// rA PACKET_ZC_NOTIFY_ACT 0x8c8 at PACKETVER >= 20131223:
					// damage and damage2 widened to int32, isSPDamage byte
					// inserted between damage and div. Body = 4 srcID + 4
					// targetID + 4 serverTick + 4 srcSpeed + 4 dmgSpeed +
					// 4 damage + 1 isSPDamage + 2 div + 1 type + 4 damage2,
					// wire = 34.
					// Ref: rathena packets.hpp:1413, map/clif.cpp clif_damage.
					packet.PutInt(handleSource);
					packet.PutInt(handleTarget);
					packet.PutInt(tick);
					packet.PutInt(ScaleToKroAnimationSpeed(arg2));
					packet.PutInt(arg3);
					packet.PutInt(arg1);
					packet.PutByte(0);
					packet.PutShort((short)arg4);
					packet.PutByte((byte)type);
					packet.PutInt(arg5);

					character.Map.Broadcast(packet, character, BroadcastTargets.All);
					return;
				}

				packet.PutInt(handleSource);
				packet.PutInt(handleTarget);
				packet.PutInt(tick);

				if (Game.Version >= Versions.Beta1)
				{
					// kRO Sak EP12+ clients reinterpret srcSpeed as an
					// inverted animation modifier (rA clif.cpp:5213,
					// DEFAULT_ANIMATION_SPEED = 432; >432 caps to 1x,
					// lower = faster). Sabine's AttackMotionDelay is
					// calibrated in raw ms for alpha/beta clients
					// (Novice bare-hand = 800ms), which the EP12
					// client clamps to 1x and renders extra slow.
					// Map ms → modifier so amotion=800 ⇒ 432 (1x) and
					// faster amotions speed up the animation
					// proportionally. Pre-S2500 wire is unchanged.
					var srcSpeed = arg2;
					if (Game.Version >= Versions.S2500)
						srcSpeed = ScaleToKroAnimationSpeed(arg2);

					packet.PutInt(srcSpeed);
					packet.PutInt(arg3);
				}

				packet.PutShort((short)arg1);

				if (Game.Version >= Versions.Beta1)
					packet.PutShort((short)arg4);

				packet.PutByte((byte)type);

				if (Game.Version >= Versions.Beta2)
					packet.PutShort((short)arg5);

				character.Map.Broadcast(packet, character, BroadcastTargets.All);
			}

			// rAthena's DEFAULT_ANIMATION_SPEED. 432 = 1x animation
			// on the modern kRO client; below caps to faster.
			private const int KroDefaultAnimationSpeed = 432;

			// Sabine's bare-hand Novice amotion. Anchors the mapping
			// so the legacy default (800ms) lines up exactly with the
			// client's 1x.
			private const int SabineBaselineAmotionMs = 800;

			/// <summary>
			/// Maps a Sabine amotion (ms) onto kRO's inverted animation
			/// modifier scale used by Sak EP12+ clients. Linear: 800ms
			/// ⇒ 432 (1x cap), 400ms ⇒ 216 (2x), 200ms ⇒ 108 (4x). Capped
			/// at <c>KroDefaultAnimationSpeed</c> so larger amotions
			/// don't underflow into the &gt;432 ignore-band.
			/// </summary>
			internal static int ScaleToKroAnimationSpeed(int amotionMs)
			{
				if (amotionMs <= 0)
					return KroDefaultAnimationSpeed;

				var scaled = amotionMs * KroDefaultAnimationSpeed / SabineBaselineAmotionMs;
				if (scaled < 1) scaled = 1;
				if (scaled > KroDefaultAnimationSpeed) scaled = KroDefaultAnimationSpeed;
				return scaled;
			}
		}

		/// <summary>
		/// Informs the character that their attack failed due to the
		/// target being out of range, making them get closer.
		/// </summary>
		/// <param name="character">The character whose attack failed.</param>
		/// <param name="target">The target character that is out of range.</param>
		/// <param name="range">The range into which the character should get to attack.</param>
		public static void ZC_ATTACK_FAILURE_FOR_DISTANCE(PlayerCharacter character, Character target, int range)
		{
			using var packet = Packet.Rent(Op.ZC_ATTACK_FAILURE_FOR_DISTANCE);

			packet.PutInt(target.Handle);
			packet.PutShort((short)target.Position.X);
			packet.PutShort((short)target.Position.Y);
			packet.PutShort((short)character.Position.X);
			packet.PutShort((short)character.Position.Y);
			packet.PutShort((short)range);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Displays dialog on character's client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="npcHandle"></param>
		/// <param name="message"></param>
		public static void ZC_SAY_DIALOG(PlayerCharacter character, int npcHandle, string message)
		{
			using var packet = Packet.Rent(Op.ZC_SAY_DIALOG);

			packet.PutInt(npcHandle);
			packet.PutString(message);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Shows a button to continue or end the dialog.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="npcHandle"></param>
		public static void ZC_WAIT_DIALOG(PlayerCharacter character, int npcHandle)
		{
			using var packet = Packet.Rent(Op.ZC_WAIT_DIALOG);
			packet.PutInt(npcHandle);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Closes the dialog window.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="npcHandle"></param>
		public static void ZC_CLOSE_DIALOG(PlayerCharacter character, int npcHandle)
		{
			using var packet = Packet.Rent(Op.ZC_CLOSE_DIALOG);
			packet.PutInt(npcHandle);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Displays a list of options to choose from during a dialog.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="npcHandle"></param>
		/// <param name="optionsString">Options separated by colons.</param>
		public static void ZC_MENU_LIST(PlayerCharacter character, int npcHandle, string optionsString)
		{
			using var packet = Packet.Rent(Op.ZC_MENU_LIST);

			packet.PutInt(npcHandle);
			packet.PutString(optionsString);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Makes client close the connection and display a message
		/// for why this disconnect was requested.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="reason"></param>
		public static void SC_NOTIFY_BAN(ZoneConnection conn, DisconnectReason reason)
		{
			using var packet = Packet.Rent(Op.SC_NOTIFY_BAN);
			packet.PutByte((byte)reason);

			conn.Send(packet);
		}

		/// <summary>
		/// Either makes given item appear in character's inventory or
		/// displays a message for why they couldn't pick up an item.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="item"></param>
		/// <param name="result"></param>
		public static void ZC_ITEM_PICKUP_ACK(PlayerCharacter character, Item item, PickUpResult result)
			=> ZC_ITEM_PICKUP_ACK(character, item, item.Amount, result);

		/// <summary>
		/// Adds item to inventory with the given amount. If the item
		/// already exists, the amount is added to the existing stack.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="item"></param>
		/// <param name="result"></param>
		public static void ZC_ITEM_PICKUP_ACK(PlayerCharacter character, Item item, int amount, PickUpResult result)
		{
			using var packet = Packet.Rent(Op.ZC_ITEM_PICKUP_ACK);

			if (Game.Version >= Versions.Sabine)
			{
				// rA PACKET_ZC_ITEM_PICKUP_ACK at PACKETVER_MAIN_NUM >=
				// 20181121: 61-byte body — Index(2) + count(2) + nameid(4)
				// + IsIdentified(1) + IsDamaged(1) + refiningLevel(1) +
				// slot[8] + location(4) + type(1) + result(1) +
				// HireExpireDate(4) + bindOnEquipType(2) +
				// option_data[5](25) + favorite(1) + look(2). Wire = 63.
				// Ref: rathena packets_struct.hpp:540, map/clif.cpp clif_additem.
				packet.PutShort((short)item.InventoryId);
				packet.PutShort((short)amount);
				packet.PutInt(item.ClassId);
				packet.PutByte(item.IsIdentified);
				packet.PutByte(item.IsDamaged);
				packet.PutByte(item.RefineLevel);
				packet.AddSlotInfoForPickupAck(item);
				packet.PutInt((int)item.GetSlotsFor(character));
				packet.PutByte((byte)item.Type);
				packet.PutByte((byte)result);
				packet.PutInt(item.HireExpireDate);
				packet.PutShort((short)item.BindOnEquipType);
				packet.AddItemOptionsForPickupAck(item);
				packet.PutByte((byte)(item.IsFavorite ? 1 : 0));
				packet.PutShort((short)(item.SpriteNumber != 0 ? item.SpriteNumber : item.ClassId));

				character.Connection.Send(packet);
				return;
			}

			// eA at PV >= 20071002 widens the pickup ack body to add
			// L expire_time + W bindOnEquipType — historical-era tables
			// route the base Op through wire 0x02D4 size 29 at S2500+,
			// older clients keep the legacy 0x003B/23 layout.
			// Ref: eA src/map/clif.c clif_additem (line 2078).
			var useV3 = Game.Version >= Versions.S2500;

			packet.PutShort((short)item.InventoryId);
			packet.PutShort((short)amount);

			if (Game.Version < Versions.Beta2)
			{
				packet.PutString(item.StringId, Sizes.ItemNames);
				packet.PutByte((byte)item.Type);
				packet.PutByte((byte)item.GetSlotsFor(character));
			}
			else
			{
				packet.PutShort((short)item.ClassId);
				packet.PutByte(item.IsIdentified);
				packet.PutByte(0);   // Attribute / IsDamaged
				packet.PutByte(0);   // Refine
				packet.PutShort(0);  // Card1
				packet.PutShort(0);  // Card2
				packet.PutShort(0);  // Card3
				packet.PutShort(0);  // Card4
				packet.PutShort((short)item.GetSlotsFor(character));
				packet.PutByte((byte)item.Type);
			}

			packet.PutByte((byte)result);

			if (useV3)
			{
				packet.PutInt(0);    // expire_time (rental)
				packet.PutShort(0);  // bindOnEquipType
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the character's item and etc inventory tabs using
		/// the given list of items, filtering it for normal items.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="items"></param>
		public static void ZC_NORMAL_ITEMLIST(PlayerCharacter character, IEnumerable<Item> items)
		{
			// Sabine writes rA V6 26-byte entries (uint32 ITID + bonus
			// options block). S2500 writes the 22-byte V3 layout. Older
			// clients keep the legacy 18-byte entry shape.
			var useV6 = Game.Version >= Versions.Sabine;
			var useV3 = !useV6 && Game.Version >= Versions.S2500;
			using var packet = Packet.Rent(Op.ZC_NORMAL_ITEMLIST);

			foreach (var item in items)
			{
				if (item.Type.IsEquip())
					continue;

				if (useV6) packet.AddNormalItemV6(item);
				else if (useV3) packet.AddNormalItemV3(item);
				else packet.AddNormalItem(item);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens storage window if it's not open yet and fills it with
		/// the given items, filtering for non-equip.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="items"></param>
		public static void ZC_STORE_NORMAL_ITEMLIST(PlayerCharacter character, IEnumerable<Item> items)
		{
			var useV6 = Game.Version >= Versions.Sabine;
			var useV3 = !useV6 && Game.Version >= Versions.S2500;
			using var packet = Packet.Rent(Op.ZC_STORE_NORMAL_ITEMLIST);

			foreach (var item in items)
			{
				if (item.Type.IsEquip())
					continue;

				if (useV6) packet.AddNormalItemV6(item);
				else if (useV3) packet.AddNormalItemV3(item);
				else packet.AddNormalItem(item);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends a list of purchasable items to the character's client,
		/// making it open a window where the player can do so.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="items"></param>
		public static void ZC_PC_PURCHASE_ITEMLIST(PlayerCharacter character, IEnumerable<ShopItem> items)
		{
			using var packet = Packet.Rent(Op.ZC_PC_PURCHASE_ITEMLIST);

			foreach (var item in items)
			{
				packet.PutInt(item.Price);

				if (Game.Version >= Versions.Beta1)
					packet.PutInt(Sabine.Zone.Skills.MerchantPricing.GetBuyPrice(character, item.Price));

				packet.PutByte(0);

				if (Game.Version >= Versions.Sabine)
				{
					// rA PACKET_ZC_PC_PURCHASE_ITEMLIST_sub at PACKETVER_MAIN
					// >= 20181121 widens itemId to uint32. Per-entry body =
					// 4 price + 4 discountPrice + 1 itemType + 4 itemId = 13.
					// Ref: rathena packets_struct.hpp:3065.
					packet.PutInt(item.ClassId);
				}
				else if (Game.Version < Versions.Beta2)
				{
					packet.PutString(item.StringId, Sizes.ItemNames);
				}
				else
				{
					packet.PutShort((short)item.ClassId);
				}
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends a list of items the character can sell to the client,
		/// making it open a window where the player can do so.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="items"></param>
		public static void ZC_PC_SELL_ITEMLIST(PlayerCharacter character, IEnumerable<Item> items)
		{
			using var packet = Packet.Rent(Op.ZC_PC_SELL_ITEMLIST);

			foreach (var item in items)
			{
				var sellPrice = item.Data.SellPrice;

				// The alpha client halves the price sent here, so we have
				// to multiply our sell price to get it to display the
				// correct amount. We could also send the buy price,
				// but that would potentially cause unexpected behavior
				// if someone were to set a selling price for an item
				// that's not half of the buy price.
				if (Game.Version < Versions.Beta1)
					sellPrice *= 2;

				packet.PutShort((short)item.InventoryId);
				packet.PutInt(sellPrice);

				if (Game.Version >= Versions.Beta1)
					packet.PutInt(Sabine.Zone.Skills.MerchantPricing.GetSellPrice(character, sellPrice));
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the result of a purchase request to the client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="result"></param>
		public static void ZC_PC_PURCHASE_RESULT(PlayerCharacter character, PurchaseResult result)
		{
			using var packet = Packet.Rent(Op.ZC_PC_PURCHASE_RESULT);
			packet.PutByte((byte)result);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the result of a sell request to the client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="result"></param>
		public static void ZC_PC_SELL_RESULT(PlayerCharacter character, SellResult result)
		{
			using var packet = Packet.Rent(Op.ZC_PC_SELL_RESULT);
			packet.PutByte((byte)result);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the character's equip inventory tab using the given
		/// list of items, filtering it for equip items.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="items"></param>
		public static void ZC_EQUIPMENT_ITEMLIST(PlayerCharacter character, IEnumerable<Item> items)
		{
			var useV6 = Game.Version >= Versions.Sabine;
			var useV3 = !useV6 && Game.Version >= Versions.S2500;
			using var packet = Packet.Rent(Op.ZC_EQUIPMENT_ITEMLIST);

			foreach (var item in items)
			{
				if (!item.Type.IsEquip())
					continue;

				if (useV6) packet.AddEquipItemV6(item, item.GetSlotsFor(character));
				else if (useV3) packet.AddEquipItemV3(item, item.GetSlotsFor(character));
				else packet.AddEquipItem(item, item.GetSlotsFor(character));
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens storage window if it's not open yet and fills it with
		/// the given items, filtering for equip.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="items"></param>
		public static void ZC_STORE_EQUIPMENT_ITEMLIST(PlayerCharacter character, IEnumerable<Item> items)
		{
			var useV6 = Game.Version >= Versions.Sabine;
			var useV3 = !useV6 && Game.Version >= Versions.S2500;
			using var packet = Packet.Rent(Op.ZC_STORE_EQUIPMENT_ITEMLIST);

			foreach (var item in items)
			{
				if (!item.Type.IsEquip())
					continue;

				if (useV6) packet.AddEquipItemV6(item, item.GetSlotsFor(character));
				else if (useV3) packet.AddEquipItemV3(item, item.GetSlotsFor(character));
				else packet.AddEquipItem(item, item.GetSlotsFor(character));
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Makes item appear for player, lying on the ground.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="item"></param>
		public static void ZC_ITEM_ENTRY(PlayerCharacter character, Item item)
		{
			using var packet = Packet.Rent(Op.ZC_ITEM_ENTRY);

			if (Game.Version >= Versions.Sabine)
			{
				// rA PACKET_ZC_ITEM_ENTRY at PACKETVER_MAIN_NUM >=
				// 20181121: AID + uint32 itemId + identify + x + y +
				// amount + subX + subY. Wire = 19.
				// Ref: rathena packets_struct.hpp:2350.
				packet.PutInt(item.Handle);
				packet.PutInt(item.ClassId);
				packet.PutByte(item.IsIdentified);
				packet.PutShort((short)item.Position.X);
				packet.PutShort((short)item.Position.Y);
				packet.PutShort((short)item.Amount);
				packet.PutByte(8);
				packet.PutByte(8);
			}
			else
			{
				packet.AddEntryItem(item);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Makes item appear for players around it, dropping to the ground.
		/// </summary>
		/// <param name="item"></param>
		public static void ZC_ITEM_FALL_ENTRY(Item item)
		{
			using var packet = Packet.Rent(Op.ZC_ITEM_FALL_ENTRY);

			if (Game.Version >= Versions.Sabine)
			{
				// rA packet_dropflooritem at PACKETVER >= 20180418:
				// AID + uint32 ITID + type + IsIdentified + xPos + yPos +
				// subX + subY + count + showdropeffect + dropeffectmode.
				// Wire = 24.
				// Ref: rathena packets_struct.hpp:597.
				packet.PutInt(item.Handle);
				packet.PutInt(item.ClassId);
				packet.PutShort(0);                         // type (dropped-item appearance flags)
				packet.PutByte(item.IsIdentified);
				packet.PutShort((short)item.Position.X);
				packet.PutShort((short)item.Position.Y);
				packet.PutByte(8);
				packet.PutByte(8);
				packet.PutShort((short)item.Amount);
				packet.PutByte(0);                          // showdropeffect (off)
				packet.PutShort(0);                         // dropeffectmode
			}
			else
			{
				packet.AddEntryItem(item);
			}

			item.Map.Broadcast(packet, item, BroadcastTargets.All);
		}

		/// <summary>
		/// Makes item with given handle disappear for the character.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="handle"></param>
		public static void ZC_ITEM_DISAPPEAR(PlayerCharacter character, int handle)
		{
			using var packet = Packet.Rent(Op.ZC_ITEM_DISAPPEAR);
			packet.PutInt(handle);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Makes item disappear on clients around it.
		/// </summary>
		/// <param name="item"></param>
		public static void ZC_ITEM_DISAPPEAR(Item item)
		{
			using var packet = Packet.Rent(Op.ZC_ITEM_DISAPPEAR);
			packet.PutInt(item.Handle);

			var map = item.Map;
			if (map == null)
			{
				Log.Debug("[ItemLifecycle] ZC_ITEM_DISAPPEAR(Item) handle={0}: item.Map is NULL, NOT broadcasting", item.Handle);
				return;
			}

			Log.Debug("[ItemLifecycle] ZC_ITEM_DISAPPEAR(Item) handle={0} pos={1} map={2} broadcasting", item.Handle, item.Position, map.StringId);
			map.Broadcast(packet, item, BroadcastTargets.All);
		}

		/// <summary>
		/// Response to item drop request, which updates the item stack
		/// that was changed.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="invId">Inventory id of the item that changed or was dropped.</param>
		/// <param name="removeAmount">Amount to remove from the item stack. If it reaches 0, the item disappears.</param>
		public static void ZC_ITEM_THROW_ACK(PlayerCharacter character, int invId, int removeAmount)
		{
			using var packet = Packet.Rent(Op.ZC_ITEM_THROW_ACK);

			packet.PutShort((short)invId);
			packet.PutShort((short)removeAmount);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Response to item use request, updates the item's amount.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="invId"></param>
		/// <param name="newAmount"></param>
		public static void ZC_USE_ITEM_ACK(PlayerCharacter character, int invId, int newAmount, int classId = 0)
		{
			using var packet = Packet.Rent(Op.ZC_USE_ITEM_ACK);

			if (Game.Version >= Versions.Sabine)
			{
				// rA PACKET_ZC_USE_ITEM_ACK at PACKETVER_MAIN_NUM >=
				// 20181121 widens itemId to uint32 and adds AID.
				// Body = 2 index + 4 itemId + 4 AID + 2 amount + 1 result,
				// wire = 15.
				// Ref: rathena packets_struct.hpp:2577.
				packet.PutShort((short)invId);
				packet.PutInt(classId);
				packet.PutInt(character.Connection?.Account?.Id ?? character.Handle);
				packet.PutShort((short)newAmount);
				packet.PutByte(true);
			}
			else
			{
				packet.PutShort((short)invId);
				packet.PutShort((short)newAmount);
				packet.PutByte(true);
			}

			character.Connection.Send(packet);
		}

		public static class ZC_REQ_WEAR_EQUIP_ACK
		{
			/// <summary>
			/// Successful response to equip request, makes character
			/// equip the item in the given slot.
			/// </summary>
			/// <param name="character"></param>
			/// <param name="invId"></param>
			/// <param name="equipSlot"></param>
			public static void Success(PlayerCharacter character, int invId, EquipSlots equipSlot)
				=> Raw(character, invId, equipSlot, true);

			/// <summary>
			/// Negative response to equip request, displaying error
			/// message.
			/// </summary>
			/// <param name="character"></param>
			/// <param name="invId"></param>
			public static void Fail(PlayerCharacter character, int invId)
				=> Raw(character, invId, EquipSlots.None, false);

			/// <summary>
			/// Response to equip request.
			/// </summary>
			/// <param name="character"></param>
			/// <param name="invId"></param>
			/// <param name="equipSlot"></param>
			/// <param name="success"></param>
			public static void Raw(PlayerCharacter character, int invId, EquipSlots equipSlot, bool success)
			{
				using var packet = Packet.Rent(Op.ZC_REQ_WEAR_EQUIP_ACK);

				packet.PutShort((short)invId);

				if (Game.Version >= Versions.Sabine)
				{
					// rA PACKET_ZC_REQ_WEAR_EQUIP_ACK 0x0999 at
					// PACKETVER_MAIN_NUM >= 20121205 widens wearLocation
					// to uint32 and adds wItemSpriteNumber (uint16).
					// Body = 2 index + 4 wearLocation + 2 spriteNum + 1
					// result, wire = 11.
					// Ref: rathena packets_struct.hpp:1269.
					packet.PutInt((int)equipSlot);
					packet.PutShort(0);   // wItemSpriteNumber (sprite override)
				}
				else if (Game.Version < Versions.Beta2)
				{
					packet.PutByte((byte)equipSlot);
				}
				else
				{
					packet.PutShort((short)equipSlot);
				}

				packet.PutByte(success);

				character.Connection.Send(packet);
			}
		}

		/// <summary>
		/// Response to unequip request, moves the item out of the given
		/// slot and into the inventory.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="invId"></param>
		/// <param name="equipSlot"></param>
		public static void ZC_REQ_TAKEOFF_EQUIP_ACK(PlayerCharacter character, int invId, EquipSlots equipSlot)
		{
			using var packet = Packet.Rent(Op.ZC_REQ_TAKEOFF_EQUIP_ACK);

			packet.PutShort((short)invId);

			if (Game.Version >= Versions.Sabine)
			{
				// rA PACKET_ZC_REQ_TAKEOFF_EQUIP_ACK 0x099a at PACKETVER
				// >= 20130000 widens wearLocation to uint32. Body = 2
				// index + 4 wearLocation + 1 flag, wire = 9.
				// Ref: rathena packets.hpp:1007.
				packet.PutInt((int)equipSlot);
			}
			else if (Game.Version < Versions.Beta2)
			{
				packet.PutByte((byte)equipSlot);
			}
			else
			{
				packet.PutShort((short)equipSlot);
			}

			packet.PutByte(true);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Makes the character's client display the given item as
		/// equipped ammo.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="item"></param>
		public static void ZC_EQUIP_ARROW(PlayerCharacter character, Item item)
		{
			using var packet = Packet.Rent(Op.ZC_EQUIP_ARROW);
			packet.PutShort((short)(item?.InventoryId ?? 0));

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Response to storage closing notification. Closes the storage
		/// window on the character's client.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_CLOSE_STORE(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_CLOSE_STORE);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Shows menu to select whether to buy or sell items.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="npcHandle"></param>
		public static void ZC_SELECT_DEALTYPE(PlayerCharacter character, int npcHandle)
		{
			using var packet = Packet.Rent(Op.ZC_SELECT_DEALTYPE);
			packet.PutInt(npcHandle);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Displays MVP animation above the character's head.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_MVP(Character character)
		{
			using var packet = Packet.Rent(Op.ZC_MVP);
			packet.PutInt(character.Handle);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Displays message to the player, telling them that they were
		/// the MVP and that they'll receive an EXP bonus.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_MVP_GETTING_SPECIAL_EXP(PlayerCharacter character, int expAmount)
		{
			using var packet = Packet.Rent(Op.ZC_MVP_GETTING_SPECIAL_EXP);
			packet.PutInt(expAmount);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Displays a message to the MVP killer announcing the special
		/// item drop they received from the MVP.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="itemId"></param>
		public static void ZC_MVP_GETTING_ITEM(PlayerCharacter character, int itemId)
		{
			using var packet = Packet.Rent(Op.ZC_MVP_GETTING_ITEM);

			// Alpha registers this packet at size 18 (header 2 + payload 16),
			// while Beta2 shrinks it to 4 (header 2 + payload 2). The payload
			// shape isn't fully documented; we send the item id as the first
			// short and pad the rest with zeroes for older clients.
			if (Game.Version >= Versions.Beta2)
			{
				packet.PutShort((short)itemId);
			}
			else
			{
				packet.PutShort((short)itemId);
				packet.PutEmpty(14);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends response to restart request to character's client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="type"></param>
		public static void ZC_RESTART_ACK(PlayerCharacter character, RestartType type)
		{
			using var packet = Packet.Rent(Op.ZC_RESTART_ACK);
			packet.PutByte((byte)type);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends response to disconnect request to character's client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="s1"></param>
		public static void ZC_ACK_REQ_DISCONNECT(PlayerCharacter character, int s1)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REQ_DISCONNECT);
			packet.PutShort((short)s1);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends a list of skills the character has to the client,
		/// refreshing the skill list.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skills"></param>
		public static void ZC_SKILLINFO_LIST(PlayerCharacter character, IEnumerable<Skill> skills)
		{
			using var packet = Packet.Rent(Op.ZC_SKILLINFO_LIST);

			foreach (var skill in skills)
				packet.AddSkill(character, skill);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Adds the given skill to the character's skill list.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skill"></param>
		public static void ZC_ADD_SKILL(PlayerCharacter character, Skill skill)
		{
			using var packet = Packet.Rent(Op.ZC_ADD_SKILL);
			packet.AddSkill(skill);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the skill on the character's client.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skill"></param>
		public static void ZC_SKILLINFO_UPDATE(PlayerCharacter character, Skill skill)
			=> ZC_SKILLINFO_UPDATE(character, skill.Id, skill.Level, skill.CanBeLeveled && character.Parameters.SkillPoints > 0);

		/// <summary>
		/// Updates a skill's information, such as its level or whether it
		/// can be upgraded.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skillId"></param>
		/// <param name="level"></param>
		/// <param name="canUpgrade"></param>
		public static void ZC_SKILLINFO_UPDATE(PlayerCharacter character, SkillId skillId, int level, bool canUpgrade)
		{
			if (!ZoneServer.Instance.Data.Skills.TryFind(skillId, out var skillData))
				return;

			using var packet = Packet.Rent(Op.ZC_ADD_SKILL);

			// ZC_ADD_SKILL is a fixed-size packet (33 in Alpha, 39 in Beta1+),
			// so the payload here must match AddSkillData's layout exactly.
			packet.PutShort((short)skillId);

			// Keep packing in sync with PacketSkillHelpers.AddSkillData: low 16
			// bits = inf, high 16 bits = inf2 so the classic client can identify
			// quest/wedding/etc. skills.
			var inf = (int)skillData.TypeFlags;
			var inf2 = (int)((uint)skillData.PropertyFlags & 0xFFFF);
			packet.PutInt(inf | (inf2 << 16));

			packet.PutShort((short)level);
			packet.PutShort((short)skillData.GetSpCost(level));
			packet.PutShort((short)skillData.GetRange(level));

			var nameLength = (Game.Version < Versions.Beta1) ? 16 : 24;
			packet.PutString(skillData.StringId, nameLength);

			packet.PutByte(canUpgrade);

			if (Game.Version < Versions.Beta1)
				packet.PutEmpty(2);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Informs the client about success or failure of a skill usage
		/// request.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skillId"></param>
		/// <param name="result"></param>
		/// <param name="failType"></param>
		/// <param name="failReason"></param>
		public static void ZC_ACK_TOUSESKILL(Character character, SkillId skillId, SkillUseResult result, SkillFailType failType, SkillFailReason failReason)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_TOUSESKILL);

			if (Game.Version <= Versions.Alpha)
			{
				// The alpha client checks for success and displays a
				// message on fail.

				packet.PutShort((short)skillId);
				packet.PutByte((byte)result);
				packet.PutByte((byte)failReason);
			}
			else
			{
				// Beta2+ first checks for success. On fail it checks the
				// fail type and either displays skill specific fail
				// reasons, or generic ones.

				packet.PutShort((short)skillId);
				packet.PutInt((int)failReason);
				packet.PutByte((byte)result);
				packet.PutByte((byte)failType);
			}

			character.Map.Broadcast(packet, character, BroadcastTargets.OnlySource);
		}

		/// <summary>
		/// Sends notification about a trade request to character.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="traderName"></param>
		public static void ZC_REQ_EXCHANGE_ITEM(PlayerCharacter character, string traderName)
		{
			using var packet = Packet.Rent(Op.ZC_REQ_EXCHANGE_ITEM);
			packet.PutString(traderName, Sizes.CharacterNames);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends response about a trade request to character, opening
		/// trade window on accept.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="response"></param>
		public static void ZC_ACK_EXCHANGE_ITEM(PlayerCharacter character, TradingResponse response)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_EXCHANGE_ITEM);
			packet.PutByte((byte)response);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Cancels active trade on character's client.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_CANCEL_EXCHANGE_ITEM(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_CANCEL_EXCHANGE_ITEM);

			character.Connection.Send(packet);
		}

		public static class ZC_ADD_EXCHANGE_ITEM
		{
			/// <summary>
			/// Adds item to trade window, on the side of the trading partner.
			/// </summary>
			/// <param name="character"></param>
			/// <param name="item"></param>
			/// <param name="amount"></param>
			public static void Item(PlayerCharacter character, Item item, int amount)
			{
				using var packet = Packet.Rent(Op.ZC_ADD_EXCHANGE_ITEM);

				if (Game.Version >= Versions.Sabine)
				{
					packet.AddExchangeItemV6(item, amount, item.GetSlotsFor(character));
					character.Connection.Send(packet);
					return;
				}

				packet.PutInt(amount);
				packet.PutString(item.StringId, Sizes.ItemNames);

				character.Connection.Send(packet);
			}

			/// <summary>
			/// Adds zeny to trade window, on the side of the trading
			/// partner.
			/// </summary>
			/// <param name="character"></param>
			/// <param name="amount"></param>
			public static void Zeny(PlayerCharacter character, int amount)
			{
				using var packet = Packet.Rent(Op.ZC_ADD_EXCHANGE_ITEM);

				if (Game.Version >= Versions.Sabine)
				{
					// V6 fixed-size body must match the Item variant; emit
					// a zero-id placeholder with zeny in the amount slot.
					// The classic eA convention used a "money" string id at
					// the same op — open-midgard recognises the 0 classId
					// as the zeny pseudo-entry.
					packet.PutInt(0);                  // itemId (zeny pseudo)
					packet.PutByte(0);                 // itemType
					packet.PutInt(amount);             // amount = zeny
					packet.PutByte(1);                 // identified
					packet.PutByte(0);                 // damaged
					packet.PutByte(0);                 // refine
					packet.PutEmpty(8);                // slot[4]
					packet.PutEmpty(25);               // option_data[5]
					packet.PutInt(0);                  // location
					packet.PutShort(0);                // look
					character.Connection.Send(packet);
					return;
				}

				packet.PutInt(amount);
				packet.PutString("money", Sizes.ItemNames);

				character.Connection.Send(packet);
			}
		}

		/// <summary>
		/// Sends response to request to add item to trade, adding the
		/// item to the own side on success.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="invId"></param>
		/// <param name="result"></param>
		public static void ZC_ACK_ADD_EXCHANGE_ITEM(PlayerCharacter character, int invId, TradingSuccess result)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_ADD_EXCHANGE_ITEM);

			packet.PutShort((short)invId);
			packet.PutByte((byte)result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the character about the given side requesting to
		/// lock in the trade.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="side"></param>
		public static void ZC_CONCLUDE_EXCHANGE_ITEM(PlayerCharacter character, TradingSide side)
		{
			using var packet = Packet.Rent(Op.ZC_CONCLUDE_EXCHANGE_ITEM);
			packet.PutByte((byte)side);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the character about the result of the trade finish
		/// request.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="result"></param>
		public static void ZC_EXEC_EXCHANGE_ITEM(PlayerCharacter character, TradingSuccess result)
		{
			using var packet = Packet.Rent(Op.ZC_EXEC_EXCHANGE_ITEM);
			packet.PutByte((byte)result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the character about the result of the chat room
		/// creation request.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="result"></param>
		public static void ZC_ACK_CREATE_CHATROOM(PlayerCharacter character, ChatRoomSuccess result)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_CREATE_CHATROOM);
			packet.PutByte((byte)result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Creates a new chat room on all clients close to the chat,
		/// displaying it above the owner's head.
		/// </summary>
		/// <param name="room"></param>
		public static void ZC_ROOM_NEWENTRY(ChatRoom room)
		{
			using var packet = Packet.Rent(Op.ZC_ROOM_NEWENTRY);

			packet.PutInt(room.OwnerHandle);
			packet.PutInt(room.Id);
			packet.PutShort((short)room.Limit);
			packet.PutShort((short)room.MemberCount);
			packet.PutByte((byte)room.Privacy);
			packet.PutString(room.Title, false);

			room.Map.Broadcast(packet);
		}

		/// <summary>
		/// Updates the chat room on the character's client in respons to
		/// a change request.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="room"></param>
		public static void ZC_CHANGE_CHATROOM(PlayerCharacter character, ChatRoom room)
		{
			using var packet = Packet.Rent(Op.ZC_CHANGE_CHATROOM);

			packet.PutInt(room.OwnerHandle);
			packet.PutInt(room.Id);
			packet.PutShort((short)room.Limit);
			packet.PutShort((short)room.MemberCount);
			packet.PutByte((byte)room.Privacy);
			packet.PutString(room.Title, false);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Removes chat room from all clients close to the chat, no
		/// longer displaying it above the owner's head.
		/// </summary>
		/// <param name="room"></param>
		public static void ZC_DESTROY_ROOM(ChatRoom room)
		{
			using var packet = Packet.Rent(Op.ZC_DESTROY_ROOM);
			packet.PutInt(room.Id);

			room.Map.Broadcast(packet);
		}

		/// <summary>
		/// Notifies the character about the reason they weren't able to
		/// join a chat room.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="reason"></param>
		public static void ZC_REFUSE_ENTER_ROOM(PlayerCharacter character, ChatRoomRefuseReason reason)
		{
			using var packet = Packet.Rent(Op.ZC_REFUSE_ENTER_ROOM);
			packet.PutByte((byte)reason);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends information about the chat they're entering to character.
		/// </summary>
		/// <param name="room"></param>
		/// <param name="newMember"></param>
		public static void ZC_ENTER_ROOM(ChatRoom room, PlayerCharacter newMember)
		{
			using var packet = Packet.Rent(Op.ZC_ENTER_ROOM);

			packet.PutInt(room.Id);

			using var members = room.GetMembers();
			foreach (var member in members)
			{
				packet.PutInt((int)member.Role);
				packet.PutString(member.Name, Sizes.CharacterNames);
			}

			newMember.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the chat members about a new member joining.
		/// </summary>
		/// <param name="room"></param>
		/// <param name="newMember"></param>
		public static void ZC_MEMBER_NEWENTRY(ChatRoom room, PlayerCharacter newMember)
		{
			using var packet = Packet.Rent(Op.ZC_MEMBER_NEWENTRY);

			packet.PutShort((short)room.MemberCount);
			packet.PutString(newMember.Name, Sizes.CharacterNames);

			using var members = room.GetMembers();
			foreach (var member in members)
			{
				if (room.Map.TryGetPlayer(member.Handle, out var memberCharacter))
					memberCharacter.Connection.Send(packet);
			}
		}

		/// <summary>
		/// Notifies the chat members about a member leaving.
		/// </summary>
		/// <param name="room"></param>
		/// <param name="formerMember"></param>
		/// <param name="reason"></param>
		public static void ZC_MEMBER_EXIT(ChatRoom room, PlayerCharacter formerMember, MemberExitReason reason)
		{
			using var packet = Packet.Rent(Op.ZC_MEMBER_EXIT);

			packet.PutShort((short)room.MemberCount);
			packet.PutString(formerMember.Name, Sizes.CharacterNames);
			packet.PutByte((byte)reason);

			formerMember.Connection.Send(packet);

			using var members = room.GetMembers();
			foreach (var member in members)
			{
				if (room.Map.TryGetPlayer(member.Handle, out var memberCharacter))
					memberCharacter.Connection.Send(packet);
			}
		}

		/// <summary>
		/// Updates the given members role in the chat, moving the owner
		/// to the top.
		/// </summary>
		/// <param name="room"></param>
		/// <param name="memberName"></param>
		/// <param name="role"></param>
		public static void ZC_ROLE_CHANGE(ChatRoom room, string memberName, ChatRoomRole role)
		{
			using var packet = Packet.Rent(Op.ZC_ROLE_CHANGE);

			packet.PutInt((int)role);
			packet.PutString(memberName, Sizes.CharacterNames);

			using var members = room.GetMembers();
			foreach (var member in members)
			{
				if (room.Map.TryGetPlayer(member.Handle, out var memberCharacter))
					memberCharacter.Connection.Send(packet);
			}
		}

		/// <summary>
		/// Sends result of the party creation request to the character,
		/// opening the party window on success and/or displaying a
		/// message about the result.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="result"></param>
		public static void ZC_ACK_MAKE_GROUP(PlayerCharacter character, PartyCreationResult result)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_MAKE_GROUP);
			packet.PutByte((byte)result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Displays effect on character, for them and the characters
		/// nearby.
		/// </summary>
		/// <remarks>
		/// First seen in i20030430, where the client handles 5 effects,
		/// from 0 to 4. Based on their usage in eAthena, those might be:
		/// 
		/// - 0: BaseLevelUp
		/// - 1: JobLevelUp
		/// - 2: RefineFail
		/// - 3: RefineUp
		/// - 4: ?
		/// 
		/// Unfortunately no clients that were tested did anything when
		/// these were sent on their own.
		/// </remarks>
		/// <param name="character"></param>
		/// <param name="effectId"></param>
		public static void ZC_NOTIFY_EFFECT(PlayerCharacter character, int effectId)
			=> ZC_NOTIFY_EFFECT((Character)character, effectId);

		/// <summary>
		/// Shows or hides an illustration image (cutin) on the player's
		/// client.
		/// </summary>
		/// <remarks>
		/// Single op, era-driven width: Alpha/Beta1/Beta2 use 0x0145
		/// with a 16-byte image-name field; EP4+ remaps the op to 0x01B3
		/// with a 64-byte field (rA <c>clif_cutin</c>). Pass
		/// <c>type = 255</c> to clear an active cutin.
		/// Ref: rathena packets_struct.hpp:5340, map/clif.cpp:2741.
		/// </remarks>
		public static void ZC_SHOW_IMAGE(PlayerCharacter character, string image, byte type)
		{
			using var packet = Packet.Rent(Op.ZC_SHOW_IMAGE);

			packet.PutString(image, Game.Version >= Versions.S350 ? 64 : 16);
			packet.PutByte(type);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Plays a sound effect at the source actor's position. Broadcast
		/// to nearby players.
		/// </summary>
		/// <remarks>
		/// rA <c>ZC_SOUND</c> 0x01d3 (PV>=4) — 24-byte WAV name + act +
		/// term + AID. act = 0 plays once, 1 stops. term is the
		/// fade/loop delay in ms (0 = default).
		/// Ref: rathena packets_struct.hpp:5584, map/clif.cpp clif_soundeffect.
		/// </remarks>
		public static void ZC_SOUND(Character source, string wav, byte act, int term)
		{
			using var packet = Packet.Rent(Op.ZC_SOUND);
			packet.PutString(wav, 24);
			packet.PutByte(act);
			packet.PutInt(term);
			packet.PutInt(source.Handle);

			source.Map.Broadcast(packet, source, BroadcastTargets.All);
		}

		/// <summary>
		/// Adds or removes a marker on the player's minimap (compass /
		/// viewpoint).
		/// </summary>
		/// <remarks>
		/// rA <c>ZC_COMPASS</c> 0x0144. type = 0 add to map permanently,
		/// 1 add as floating marker (with text), 2 remove all of type
		/// flag from npcId. Mirrors rAthena's <c>viewpoint</c> /
		/// <c>clif_viewpoint</c>.
		/// Ref: rathena packets.hpp:807.
		/// </remarks>
		public static void ZC_COMPASS(PlayerCharacter character, int npcId, int type, int x, int y, byte id, int color)
		{
			using var packet = Packet.Rent(Op.ZC_COMPASS);
			packet.PutInt(npcId);
			packet.PutInt(type);
			packet.PutInt(x);
			packet.PutInt(y);
			packet.PutByte(id);
			packet.PutInt(color);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the guild + emblem displayed above an actor (used for
		/// guild master sprites, castle flag NPCs, etc.).
		/// </summary>
		/// <remarks>
		/// rA <c>ZC_CHANGE_GUILD</c> 0x01b4 (pre-2019 layout: AID +
		/// guild_id + emblem_id). Broadcast to nearby players so the
		/// emblem renders on the actor sprite.
		/// Ref: rathena packets_struct.hpp:5824.
		/// </remarks>
		public static void ZC_CHANGE_GUILD(Character actor, int guildId, int emblemId)
		{
			using var packet = Packet.Rent(Op.ZC_CHANGE_GUILD);
			packet.PutInt(actor.Handle);
			packet.PutInt(guildId);
			packet.PutShort((short)emblemId);

			actor.Map.Broadcast(packet, actor, BroadcastTargets.All);
		}

		/// <summary>
		/// Sets the quest indicator (exclamation / question mark) above
		/// an NPC's head for nearby players. Broadcast so that all
		/// clients with the NPC in sight see the marker.
		/// </summary>
		/// <remarks>
		/// rA <c>ZC_QUEST_NOTIFY_EFFECT</c> 0x0446 at PACKETVER >=
		/// 20090218 — 14-byte body: npcId(4) + x(2) + y(2) + effect(2)
		/// + color(2). Send <c>effect = None</c> to clear.
		/// Ref: rathena map/clif.cpp:18171 clif_quest_show_event.
		/// </remarks>
		public static void ZC_QUEST_NOTIFY_EFFECT(Npc npc, QuestIconEffect effect, QuestIconColor color)
		{
			using var packet = Packet.Rent(Op.ZC_QUEST_NOTIFY_EFFECT);
			packet.PutInt(npc.Handle);
			packet.PutShort((short)npc.Position.X);
			packet.PutShort((short)npc.Position.Y);
			packet.PutShort((short)effect);
			packet.PutShort((short)color);

			if (npc.Map != null)
				npc.Map.Broadcast(packet, npc, BroadcastTargets.All);
		}

		/// <summary>
		/// Shows a cast-style progress bar to the player. Mirrors
		/// rAthena's <c>clif_progressbar</c> (<c>ZC_PROGRESS</c>
		/// 0x02f0, 10 bytes). The bar fills over <paramref name="seconds"/>
		/// seconds; the client emits <c>CZ_PROGRESS</c> when it
		/// completes.
		/// </summary>
		/// <param name="character">Recipient.</param>
		/// <param name="color">RGB color (e.g. <c>0xFFFFFF</c> for white).</param>
		/// <param name="seconds">Duration in seconds.</param>
		public static void ZC_PROGRESS(PlayerCharacter character, int color, int seconds)
		{
			using var packet = Packet.Rent(Op.ZC_PROGRESS);
			packet.PutInt(color);
			packet.PutInt(seconds);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Cancels any progress bar currently displayed on the player's
		/// client. Mirrors rAthena's <c>clif_progressbar_abort</c>
		/// (<c>ZC_PROGRESS_CANCEL</c> 0x02f2).
		/// </summary>
		public static void ZC_PROGRESS_CANCEL(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_PROGRESS_CANCEL);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Displays large digital-clock-style digits on the player's
		/// screen (countdown timer / score overlay). Mirrors rAthena's
		/// <c>clif_showdigit</c> (<c>ZC_SHOWDIGIT</c> 0x01b1).
		/// </summary>
		/// <param name="character">Recipient.</param>
		/// <param name="type">Display behavior (one-shot / counters).</param>
		/// <param name="value">Number to display; semantics depend on <paramref name="type"/>.</param>
		public static void ZC_SHOWDIGIT(PlayerCharacter character, ShowDigitType type, int value)
		{
			using var packet = Packet.Rent(Op.ZC_SHOWDIGIT);
			packet.PutByte((byte)type);
			packet.PutInt(value);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sets the map property (PvP zone, GVG, etc.) for the player.
		/// Mirrors rAthena's <c>clif_map_property</c>
		/// (<c>ZC_NOTIFY_MAPPROPERTY</c> 0x0199).
		/// </summary>
		public static void ZC_NOTIFY_MAPPROPERTY(PlayerCharacter character, MapProperty property)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_MAPPROPERTY);
			packet.PutShort((short)property);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens a numeric input dialog tied to the given NPC. The client
		/// replies with <c>CZ_INPUT_EDITDLG</c> carrying the entered
		/// integer. Mirrors rAthena's <c>clif_scriptinput</c>
		/// (<c>ZC_OPEN_EDITDLG</c> 0x0142).
		/// </summary>
		public static void ZC_OPEN_EDITDLG(PlayerCharacter character, int npcHandle)
		{
			using var packet = Packet.Rent(Op.ZC_OPEN_EDITDLG);
			packet.PutInt(npcHandle);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens a string input dialog tied to the given NPC. The client
		/// replies with <c>CZ_INPUT_EDITDLGSTR</c> carrying the entered
		/// text. Mirrors rAthena's <c>clif_scriptinputstr</c>
		/// (<c>ZC_OPEN_EDITDLGSTR</c> 0x01d4).
		/// </summary>
		public static void ZC_OPEN_EDITDLGSTR(PlayerCharacter character, int npcHandle)
		{
			using var packet = Packet.Rent(Op.ZC_OPEN_EDITDLGSTR);
			packet.PutInt(npcHandle);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Displays a localised system message identified by id (e.g.
		/// "Equipment cannot be used in this place"). The client
		/// resolves the id against its bundled msgstringtable.
		/// Mirrors rAthena's <c>clif_msg</c> (<c>ZC_MSG</c> 0x0291).
		/// </summary>
		public static void ZC_MSG(PlayerCharacter character, int messageId)
		{
			using var packet = Packet.Rent(Op.ZC_MSG);
			packet.PutShort((short)messageId);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Tells the client the result of a refining attempt and the
		/// item's new refine level. Mirrors rAthena's <c>clif_refine</c>
		/// (<c>ZC_ACK_ITEMREFINING</c> 0x0188). Result codes:
		/// 0 = success, 1 = failure (unchanged), 2 = downgrade,
		/// 3 = failure (alt).
		/// </summary>
		public static void ZC_ACK_ITEMREFINING(PlayerCharacter character, int result, int invId, int newRefineLevel)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_ITEMREFINING);
			packet.PutShort((short)result);
			packet.PutShort((short)invId);
			packet.PutShort((short)newRefineLevel);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts a chat bubble above a monster or NPC in a custom
		/// color. Mirrors rAthena's <c>clif_messagecolor_target</c>
		/// (<c>ZC_NPC_CHAT</c> 0x02C1).
		/// </summary>
		/// <param name="source">Source actor — the message floats above it.</param>
		/// <param name="message">Chat text to display.</param>
		/// <param name="color">RGB color (0xRRGGBB).</param>
		public static void ZC_NPC_CHAT(Character source, string message, int color)
		{
			using var packet = Packet.Rent(Op.ZC_NPC_CHAT);
			packet.PutInt(source.Handle);
			packet.PutInt(color);
			packet.PutString(message);

			if (source.Map != null)
				source.Map.Broadcast(packet, source, BroadcastTargets.All);
		}

		/// <summary>
		/// Pushes the guild MOTD (subject + body) to a member. Sent on
		/// login and after the guild master changes the notice.
		/// Mirrors rAthena's <c>clif_guild_notice</c>
		/// (<c>ZC_GUILD_NOTICE</c> 0x016F, 182 bytes).
		/// </summary>
		public static void ZC_GUILD_NOTICE(PlayerCharacter character, string subject, string notice)
		{
			using var packet = Packet.Rent(Op.ZC_GUILD_NOTICE);
			packet.PutString(subject, 60);
			packet.PutString(notice, 120);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Shows the talkbox dialog above an actor (used by Bard /
		/// Dancer song chats and quest event flavor). Mirrors rA
		/// <c>clif_talkbox</c> (<c>ZC_TALKBOX_CHATCONTENTS</c> 0x0191,
		/// 86 bytes).
		/// </summary>
		public static void ZC_TALKBOX_CHATCONTENTS(Character source, string message)
		{
			using var packet = Packet.Rent(Op.ZC_TALKBOX_CHATCONTENTS);
			packet.PutInt(source.Handle);
			packet.PutString(message, 80);

			if (source.Map != null)
				source.Map.Broadcast(packet, source, BroadcastTargets.All);
		}

		/// <summary>
		/// Notifies players around an actor that one of its equipped
		/// items broke. Drives the "Equipment broken!" toast on the
		/// affected player's client and a visual flash for onlookers.
		/// Mirrors rA <c>clif_equipitemdamaged</c>
		/// (<c>ZC_EQUIPITEM_DAMAGED</c> 0x02BB, 8 bytes).
		/// </summary>
		public static void ZC_EQUIPITEM_DAMAGED(Character source, EquipSlots equipLocation)
		{
			using var packet = Packet.Rent(Op.ZC_EQUIPITEM_DAMAGED);
			packet.PutShort((short)equipLocation);
			packet.PutInt(source.Handle);

			if (source.Map != null)
				source.Map.Broadcast(packet, source, BroadcastTargets.All);
		}

		/// <summary>
		/// Pops the "This item is bound on equip — really equip?"
		/// confirmation on the player's client. Mirrors rA
		/// <c>clif_msg_bind</c> (<c>ZC_NOTIFY_BIND_ON_EQUIP</c> 0x02D3,
		/// 4 bytes).
		/// </summary>
		public static void ZC_NOTIFY_BIND_ON_EQUIP(PlayerCharacter character, int invId)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_BIND_ON_EQUIP);
			packet.PutShort((short)invId);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates one stat (HP, MaxHP, level, etc.) for a party member
		/// on the party HUD. Mirrors rA <c>clif_parchanged_user</c>
		/// (<c>ZC_PAR_CHANGE_USER</c> 0x01AB, 12 bytes).
		/// </summary>
		/// <param name="recipient">Member who will see the update.</param>
		/// <param name="targetGid">Handle of the member whose stat changed.</param>
		/// <param name="type">Stat type (matches ParameterType wire values).</param>
		/// <param name="value">New value.</param>
		public static void ZC_PAR_CHANGE_USER(PlayerCharacter recipient, int targetGid, short type, int value)
		{
			using var packet = Packet.Rent(Op.ZC_PAR_CHANGE_USER);
			packet.PutInt(targetGid);
			packet.PutShort(type);
			packet.PutInt(value);

			recipient.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts a party-loot toast to all members when one of them
		/// picks up an item. Mirrors rA <c>clif_party_show_picker</c>
		/// (<c>ZC_ITEM_PICKUP_PARTY</c> 0x02B8). At Sabine PV 50000 the
		/// itemId is widened to uint32 (rA PV>=20181121, 24-byte wire).
		/// </summary>
		public static void ZC_ITEM_PICKUP_PARTY(PlayerCharacter recipient, int pickerHandle, Item item, EquipSlots wearLocation)
		{
			using var packet = Packet.Rent(Op.ZC_ITEM_PICKUP_PARTY);
			packet.PutInt(pickerHandle);

			if (Game.Version >= Versions.Sabine)
				packet.PutInt(item.ClassId);
			else
				packet.PutShort((short)item.ClassId);

			packet.PutByte(item.IsIdentified);
			packet.PutByte(item.IsDamaged);
			packet.PutByte(item.RefineLevel);
			packet.AddSlotInfoForPickupAck(item);
			packet.PutShort((short)wearLocation);
			packet.PutByte((byte)item.Type);

			recipient.Connection.Send(packet);
		}

		/// <summary>
		/// Plays the resurrection visual effect over the target actor.
		/// Mirrors rAthena's <c>clif_resurrection</c>
		/// (<c>ZC_RESURRECTION</c> 0x0148, 8 bytes). type=0 is the
		/// classic Priest resurrect; other values are unused in rA.
		/// </summary>
		public static void ZC_RESURRECTION(Character target, int type = 0)
		{
			using var packet = Packet.Rent(Op.ZC_RESURRECTION);
			packet.PutInt(target.Handle);
			packet.PutShort((short)type);

			if (target.Map != null)
				target.Map.Broadcast(packet, target, BroadcastTargets.All);
		}

		/// <summary>
		/// Plays the dispell visual effect over the target. Mirrors
		/// rAthena's <c>clif_dispel</c> (<c>ZC_DISPEL</c> 0x01B9,
		/// 6 bytes). Used by Sage SA_DISPELL and similar buff-strip
		/// effects.
		/// </summary>
		public static void ZC_DISPEL(Character target)
		{
			using var packet = Packet.Rent(Op.ZC_DISPEL);
			packet.PutInt(target.Handle);

			if (target.Map != null)
				target.Map.Broadcast(packet, target, BroadcastTargets.All);
		}

		/// <summary>
		/// Toggles a passive / auto-cast skill display on the player's
		/// hotbar (typically triggered by item cards). Mirrors rA
		/// <c>clif_skill_autospell</c> family
		/// (<c>ZC_AUTORUN_SKILL</c> 0x0147, 39 bytes).
		/// </summary>
		public static void ZC_AUTORUN_SKILL(PlayerCharacter character, SkillId skillId, int skillType, int level, int sp, int range, string name, bool upgradable)
		{
			using var packet = Packet.Rent(Op.ZC_AUTORUN_SKILL);
			packet.PutShort((short)skillId);
			packet.PutInt(skillType);
			packet.PutShort((short)level);
			packet.PutShort((short)sp);
			packet.PutShort((short)range);
			packet.PutString(name, 24);
			packet.PutByte(upgradable);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens the Arrow Crafting UI with the supplied source-item
		/// list. Each entry is the class id of an item in the player's
		/// inventory that yields arrows. Mirrors rA
		/// <c>clif_arrow_create_list</c>
		/// (<c>ZC_MAKINGARROW_LIST</c> 0x01AD, dynamic). Per-entry size
		/// is uint32 at Sabine PV 50000 (rA PV>=20181121), uint16 below.
		/// </summary>
		public static void ZC_MAKINGARROW_LIST(PlayerCharacter character, IEnumerable<int> sourceClassIds)
		{
			using var packet = Packet.Rent(Op.ZC_MAKINGARROW_LIST);

			var wideId = Game.Version >= Versions.Sabine;
			foreach (var classId in sourceClassIds)
			{
				if (wideId)
					packet.PutInt(classId);
				else
					packet.PutShort((short)classId);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends a system message from the client's <c>msgstringtable</c>
		/// with a runtime parameter substituted into the template (e.g.
		/// "%s has joined the party"). Mirrors rA
		/// <c>clif_msgtable_str</c>
		/// (<c>ZC_FORMATSTRING_MSG</c> 0x02C2, dynamic).
		/// </summary>
		public static void ZC_FORMATSTRING_MSG(PlayerCharacter character, int messageId, string parameter)
		{
			using var packet = Packet.Rent(Op.ZC_FORMATSTRING_MSG);
			packet.PutShort((short)messageId);
			packet.PutString(parameter);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sets the map type (drives BGM and zone iconography on the
		/// client). Mirrors rA <c>clif_map_type</c>
		/// (<c>ZC_NOTIFY_MAPINFO</c> 0x0189, 4 bytes). Sent on map enter
		/// for special zones (PK servers, Battleground, jails).
		/// </summary>
		public static void ZC_NOTIFY_MAPINFO(PlayerCharacter character, MapInfoType type)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_MAPINFO);
			packet.PutShort((short)type);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends a GM broadcast with custom font color, type, size, and
		/// alignment. Mirrors rA <c>clif_broadcast2</c>
		/// (<c>ZC_BROADCAST2</c> 0x01C3, dynamic).
		/// </summary>
		/// <param name="character">Recipient.</param>
		/// <param name="message">Text to display.</param>
		/// <param name="fontColor">RGB color (0xRRGGBB).</param>
		/// <param name="fontType">Font face id (0 = default).</param>
		/// <param name="fontSize">Pixel size (typically 12).</param>
		/// <param name="fontAlign">0 = left, 1 = center, 2 = right.</param>
		/// <param name="fontY">Vertical offset (0 = default).</param>
		public static void ZC_BROADCAST2(PlayerCharacter character, string message, int fontColor, short fontType = 0, short fontSize = 12, short fontAlign = 0, short fontY = 0)
		{
			using var packet = Packet.Rent(Op.ZC_BROADCAST2);
			packet.PutInt(fontColor);
			packet.PutShort(fontType);
			packet.PutShort(fontSize);
			packet.PutShort(fontAlign);
			packet.PutShort(fontY);
			packet.PutString(message);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens the monster encyclopedia info window with the given
		/// monster's stats. Mirrors rA <c>clif_monster_info</c>
		/// (<c>ZC_MONSTER_INFO</c> 0x018C, 29 bytes).
		/// </summary>
		public static void ZC_MONSTER_INFO(PlayerCharacter character, Monster mob)
		{
			using var packet = Packet.Rent(Op.ZC_MONSTER_INFO);
			packet.PutShort((short)mob.IdentityId);                      // class
			packet.PutShort((short)mob.Parameters.BaseLevel);            // level
			packet.PutShort(0);                                          // size
			packet.PutInt(mob.Parameters.HpMax);                         // hp
			packet.PutShort(0);                                          // def
			packet.PutShort(0);                                          // race
			packet.PutShort(0);                                          // mdef
			packet.PutShort(0);                                          // element
																		 // Element resistances (water/earth/fire/wind/poison/holy/shadow/ghost/undead).
			for (var i = 0; i < 9; i++)
				packet.PutByte(0);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Acknowledges a crafting attempt (AC_MAKINGARROW, BS_MAKINGITEM,
		/// etc.) with the result + the item that was produced. Mirrors
		/// rA <c>clif_produceeffect</c>
		/// (<c>ZC_ACK_REQMAKINGITEM</c> 0x018F). At Sabine PV 50000 the
		/// itemId is widened to uint32 (rA PV>=20181121, 8-byte wire).
		/// </summary>
		/// <param name="result">0 = success, 1 = failure.</param>
		/// <param name="classId">Produced item class id (0 on failure).</param>
		public static void ZC_ACK_REQMAKINGITEM(PlayerCharacter character, int result, int classId)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REQMAKINGITEM);
			packet.PutShort((short)result);

			if (Game.Version >= Versions.Sabine)
				packet.PutInt(classId);
			else
				packet.PutShort((short)classId);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens the Mastersmith forge / Whitesmith craft UI with the
		/// list of items the player can produce given their inventory.
		/// Per-entry layout = itemId + 3 × material ids. Mirrors rA
		/// <c>clif_skill_produce_mix_list</c>
		/// (<c>ZC_MAKABLEITEMLIST</c> 0x018D, dynamic). At Sabine PV
		/// 50000 each entry widens to 16 bytes (uint32 itemId +
		/// 3 × uint32 materials); pre-Sabine writes the 8-byte uint16
		/// form.
		/// </summary>
		/// <summary>
		/// Updates the pet detail card (name, level, fullness,
		/// intimacy, egg item, job sprite) on the owner's client.
		/// Mirrors rA <c>clif_send_petstatus</c>
		/// (<c>ZC_PROPERTY_PET</c> 0x01A2). Sabine PV 50000 includes
		/// the trailing 2-byte job field added at rA PV >= 20081126
		/// (37-byte wire).
		/// </summary>
		public static void ZC_PROPERTY_PET(PlayerCharacter owner, Pet pet, int eggItemClassId)
		{
			var petComp = owner.Pet;
			if (petComp == null)
				return;

			using var packet = Packet.Rent(Op.ZC_PROPERTY_PET);
			packet.PutString(petComp.Name, 24);
			packet.PutByte(0);                              // bModified (1 if name has been changed)
			packet.PutShort((short)pet.Parameters.BaseLevel);
			packet.PutShort((short)petComp.Hunger);
			packet.PutShort((short)petComp.Intimacy);
			packet.PutShort((short)eggItemClassId);

			if (Game.Version >= Versions.Sabine)
				packet.PutShort((short)pet.IdentityId);     // job sprite (PV>=20081126)

			owner.Connection.Send(packet);
		}

		/// <summary>
		/// Confirms a pet-feed attempt and echoes which food item was
		/// consumed. Mirrors rA <c>clif_feed_pet</c>
		/// (<c>ZC_FEED_PET</c> 0x01A3). At Sabine PV 50000 the itemId
		/// is widened to uint32 (7-byte wire).
		/// </summary>
		/// <param name="result">0 = success, 1 = fail.</param>
		public static void ZC_FEED_PET(PlayerCharacter owner, int result, int foodClassId)
		{
			using var packet = Packet.Rent(Op.ZC_FEED_PET);
			packet.PutByte((byte)result);

			if (Game.Version >= Versions.Sabine)
				packet.PutInt(foodClassId);
			else
				packet.PutShort((short)foodClassId);

			owner.Connection.Send(packet);
		}

		/// <summary>
		/// Updates a single pet state field on the owner's client
		/// (intimacy, hunger, accessory, or performance pose). Mirrors
		/// rA <c>clif_pet_emotion</c> family
		/// (<c>ZC_CHANGESTATE_PET</c> 0x01A4, 11 bytes).
		/// </summary>
		/// <param name="type">
		/// 0 = pre-spawn, 1 = intimacy update, 2 = hunger update,
		/// 3 = accessory update, 4 = performance / emote id.
		/// </param>
		public static void ZC_CHANGESTATE_PET(PlayerCharacter owner, byte type, int petHandle, int data)
		{
			using var packet = Packet.Rent(Op.ZC_CHANGESTATE_PET);
			packet.PutByte(type);
			packet.PutInt(petHandle);
			packet.PutInt(data);

			owner.Connection.Send(packet);
		}

		/// <summary>
		/// Opens the Sage / Mage Item Appraisal UI showing the player's
		/// unidentified inventory items. Per-entry = 2-byte inventory
		/// index. Mirrors rA <c>clif_item_identify_list</c>
		/// (<c>ZC_ITEMIDENTIFY_LIST</c> 0x0177, dynamic).
		/// </summary>
		public static void ZC_ITEMIDENTIFY_LIST(PlayerCharacter character, IEnumerable<int> unidentifiedInvIds)
		{
			using var packet = Packet.Rent(Op.ZC_ITEMIDENTIFY_LIST);

			foreach (var invId in unidentifiedInvIds)
				packet.PutShort((short)invId);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// GM-tooling reply: resolves an account id to its username.
		/// Mirrors rA <c>clif_account_name</c>
		/// (<c>ZC_ACK_ACCOUNTNAME</c> 0x01E0, 30 bytes).
		/// </summary>
		public static void ZC_ACK_ACCOUNTNAME(PlayerCharacter character, int accountId, string accountName)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_ACCOUNTNAME);
			packet.PutInt(accountId);
			packet.PutString(accountName, 24);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates a single quest's active/inactive flag on the player's
		/// quest log. Mirrors rA <c>clif_quest_update_status</c>
		/// (<c>ZC_ACTIVE_QUEST</c> 0x02B7, 7 bytes).
		/// </summary>
		public static void ZC_ACTIVE_QUEST(PlayerCharacter character, int questId, bool active)
		{
			using var packet = Packet.Rent(Op.ZC_ACTIVE_QUEST);
			packet.PutInt(questId);
			packet.PutByte(active);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts a character's chosen chat font id to nearby
		/// players so their messages render in the selected font.
		/// Mirrors rA <c>clif_font</c> (<c>ZC_NOTIFY_FONT</c> 0x02EF,
		/// 8 bytes). Sent on map enter and whenever
		/// <c>CZ_REQ_CHANGE_FONT</c> updates it.
		/// </summary>
		public static void ZC_NOTIFY_FONT(PlayerCharacter character, int fontId)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_FONT);
			packet.PutInt(character.Connection?.Account?.Id ?? character.Handle);
			packet.PutShort((short)fontId);

			if (character.Map != null)
				character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Acks the guild-creation request. Mirrors rA
		/// <c>clif_guild_created</c> (<c>ZC_RESULT_MAKE_GUILD</c> 0x0167,
		/// 3 bytes: opcode + 1-byte result).
		/// </summary>
		/// <param name="result">0 = success, 1 = name taken, 2 = emblem
		/// duplicate (unused in Sabine), 3 = already in a guild.</param>
		public static void ZC_RESULT_MAKE_GUILD(PlayerCharacter character, byte result)
		{
			using var packet = Packet.Rent(Op.ZC_RESULT_MAKE_GUILD);
			packet.PutByte(result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Tells the inviter whether the recipient accepted their guild
		/// invitation. Mirrors rA <c>clif_guild_inviteack</c>
		/// (<c>ZC_ACK_REQ_JOIN_GUILD</c> 0x0169, 3 bytes).
		/// </summary>
		/// <param name="result">0 = already in guild, 1 = rejected,
		/// 2 = accepted, 3 = guild full.</param>
		public static void ZC_ACK_REQ_JOIN_GUILD(PlayerCharacter inviter, byte result)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REQ_JOIN_GUILD);
			packet.PutByte(result);

			inviter.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the leaving player + their guild that a member left
		/// (or was expelled). Mirrors rA <c>clif_guild_leave</c>
		/// (<c>ZC_ACK_LEAVE_GUILD</c> 0x015A, 66 bytes: 24-byte name +
		/// 40-byte reason).
		/// </summary>
		public static void ZC_ACK_LEAVE_GUILD(PlayerCharacter character, string memberName, string reason)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_LEAVE_GUILD);
			packet.PutString(memberName, 24);
			packet.PutString(reason, 40);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts a guild-channel chat message to a single member.
		/// Mirrors rA <c>clif_guild_message</c>
		/// (<c>ZC_GUILD_CHAT</c> 0x017F, dynamic). Callers typically
		/// iterate all online members and send to each.
		/// </summary>
		public static void ZC_GUILD_CHAT(PlayerCharacter character, string message)
		{
			using var packet = Packet.Rent(Op.ZC_GUILD_CHAT);
			packet.PutString(message);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Swaps the sprite of an NPC mid-script. Mirrors rA
		/// <c>clif_changelook_npc</c>
		/// (<c>ZC_NPCSPRITE_CHANGE</c> 0x01B0, 11 bytes). type = sprite
		/// slot (LOOK_BASE=0 for body sprite, LOOK_WEAPON=1, etc.);
		/// class is the new id for that slot.
		/// </summary>
		public static void ZC_NPCSPRITE_CHANGE(Character actor, byte type, int classId)
		{
			using var packet = Packet.Rent(Op.ZC_NPCSPRITE_CHANGE);
			packet.PutInt(actor.Handle);
			packet.PutByte(type);
			packet.PutInt(classId);

			if (actor.Map != null)
				actor.Map.Broadcast(packet, actor, BroadcastTargets.All);
		}

		/// <summary>
		/// Echoes a client setting back to the player after toggling
		/// (e.g. open/close cart, equip-window). Mirrors rA
		/// <c>clif_configuration</c>
		/// (<c>ZC_CONFIG_NOTIFY</c> 0x02DA, 3 bytes).
		/// </summary>
		public static void ZC_CONFIG_NOTIFY(PlayerCharacter character, byte flag)
		{
			using var packet = Packet.Rent(Op.ZC_CONFIG_NOTIFY);
			packet.PutByte(flag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Reply to <c>CZ_REQNAME_BYGID</c>: resolves a character id to
		/// their name. Mirrors rA <c>clif_solved_charname</c>
		/// (<c>ZC_ACK_REQNAME_BYGID</c> 0x0194, 30 bytes).
		/// </summary>
		public static void ZC_ACK_REQNAME_BYGID(PlayerCharacter character, int charId, string name)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REQNAME_BYGID);
			packet.PutInt(charId);
			packet.PutString(name, 24);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Reply for the unified name+party+guild+position lookup.
		/// Mirrors rA <c>clif_charnameack</c>
		/// (<c>ZC_ACK_REQNAMEALL</c> 0x0195, 102 bytes). Triggered when
		/// the client hovers a character to show the full identity card.
		/// </summary>
		public static void ZC_ACK_REQNAMEALL(PlayerCharacter recipient, int gid, string name, string partyName, string guildName, string positionName)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REQNAMEALL);
			packet.PutInt(gid);
			packet.PutString(name, 24);
			packet.PutString(partyName, 24);
			packet.PutString(guildName, 24);
			packet.PutString(positionName, 24);

			recipient.Connection.Send(packet);
		}

		/// <summary>
		/// Opens the guild info window (top-level summary tab) with
		/// counts, levels, exp, alignment, treasury, and master name.
		/// Mirrors rA <c>clif_guild_basicinfo</c>
		/// (<c>ZC_GUILD_INFO2</c> 0x01B6, 114 bytes — rA's classic
		/// 0x01b6 layout). At PV>=20161019 rA switches to 0x0a84;
		/// Sabine PV 50000 keeps the 0x01b6 form since open-midgard
		/// targets that layout.
		/// </summary>
		public static void ZC_GUILD_INFO2(PlayerCharacter character, Sabine.Zone.World.Groups.Guild guild, int onlineMemberCount, int averageLevel, int maxMembers)
		{
			using var packet = Packet.Rent(Op.ZC_GUILD_INFO2);
			packet.PutInt(guild.Id);
			packet.PutInt(guild.Level);
			packet.PutInt(onlineMemberCount);
			packet.PutInt(maxMembers);
			packet.PutInt(averageLevel);
			packet.PutInt(guild.ExpCurrent);
			packet.PutInt(0);                            // maxExp (next level cap, lookup later)
			packet.PutInt(0);                            // point
			packet.PutInt(0);                            // honor
			packet.PutInt(0);                            // virtue
			packet.PutInt(guild.EmblemId);
			packet.PutString(guild.Name, 24);
			packet.PutString(guild.MasterName ?? "", 24);
			packet.PutString("", 16);                    // manageLand (castle name)
			packet.PutInt(0);                            // zeny (treasury)

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Pushes a guild emblem bitmap to a player so the client can
		/// render it on member sprites, castle flags, and the guild
		/// window. Mirrors rA <c>clif_guild_emblem</c>
		/// (<c>ZC_GUILD_EMBLEM_IMG</c> 0x0152, dynamic). emblem_data is
		/// the raw BMP bytes (typically &lt; 2KB).
		/// </summary>
		public static void ZC_GUILD_EMBLEM_IMG(PlayerCharacter character, int guildId, int emblemVersion, byte[] emblemData)
		{
			using var packet = Packet.Rent(Op.ZC_GUILD_EMBLEM_IMG);
			packet.PutInt(guildId);
			packet.PutInt(emblemVersion);

			if (emblemData != null && emblemData.Length > 0)
				packet.PutBytes(emblemData);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Pops the guild-invite popup on the target's client. Mirrors
		/// rA <c>clif_guild_invite</c> (<c>ZC_REQ_JOIN_GUILD</c> 0x016A,
		/// 30 bytes). Recipient replies via <c>CZ_JOIN_GUILD</c> with
		/// the guild_id echo.
		/// </summary>
		public static void ZC_REQ_JOIN_GUILD(PlayerCharacter target, int guildId, string guildName)
		{
			using var packet = Packet.Rent(Op.ZC_REQ_JOIN_GUILD);
			packet.PutInt(guildId);
			packet.PutString(guildName, 24);

			target.Connection.Send(packet);
		}

		/// <summary>
		/// Populates the guild Members tab with one row per online +
		/// offline member. Per-entry layout matches rA's classic 0x0154
		/// pre-2016 form (AID + GID + appearance + level + exp + state
		/// + position + 50-byte intro + 24-byte name = 104 bytes).
		/// Mirrors rA <c>clif_guild_memberlist</c>.
		/// </summary>
		public static void ZC_MEMBERMGR_INFO(PlayerCharacter character, IEnumerable<Sabine.Zone.World.Groups.GuildMember> members)
		{
			using var packet = Packet.Rent(Op.ZC_MEMBERMGR_INFO);

			foreach (var member in members)
			{
				var player = member.Player;
				packet.PutInt(player?.Connection?.Account?.Id ?? 0);   // AID
				packet.PutInt(member.CharacterId);                     // GID
				packet.PutShort(0);                                    // head
				packet.PutShort(0);                                    // headPalette
				packet.PutShort((short)(player?.Sex ?? Sex.Male));     // sex
				packet.PutShort((short)(player?.JobId ?? 0));          // job
				packet.PutShort((short)(player?.Parameters.BaseLevel ?? 0));
				packet.PutInt(0);                                      // contributionExp
				packet.PutInt(player != null ? 1 : 0);                 // currentState (1=online)
				packet.PutInt(member.IsMaster ? 0 : 1);                // positionID (0=master, 1=member)
				packet.PutString("", 50);                              // intro
				packet.PutString(player?.Name ?? "", 24);              // char_name
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Populates the guild Skills tab. Per-entry = id + inf + level
		/// + sp + range + name(24) + upFlag = 37 bytes. Mirrors rA
		/// <c>clif_guild_skillinfo</c>
		/// (<c>ZC_GUILD_SKILLINFO</c> 0x0162, dynamic).
		/// </summary>
		public static void ZC_GUILD_SKILLINFO(PlayerCharacter character, short skillPoints, IEnumerable<(short id, int inf, short level, short sp, short range, string name, bool upgradable)> skills)
		{
			using var packet = Packet.Rent(Op.ZC_GUILD_SKILLINFO);
			packet.PutShort(skillPoints);

			foreach (var (id, inf, level, sp, range, name, upgradable) in skills)
			{
				packet.PutShort(id);
				packet.PutInt(inf);
				packet.PutShort(level);
				packet.PutShort(sp);
				packet.PutShort(range);
				packet.PutString(name, 24);
				packet.PutByte(upgradable);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens the pet egg / incubator UI listing all egg items in the
		/// player's inventory. Per-entry = 2-byte inventory index.
		/// Mirrors rA <c>clif_sendegg</c>
		/// (<c>ZC_PETEGG_LIST</c> 0x01A6, dynamic).
		/// </summary>
		public static void ZC_PETEGG_LIST(PlayerCharacter character, IEnumerable<int> eggInvIds)
		{
			using var packet = Packet.Rent(Op.ZC_PETEGG_LIST);

			foreach (var invId in eggInvIds)
				packet.PutShort((short)invId);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies clients near an actor that one of its skill metas
		/// changed (level / cooldown / availability). Mirrors rA
		/// <c>clif_skillinfo_update</c>
		/// (<c>ZC_SKILL_UPDATE</c> 0x01AC, 6 bytes). Followed by the
		/// usual <c>ZC_SKILLINFO_UPDATE</c> for the owner.
		/// </summary>
		public static void ZC_SKILL_UPDATE(Character actor)
		{
			using var packet = Packet.Rent(Op.ZC_SKILL_UPDATE);
			packet.PutInt(actor.Handle);

			if (actor.Map != null)
				actor.Map.Broadcast(packet, actor, BroadcastTargets.All);
		}

		/// <summary>
		/// Notifies the guild that a member was expelled (kicked).
		/// Mirrors rA <c>clif_guild_explusion</c>
		/// (<c>ZC_ACK_BAN_GUILD</c> 0x015C, 90 bytes — classic V1:
		/// 24-byte name + 40-byte reason + 24-byte account_name).
		/// </summary>
		public static void ZC_ACK_BAN_GUILD(PlayerCharacter character, string memberName, string reason, string accountName)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_BAN_GUILD);
			packet.PutString(memberName, 24);
			packet.PutString(reason, 40);
			packet.PutString(accountName, 24);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Disband prompt — sent to the guild master after they request
		/// a break. Mirrors rA <c>clif_guild_break</c> (<c>ZC_ACK_DISORGANIZE_GUILD</c>
		/// 0x015F, 42 bytes — 40-byte reason).
		/// </summary>
		public static void ZC_ACK_DISORGANIZE_GUILD(PlayerCharacter character, string reason)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_DISORGANIZE_GUILD);
			packet.PutString(reason, 40);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Disband result. Mirrors rA <c>clif_guild_broken</c>
		/// (<c>ZC_ACK_DISORGANIZE_GUILD_RESULT</c> 0x015E, 6 bytes).
		/// flag: 0=success, 1=invalid key, 2=still has members.
		/// </summary>
		public static void ZC_ACK_DISORGANIZE_GUILD_RESULT(PlayerCharacter character, int flag)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_DISORGANIZE_GUILD_RESULT);
			packet.PutInt(flag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Pops the alliance-request dialog on the target guild master.
		/// Mirrors rA <c>clif_guild_reqalliance</c>
		/// (<c>ZC_REQ_ALLY_GUILD</c> 0x0171, 30 bytes: 4-byte inviterAID
		/// + 24-byte inviterName). Recipient replies via
		/// <c>CZ_ALLY_GUILD</c>.
		/// </summary>
		/// <summary>
		/// Sends the full position roster (all 9 slots) to a client.
		/// Mirrors rA <c>clif_guild_positionnamelist</c>
		/// (<c>ZC_POSITION_INFO</c> 0x0160, dynamic): each entry is
		/// u32 positionId + char[24] name = 28 bytes.
		/// </summary>
		public static void ZC_POSITION_INFO(PlayerCharacter character, Sabine.Zone.World.Groups.Guild guild)
		{
			using var packet = Packet.Rent(Op.ZC_POSITION_INFO);

			for (var i = 0; i < Sabine.Zone.World.Groups.Guild.PositionCount; i++)
			{
				packet.PutInt(i);
				packet.PutString(guild.Positions[i].Name ?? string.Empty, 24);
			}

			character.Connection.Send(packet);
		}

		public static void ZC_REQ_ALLY_GUILD(PlayerCharacter target, int inviterAccountId, string inviterName)
		{
			using var packet = Packet.Rent(Op.ZC_REQ_ALLY_GUILD);
			packet.PutInt(inviterAccountId);
			packet.PutString(inviterName ?? string.Empty, 24);

			target.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the inviter whether their alliance request was
		/// accepted. Mirrors rA <c>clif_guild_allianceack</c>
		/// (<c>ZC_ACK_REQ_ALLY_GUILD</c> 0x0173, 3 bytes).
		/// </summary>
		/// <param name="result">0 = accepted, 1 = rejected,
		/// 2 = ally count full, 3 = guild is hostile.</param>
		public static void ZC_ACK_REQ_ALLY_GUILD(PlayerCharacter character, byte result)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REQ_ALLY_GUILD);
			packet.PutByte(result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the requester whether their hostility (antagonist)
		/// request was accepted. Mirrors rA <c>clif_guild_oppositionack</c>
		/// (<c>ZC_ACK_REQ_HOSTILE_GUILD</c> 0x0181, 3 bytes).
		/// </summary>
		/// <param name="result">0 = set, 1 = too many antagonists,
		/// 2 = already antagonist, 3 = disabled.</param>
		public static void ZC_ACK_REQ_HOSTILE_GUILD(PlayerCharacter character, byte result)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REQ_HOSTILE_GUILD);
			packet.PutByte(result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Adds an ally or antagonist row to the guild's relations
		/// list. Mirrors rA <c>clif_guild_allianceadded</c>
		/// (<c>ZC_ADD_RELATED_GUILD</c> 0x0185, 34 bytes).
		/// relation: 0 = ally, 1 = antagonist.
		/// </summary>
		public static void ZC_ADD_RELATED_GUILD(PlayerCharacter character, int relation, int otherGuildId, string otherGuildName)
		{
			using var packet = Packet.Rent(Op.ZC_ADD_RELATED_GUILD);
			packet.PutInt(relation);
			packet.PutInt(otherGuildId);
			packet.PutString(otherGuildName, 24);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Removes an ally or antagonist row from the relations list.
		/// Mirrors rA <c>clif_guild_delalliance</c>
		/// (<c>ZC_DELETE_RELATED_GUILD</c> 0x0184, 10 bytes).
		/// flag: 0 = ally, 1 = antagonist.
		/// </summary>
		public static void ZC_DELETE_RELATED_GUILD(PlayerCharacter character, int otherGuildId, int flag)
		{
			using var packet = Packet.Rent(Op.ZC_DELETE_RELATED_GUILD);
			packet.PutInt(otherGuildId);
			packet.PutInt(flag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sets the bitmask of enabled guild window tabs for the
		/// current member. Mirrors rA <c>clif_guild_masterormember</c>
		/// (<c>ZC_ACK_GUILD_MENUINTERFACE</c> 0x014E, 6 bytes). Master
		/// flag = 0xD7 (everything), member flag = 0x57 (no expulsion /
		/// no notice edit).
		/// </summary>
		public static void ZC_ACK_GUILD_MENUINTERFACE(PlayerCharacter character, int menuFlag)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_GUILD_MENUINTERFACE);
			packet.PutInt(menuFlag);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Result of a guild-treasury deposit attempt. Mirrors rA
		/// <c>clif_guild_zenyack</c> (<c>ZC_GUILD_ZENY_ACK</c> 0x01B8,
		/// 3 bytes). result: 0 = success, 1 = fail.
		/// </summary>
		public static void ZC_GUILD_ZENY_ACK(PlayerCharacter character, byte result)
		{
			using var packet = Packet.Rent(Op.ZC_GUILD_ZENY_ACK);
			packet.PutByte(result);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Replies to <c>/pvpinfo</c> with the caller's PvP stats.
		/// Mirrors rA <c>clif_PVPInfo</c>
		/// (<c>ZC_ACK_PVPPOINT</c> 0x0210, 22 bytes).
		/// </summary>
		public static void ZC_ACK_PVPPOINT(PlayerCharacter character, int wins, int losses, int points)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_PVPPOINT);
			packet.PutInt(character.Id);
			packet.PutInt(character.Connection?.Account?.Id ?? character.Handle);
			packet.PutInt(wins);
			packet.PutInt(losses);
			packet.PutInt(points);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Detail popup for a single guild member (right-click on the
		/// members tab). Mirrors rA <c>clif_guild_memberinfo</c>
		/// (<c>ZC_ACK_GUILD_MEMBER_INFO</c> 0x0176, 106 bytes).
		/// </summary>
		public static void ZC_ACK_GUILD_MEMBER_INFO(PlayerCharacter character, int aid, int gid, int head, int hp, int sex, int job, int level, int contributionExp, int currentState, int positionId, string intro, string name)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_GUILD_MEMBER_INFO);
			packet.PutInt(aid);
			packet.PutInt(gid);
			packet.PutShort((short)head);
			packet.PutShort((short)hp);
			packet.PutShort((short)sex);
			packet.PutShort((short)job);
			packet.PutShort((short)level);
			packet.PutInt(contributionExp);
			packet.PutInt(currentState);
			packet.PutInt(positionId);
			packet.PutString(intro, 50);
			packet.PutString(name, 24);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Renders the guild ban (expel) history list. Per-entry uses
		/// rA's classic V1 layout: 24-byte char name + 24-byte account
		/// name + 40-byte reason = 88 bytes. Mirrors rA
		/// <c>clif_guild_expulsionlist</c>
		/// (<c>ZC_BAN_LIST</c> 0x0163, dynamic).
		/// </summary>
		public static void ZC_BAN_LIST(PlayerCharacter character, IEnumerable<(string charName, string accountName, string reason)> entries)
		{
			using var packet = Packet.Rent(Op.ZC_BAN_LIST);

			foreach (var (charName, accountName, reason) in entries)
			{
				packet.PutString(charName, 24);
				packet.PutString(accountName, 24);
				packet.PutString(reason, 40);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Pushes one stat detail entry to the Status window — shows
		/// base value + bonus (e.g. STR 50 (+10)). Despite the
		/// historical name, this is the per-stat detail packet, not
		/// marriage-related. Mirrors rA <c>clif_couplestatus</c>
		/// (<c>ZC_COUPLESTATUS</c> 0x0141, 14 bytes).
		/// </summary>
		public static void ZC_COUPLESTATUS(PlayerCharacter character, int statusType, int defaultValue, int plusValue)
		{
			using var packet = Packet.Rent(Op.ZC_COUPLESTATUS);
			packet.PutInt(statusType);
			packet.PutInt(defaultValue);
			packet.PutInt(plusValue);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the guild Positions tab — pushes the (renamed/
		/// re-flagged) position rows back to all online members.
		/// Mirrors rA <c>clif_guild_positionchanged</c>
		/// (<c>ZC_ACK_CHANGE_GUILD_POSITIONINFO</c> 0x0174, dynamic).
		/// Per-entry layout = positionId(4) + mode(4) + ranking(4) +
		/// payRate(4) + name(24) = 40 bytes.
		/// </summary>
		public static void ZC_ACK_CHANGE_GUILD_POSITIONINFO(PlayerCharacter character, IEnumerable<(int positionId, int mode, int ranking, int payRate, string name)> positions)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_CHANGE_GUILD_POSITIONINFO);

			foreach (var (positionId, mode, ranking, payRate, name) in positions)
			{
				packet.PutInt(positionId);
				packet.PutInt(mode);
				packet.PutInt(ranking);
				packet.PutInt(payRate);
				packet.PutString(name, 24);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts that one or more members were re-assigned to a
		/// different guild position. Mirrors rA
		/// <c>clif_guild_memberpositionchanged</c>
		/// (<c>ZC_ACK_REQ_CHANGE_MEMBERS</c> 0x0156, dynamic).
		/// Per-entry layout = AID(4) + CID(4) + positionId(4) = 12.
		/// </summary>
		public static void ZC_ACK_REQ_CHANGE_MEMBERS(PlayerCharacter character, IEnumerable<(int accountId, int charId, int positionId)> changes)
		{
			using var packet = Packet.Rent(Op.ZC_ACK_REQ_CHANGE_MEMBERS);

			foreach (var (accountId, charId, positionId) in changes)
			{
				packet.PutInt(accountId);
				packet.PutInt(charId);
				packet.PutInt(positionId);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Opens the "view equipment" window — shows what another
		/// player is wearing. Mirrors rA <c>clif_viewequip_ack</c>
		/// (<c>ZC_EQUIPWIN_MICROSCOPE</c> 0x02D7, dynamic). Per-equip
		/// entry uses the V6 layout (59 bytes) under Sabine PV 50000
		/// via <see cref="PacketItemExtensions.AddEquipItemV6"/>.
		/// </summary>
		public static void ZC_EQUIPWIN_MICROSCOPE(PlayerCharacter viewer, PlayerCharacter target)
		{
			using var packet = Packet.Rent(Op.ZC_EQUIPWIN_MICROSCOPE);
			packet.PutString(target.Name, 24);
			packet.PutShort((short)target.JobId);
			packet.PutShort((short)target.HairId);
			packet.PutShort((short)target.HeadBottomLook);
			packet.PutShort((short)target.HeadTopLook);
			packet.PutShort((short)target.HeadMiddleLook);
			packet.PutShort(0);                            // headpalette
			packet.PutShort(0);                            // bodypalette
			packet.PutByte((byte)target.Sex);

			foreach (var item in target.Inventory.GetEquippedItems())
			{
				if (item == null || !item.Type.IsEquip())
					continue;
				packet.AddEquipItemV6(item, item.EquippedOn);
			}

			viewer.Connection.Send(packet);
		}

		public static void ZC_MAKABLEITEMLIST(PlayerCharacter character, IEnumerable<(int classId, int mat1, int mat2, int mat3)> recipes)
		{
			using var packet = Packet.Rent(Op.ZC_MAKABLEITEMLIST);

			var wideId = Game.Version >= Versions.Sabine;
			foreach (var (classId, mat1, mat2, mat3) in recipes)
			{
				if (wideId)
				{
					packet.PutInt(classId);
					packet.PutInt(mat1);
					packet.PutInt(mat2);
					packet.PutInt(mat3);
				}
				else
				{
					packet.PutShort((short)classId);
					packet.PutShort((short)mat1);
					packet.PutShort((short)mat2);
					packet.PutShort((short)mat3);
				}
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Character-base overload of <see cref="ZC_NOTIFY_EFFECT"/>, so
		/// NPCs and monsters can also be the source of an effect.
		/// </summary>
		public static void ZC_NOTIFY_EFFECT(Character character, int effectId)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_EFFECT);

			packet.PutInt(character.Handle);
			packet.PutInt((int)effectId);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Displays effect on character, for them and the characters
		/// nearby.
		/// </summary>
		/// <remarks>
		/// First seen in eu20040512, these effects appear to be more
		/// general purpose than the original ZC_NOTIFY_EFFECT and make
		/// up the vast majority of effects in use.
		/// </remarks>
		/// <param name="character"></param>
		/// <param name="effectId"></param>
		public static void ZC_NOTIFY_EFFECT2(PlayerCharacter character, EffectId effectId)
			=> ZC_NOTIFY_EFFECT2((Character)character, effectId);

		/// <summary>
		/// Character-base overload of <see cref="ZC_NOTIFY_EFFECT2"/>.
		/// </summary>
		public static void ZC_NOTIFY_EFFECT2(Character character, EffectId effectId)
		{
			using var packet = Packet.Rent(Op.ZC_NOTIFY_EFFECT2);

			packet.PutInt(character.Handle);
			packet.PutInt((int)effectId);

			character.Map.Broadcast(packet, character, BroadcastTargets.All);
		}

		/// <summary>
		/// Sends the saved hotkey bar to the client (S2500+).
		/// </summary>
		/// <remarks>
		/// Reference: rAthena <c>clif_hotkeys_send</c>
		/// (<c>src/map/clif.cpp:11215</c>) — 27 slots × (B isSkill +
		/// L id + W count) = 189 + 2 op header = 191 bytes.
		/// </remarks>
		public static void ZC_SHORTCUT_KEY_LIST(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_SHORTCUT_KEY_LIST);

			foreach (var entry in character.Hotkeys.GetAll())
			{
				packet.PutByte(entry.IsSkill);
				packet.PutInt(entry.Id);
				packet.PutShort(entry.Count);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the full quest log to the client (S2500+).
		/// </summary>
		/// <remarks>
		/// Reference: rAthena <c>clif_quest_send_list</c>
		/// (<c>clif.cpp:17955</c>, pre-20141022 path) — W op, W len,
		/// L count, then per-quest (L id + B state).
		/// </remarks>
		public static void ZC_ALL_QUEST_LIST(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_ALL_QUEST_LIST);

			var entries = character.Quests.ListAll();
			packet.PutInt(entries.Count);

			foreach (var entry in entries)
			{
				packet.PutInt(entry.Id);
				packet.PutByte((byte)entry.State);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the active quest objectives to the client (S2500+).
		/// </summary>
		/// <remarks>
		/// Reference: rAthena <c>clif_quest_send_mission</c>
		/// (<c>clif.cpp:17974</c>) — W op, W len, L count, then
		/// per-quest 104-byte block: L id + L start + L expire + W
		/// mobs + 3×(L mob_id + W cur + 24B name). Mob IDs / names
		/// stay zeroed until a quest DB lands; the client tolerates
		/// empty objective slots.
		/// </remarks>
		public static void ZC_ALL_QUEST_MISSION(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_ALL_QUEST_MISSION);

			var entries = character.Quests.ListActive();
			packet.PutInt(entries.Count);

			foreach (var entry in entries)
			{
				packet.PutInt(entry.Id);
				packet.PutInt(entry.AcceptedAtUtc == default ? 0 : (int)ToUnixSeconds(entry.AcceptedAtUtc));
				packet.PutInt(0);                // expire_time
				packet.PutShort(0);              // objective mob count
				for (var i = 0; i < 3; i++)
				{
					packet.PutInt(0);            // mob_id
					packet.PutShort((short)entry.Counts[i]);
					packet.PutString(string.Empty, 24); // mob name
				}
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the client that a quest was just accepted (S2500+).
		/// Fixed 107 bytes: L id + B state + L start + L expire +
		/// W mobs + 3×(L mob_id + W cur + 24B name).
		/// </summary>
		/// <remarks>
		/// Reference: rAthena <c>clif_quest_add</c>
		/// (<c>clif.cpp:18016</c>, 0x2b3 pre-20150513 path).
		/// </remarks>
		public static void ZC_ADD_QUEST(PlayerCharacter character, QuestEntry entry)
		{
			using var packet = Packet.Rent(Op.ZC_ADD_QUEST);

			packet.PutInt(entry.Id);
			packet.PutByte((byte)entry.State);
			packet.PutInt(entry.AcceptedAtUtc == default ? 0 : (int)ToUnixSeconds(entry.AcceptedAtUtc));
			packet.PutInt(0);                    // expire_time
			packet.PutShort(0);                  // objective mob count
			for (var i = 0; i < 3; i++)
			{
				packet.PutInt(0);                // mob_id
				packet.PutShort((short)entry.Counts[i]);
				packet.PutString(string.Empty, 24);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the client that a quest was removed (S2500+).
		/// Fixed 6 bytes: L quest_id.
		/// </summary>
		/// <remarks>
		/// Reference: rAthena <c>clif_quest_delete</c>
		/// (<c>clif.cpp:18094</c>).
		/// </remarks>
		public static void ZC_DEL_QUEST(PlayerCharacter character, int questId)
		{
			using var packet = Packet.Rent(Op.ZC_DEL_QUEST);
			packet.PutInt(questId);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates a hunting-mission counter on the client (S2500+).
		/// Dynamic: W op + W len + W mobs + 3×(L quest_id + L mob_id
		/// + W total + W cur).
		/// </summary>
		/// <remarks>
		/// Reference: rAthena <c>clif_quest_update_objective</c>
		/// (<c>clif.cpp:18108</c>, 0x2b5 pre-20150513 path). Sabine
		/// has no quest DB yet, so mob_id / total stay zero; the
		/// counter slot the client renders comes from the per-slot
		/// current count.
		/// </remarks>
		public static void ZC_UPDATE_MISSION_HUNT(PlayerCharacter character, QuestEntry entry)
		{
			using var packet = Packet.Rent(Op.ZC_UPDATE_MISSION_HUNT);

			packet.PutShort(3); // mobs (always 3 slots in this packet)
			for (var i = 0; i < 3; i++)
			{
				packet.PutInt(entry.Id);
				packet.PutInt(0);                // mob_id
				packet.PutShort(0);              // total
				packet.PutShort((short)entry.Counts[i]);
			}

			character.Connection.Send(packet);
		}


		/// <summary>
		/// Sends the cash shop catalog to the client and pops the
		/// shop window (S2500+).
		/// </summary>
		/// <remarks>
		/// Reference: rAthena <c>clif_cashshop_show</c>
		/// (<c>clif.cpp:17404</c>). At PV20070711+ the header carries
		/// both cash and kafra balances; each entry is
		/// L price + L discountPrice + B itemType + W itemId = 11
		/// bytes (uint16 itemId pre-2018).
		/// </remarks>
		public static void ZC_PC_CASH_POINT_ITEMLIST(PlayerCharacter character, IEnumerable<Sabine.Shared.Data.Databases.CashShopEntry> entries)
		{
			using var packet = Packet.Rent(Op.ZC_PC_CASH_POINT_ITEMLIST);

			packet.PutInt(character.Parameters.CashPoints);
			packet.PutInt(character.Parameters.KafraPoints);

			var items = ZoneServer.Instance.Data.Items;
			foreach (var entry in entries)
			{
				var price = entry.KafraPrice > 0 ? entry.KafraPrice : entry.CashPrice;
				var itemType = items.Find(entry.ItemId)?.Type ?? Sabine.Shared.Const.ItemType.Etc;

				packet.PutInt(price);
				packet.PutInt(price); // discountPrice — same as price; no sales today
				packet.PutByte((byte)itemType);
				packet.PutShort((short)entry.ItemId);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies the client of a cash-shop purchase result and the
		/// new currency balances (S2500+).
		/// </summary>
		/// <remarks>
		/// Reference: rAthena <c>clif_cashshop_ack</c>
		/// (<c>clif.cpp:17452</c>). Fixed 12 bytes at PV20070711+:
		/// L cash + L kafra + W error.
		/// </remarks>
		public static void ZC_PC_CASH_POINT_UPDATE(PlayerCharacter character, short error)
		{
			using var packet = Packet.Rent(Op.ZC_PC_CASH_POINT_UPDATE);

			packet.PutInt(character.Parameters.CashPoints);
			packet.PutInt(character.Parameters.KafraPoints);
			packet.PutShort(error);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Acknowledges a client configuration toggle by echoing the
		/// new state back (S2500+).
		/// </summary>
		/// <remarks>
		/// Reference: rAthena <c>clif_parse_configuration</c> — fixed
		/// 10 bytes: W op + L type + L flag.
		/// </remarks>
		public static void ZC_CONFIG(PlayerCharacter character, int type, int flag)
		{
			using var packet = Packet.Rent(Op.ZC_CONFIG);
			packet.PutInt(type);
			packet.PutInt(flag);

			character.Connection.Send(packet);
		}

		// --- Mercenary senders ----------------------------------------------
		// Layouts per rAthena clif.cpp clif_mercenary_info /
		// clif_mercenary_skillblock / clif_mercenary_updatestatus.

		/// <summary>
		/// Sends the full mercenary panel state on hire / spawn. 0x029B,
		/// body 78 (Sabine widens to 80 wire). Field order matches rA's
		/// <c>clif_mercenary_info</c>.
		/// </summary>
		public static void ZC_MER_INIT(PlayerCharacter owner, Mercenary merc)
		{
			using var packet = Packet.Rent(Op.ZC_MER_INIT);

			var data = merc.Data;
			var contractSeconds = owner.Mercenary?.ContractSecondsRemaining ?? 0;
			var faith = owner.Mercenary != null
				? owner.Mercenary.GetFaith(owner.Mercenary.Faction)
				: 0;

			packet.PutInt(merc.Handle);                  // mercId / AID
			packet.PutShort((short)data.AttackMin);      // atk
			packet.PutShort((short)data.AttackMin);      // matk (no separate column today)
			packet.PutShort(0);                          // hit
			packet.PutShort(0);                          // crit
			packet.PutShort((short)data.Defense);        // def
			packet.PutShort((short)data.MagicDefense);   // mdef
			packet.PutShort(0);                          // flee
			packet.PutShort((short)data.AttackDelay);    // aspd

			// Prefer the persisted custom name on the component; fall
			// back to the live entity's name (which itself defaults to
			// the mob template).
			var displayName = owner.Mercenary != null && !string.IsNullOrEmpty(owner.Mercenary.Name)
				? owner.Mercenary.Name
				: (merc.Name ?? string.Empty);
			packet.PutString(displayName, 24);
			packet.PutShort((short)data.Level);          // level
			packet.PutInt(merc.Parameters.Hp);
			packet.PutInt(merc.Parameters.HpMax);
			packet.PutInt(merc.Parameters.Sp);
			packet.PutInt(merc.Parameters.SpMax);
			packet.PutInt(contractSeconds);              // expireTime (seconds)
			packet.PutShort((short)faith);               // faith
			packet.PutInt(0);                            // calls
			packet.PutInt(0);                            // kills
			packet.PutShort((short)data.AttackRange);    // atkRange

			owner.Connection.Send(packet);
		}

		/// <summary>
		/// Dumps the mercenary's skill tree. 0x029D, variable. Each entry:
		/// 2 skillId + 4 type + 2 level + 2 spCost + 2 atkRange + 24 name
		/// + 1 upgradable = 37 bytes.
		/// </summary>
		public static void ZC_MER_SKILLINFO_LIST(PlayerCharacter owner, Mercenary merc)
		{
			using var packet = Packet.Rent(Op.ZC_MER_SKILLINFO_LIST);

			var entries = Sabine.Shared.Game.CurrentData?.Mercenaries?.Entries;
			if (entries != null && owner.Mercenary != null
				&& entries.TryGetValue(owner.Mercenary.MobId, out var mercData)
				&& !string.IsNullOrEmpty(mercData.OffensiveSkill)
				&& Enum.TryParse<Sabine.Shared.Const.SkillId>(mercData.OffensiveSkill, out var skillId))
			{
				packet.PutShort((short)skillId);
				packet.PutInt(1); // skill type — 1 = active
				packet.PutShort((short)mercData.OffensiveSkillLevel);
				packet.PutShort(0); // spCost (unknown without skill-db lookup)
				packet.PutShort(0); // atkRange
				packet.PutString(mercData.OffensiveSkill, 24);
				packet.PutByte(0); // not upgradable
			}

			owner.Connection.Send(packet);
		}

		/// <summary>
		/// Reports the result of a mercenary feed-item use. 0x022F, body 3:
		/// result byte + u16 itemId (Sabine raises itemId to u32 elsewhere
		/// but this packet keeps the legacy size; see PacketTable.0900_EP9).
		/// </summary>
		public static void ZC_FEED_MER(PlayerCharacter owner, bool success, int itemId)
		{
			using var packet = Packet.Rent(Op.ZC_FEED_MER);
			packet.PutByte(success ? (byte)1 : (byte)0);
			packet.PutShort((short)itemId);
			owner.Connection.Send(packet);
		}

		/// <summary>
		/// Single-stat update for the mercenary panel. 0x02A2, body 6:
		/// u16 varId + u32 value. varId mirrors rA's SP_* constants
		/// (SP_ATK1, SP_MATK1, SP_HIT, ... SP_MERCFAITH).
		/// </summary>
		public static void ZC_MER_PAR_CHANGE(PlayerCharacter owner, short varId, int value)
		{
			using var packet = Packet.Rent(Op.ZC_MER_PAR_CHANGE);
			packet.PutShort(varId);
			packet.PutInt(value);
			owner.Connection.Send(packet);
		}

		// --- Pet senders ----------------------------------------------------
		// Layouts per rAthena clif.cpp clif_sendegg / clif_pet_roulette /
		// clif_catchpet_result.

		/// <summary>
		/// Dumps the player's pet-egg inventory as a list of
		/// inventory-slot indices. rAthena <c>clif_sendegg</c> (0x01A6,
		/// variable). Each entry is a <c>uint16</c> matching the InvId
		/// the client should echo in <c>CZ_SELECT_PETEGG</c>.
		/// </summary>
		public static void ZC_PETEGG_LIST(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_PETEGG_LIST);

			foreach (var item in character.Inventory.GetItems(static _ => true))
			{
				if (item.Type != Sabine.Shared.Const.ItemType.PetEgg)
					continue;
				packet.PutShort((short)item.InventoryId);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Tells the client the capture-attempt UI should open (roulette
		/// animation). rAthena <c>clif_catch_process</c>, 0x019E, opcode
		/// only (2 bytes).
		/// </summary>
		public static void ZC_START_CAPTURE(PlayerCharacter character)
		{
			using var packet = Packet.Rent(Op.ZC_START_CAPTURE);
			character.Connection.Send(packet);
		}

		/// <summary>
		/// Reports the result of a monster-capture attempt. rAthena
		/// <c>clif_pet_roulette</c>, 0x01A0, body = 1 byte (0=fail, 1=success).
		/// </summary>
		public static void ZC_TRYCAPTURE_MONSTER(PlayerCharacter character, bool success)
		{
			using var packet = Packet.Rent(Op.ZC_TRYCAPTURE_MONSTER);
			packet.PutByte(success ? (byte)1 : (byte)0);
			character.Connection.Send(packet);
		}

		// --- Friends-list senders -------------------------------------------
		// Layouts per rAthena clif.cpp clif_friendslist_* (latest PACKETVER).

		/// <summary>
		/// Sends the initial friends list on login. rAthena emits one
		/// 0x0201 packet per pc_authok; Sabine repurposes
		/// <c>ZC_ADD_FRIENDS_LIST</c> as the variable-length dump.
		/// </summary>
		public static void ZC_ADD_FRIENDS_LIST(PlayerCharacter character, IList<Sabine.Zone.World.Actors.Components.Characters.FriendEntry> friends)
		{
			using var packet = Packet.Rent(Op.ZC_ADD_FRIENDS_LIST);

			foreach (var f in friends)
			{
				packet.PutInt(f.AccountId);
				packet.PutInt(f.CharId);
				packet.PutString(f.Name ?? string.Empty, 24);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Toggles one friend's online status. rAthena
		/// <c>clif_friendslist_toggle</c>: AID + CID + offline byte (0 =
		/// online, 1 = offline) + name[24] when Sabine-era.
		/// </summary>
		public static void ZC_FRIENDS_STATE(PlayerCharacter character, int friendAccountId, int friendCharId, string friendName, bool online)
		{
			using var packet = Packet.Rent(Op.ZC_FRIENDS_STATE);

			packet.PutInt(friendAccountId);
			packet.PutInt(friendCharId);
			packet.PutByte(online ? (byte)0 : (byte)1);

			if (Game.Version >= Versions.Sabine)
				packet.PutString(friendName ?? string.Empty, 24);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the incoming friend-request popup to the target.
		/// rAthena <c>clif_friendlist_req</c>: AID + CID + name[24].
		/// </summary>
		public static void ZC_REQ_ADD_FRIENDS(PlayerCharacter target, int requesterAccountId, int requesterCharId, string requesterName)
		{
			using var packet = Packet.Rent(Op.ZC_REQ_ADD_FRIENDS);

			packet.PutInt(requesterAccountId);
			packet.PutInt(requesterCharId);
			packet.PutString(requesterName ?? string.Empty, 24);

			target.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies a friend deletion on either side. rAthena
		/// <c>clif_friendslist_remove</c>: AID + CID of the removed friend.
		/// </summary>
		public static void ZC_DELETE_FRIENDS(PlayerCharacter character, int friendAccountId, int friendCharId)
		{
			using var packet = Packet.Rent(Op.ZC_DELETE_FRIENDS);

			packet.PutInt(friendAccountId);
			packet.PutInt(friendCharId);

			character.Connection.Send(packet);
		}

		// --- Battleground senders -------------------------------------------
		// rAthena reference: clif.cpp:18410-18572 (clif_bg_*). PACKETVER 23.
		// Sabine S2500 keeps the pre-renewal int16 hp/maxhp layout — the
		// rAthena widening to int32 happens at PACKETVER >= 20140613.

		/// <summary>
		/// Broadcasts a chat line from a BG team member to every recipient
		/// in the team. rAthena <c>clif_bg_message</c> / 0x02DC, variable.
		/// </summary>
		public static void ZC_BATTLEFIELD_CHAT(IEnumerable<PlayerCharacter> recipients, int accountId, string name, string message)
		{
			using var packet = Packet.Rent(Op.ZC_BATTLEFIELD_CHAT);
			packet.PutInt(accountId);
			packet.PutString(name, 24);
			packet.PutString(message);

			foreach (var pc in recipients)
				pc.Connection.Send(packet);
		}

		/// <summary>
		/// Tells the client which camp (team) a BG member belongs to.
		/// rAthena <c>clif_sendbgemblem_*</c> / 0x02DD, size 32.
		/// </summary>
		public static void ZC_BATTLEFIELD_NOTIFY_CAMPINFO(PlayerCharacter target, int accountId, string name, short camp)
		{
			using var packet = Packet.Rent(Op.ZC_BATTLEFIELD_NOTIFY_CAMPINFO);
			packet.PutInt(accountId);
			packet.PutString(name, 24);
			packet.PutShort(camp);

			target.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the BG scoreboard (camp A vs camp B kill counts).
		/// rAthena <c>clif_bg_updatescore</c> / 0x02DE, size 6.
		/// </summary>
		public static void ZC_BATTLEFIELD_NOTIFY_POINT(IEnumerable<PlayerCharacter> recipients, short campA, short campB)
		{
			using var packet = Packet.Rent(Op.ZC_BATTLEFIELD_NOTIFY_POINT);
			packet.PutShort(campA);
			packet.PutShort(campB);

			foreach (var pc in recipients)
				pc.Connection.Send(packet);
		}

		/// <summary>
		/// Refreshes a BG member's minimap pin (position + class icon).
		/// rAthena <c>clif_bg_xy</c> / 0x02DF, size 36.
		/// </summary>
		public static void ZC_BATTLEFIELD_NOTIFY_POSITION(IEnumerable<PlayerCharacter> recipients, int accountId, string name, short job, short x, short y)
		{
			using var packet = Packet.Rent(Op.ZC_BATTLEFIELD_NOTIFY_POSITION);
			packet.PutInt(accountId);
			packet.PutString(name, 24);
			packet.PutShort(job);
			packet.PutShort(x);
			packet.PutShort(y);

			foreach (var pc in recipients)
				pc.Connection.Send(packet);
		}

		/// <summary>
		/// Refreshes a BG member's HP bar on the team panel.
		/// rAthena <c>clif_bg_hp</c> / 0x02E0, size 34 (pre-renewal int16 hp).
		/// </summary>
		public static void ZC_BATTLEFIELD_NOTIFY_HP(IEnumerable<PlayerCharacter> recipients, int accountId, string name, short hp, short maxHp)
		{
			using var packet = Packet.Rent(Op.ZC_BATTLEFIELD_NOTIFY_HP);
			packet.PutInt(accountId);
			packet.PutString(name, 24);
			packet.PutShort(hp);
			packet.PutShort(maxHp);

			foreach (var pc in recipients)
				pc.Connection.Send(packet);
		}

		// --- Memorial Dungeon (instance) senders ----------------------------
		// rAthena reference: clif.cpp clif_instance_* family. PACKETVER 23.
		// Sizes are pinned by PacketTable.2500_sakEP12; field layouts below
		// follow the rAthena PACKET_ZC_MEMORIALDUNGEON_* structs at PV23 and
		// pad to the registered total when individual fields are uncertain.

		/// <summary>
		/// Sends the per-instance subscription record (name, owner, expiry,
		/// map). 0x02CB, size 65.
		/// </summary>
		public static void ZC_MEMORIALDUNGEON_SUBSCRIPTION_INFO(PlayerCharacter target, string instanceName, string mapName, int remainingSeconds)
		{
			using var packet = Packet.Rent(Op.ZC_MEMORIALDUNGEON_SUBSCRIPTION_INFO);
			packet.PutString(instanceName, 24);
			packet.PutInt(remainingSeconds);
			packet.PutString(mapName, 24);
			packet.PutEmpty(9); // padding to reach the registered 65-byte total

			target.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies a subscription state change for a memorial dungeon.
		/// 0x02CC, size 4: [u16 op][u16 state].
		/// </summary>
		public static void ZC_MEMORIALDUNGEON_SUBSCRIPTION_NOTIFY(PlayerCharacter target, short state)
		{
			using var packet = Packet.Rent(Op.ZC_MEMORIALDUNGEON_SUBSCRIPTION_NOTIFY);
			packet.PutShort(state);

			target.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the active instance info (name + state + timers). 0x02CD,
		/// size 71.
		/// </summary>
		public static void ZC_MEMORIALDUNGEON_INFO(PlayerCharacter target, string instanceName, int remainingSeconds, int progressSeconds)
		{
			using var packet = Packet.Rent(Op.ZC_MEMORIALDUNGEON_INFO);
			packet.PutString(instanceName, 61);
			packet.PutInt(remainingSeconds);
			packet.PutInt(progressSeconds);

			target.Connection.Send(packet);
		}

		/// <summary>
		/// Notifies progress/state for the active memorial dungeon.
		/// 0x02CE, size 10: [u16 op][u32 state][u32 progress].
		/// </summary>
		public static void ZC_MEMORIALDUNGEON_NOTIFY(PlayerCharacter target, int state, int progress)
		{
			using var packet = Packet.Rent(Op.ZC_MEMORIALDUNGEON_NOTIFY);
			packet.PutInt(state);
			packet.PutInt(progress);

			target.Connection.Send(packet);
		}

		// --- Interrealm / cross-server senders ------------------------------
		// Sabine is single-realm, so these are kept as wire-complete stubs:
		// no caller in Sabine's MailManager invokes them. They exist so a
		// future multi-realm bridge can call them without re-deriving the
		// packet shape.

		/// <summary>
		/// Notifies the client of a clean inter-realm disconnect, letting it
		/// surface a friendly message before the socket closes. 0x02D5,
		/// size 2 — opcode only.
		/// </summary>
		public static void ZC_ISVR_DISCONNECT(ZoneConnection conn)
		{
			using var packet = Packet.Rent(Op.ZC_ISVR_DISCONNECT);
			conn.Send(packet);
		}

		/// <summary>
		/// Acknowledges an inter-realm mail send attempt. Single-realm
		/// installs do not call this — Sabine's local mail uses the
		/// <c>ZC_MAIL_*</c> family. Layout per rAthena PV23: u8 result.
		/// </summary>
		public static void ZC_IRMAIL_SEND_RES(ZoneConnection conn, byte result)
		{
			using var packet = Packet.Rent(Op.ZC_IRMAIL_SEND_RES);
			packet.PutByte(result);
			conn.Send(packet);
		}

		/// <summary>
		/// Notifies the client of a newly arrived inter-realm mail. Unused
		/// by Sabine's single-realm install. Layout per rAthena PV23:
		/// sender name (24) + subject (40).
		/// </summary>
		public static void ZC_IRMAIL_NOTIFY(ZoneConnection conn, string senderName, string subject)
		{
			using var packet = Packet.Rent(Op.ZC_IRMAIL_NOTIFY);
			packet.PutString(senderName, 24);
			packet.PutString(subject, 40);
			conn.Send(packet);
		}
	}
}
