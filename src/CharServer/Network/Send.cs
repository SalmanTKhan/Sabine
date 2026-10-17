using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using Sabine.Char.Database;
using Sabine.Char.Network.Helpers;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Network;

namespace Sabine.Char.Network
{
	/// <summary>
	/// Packet senders.
	/// </summary>
	public static class Send
	{
		/// <summary>
		/// Sends data necessary to initialize the connection on newer
		/// clients.
		/// </summary>
		/// <param name="conn"></param>
		public static void InitConnection(CharConnection conn)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(sizeof(int));
			BinaryPrimitives.WriteInt32LittleEndian(buffer, conn.Account.Id);

			conn.Send(buffer, sizeof(int), static (data, len, type) => ArrayPool<byte>.Shared.Return(data));
		}

		/// <summary>
		/// Shows error messages about why the login request was refused.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="errorCode"></param>
		public static void HC_REFUSE_ENTER(CharConnection conn, CharConnectError errorCode)
		{
			using var packet = Packet.Rent(Op.HC_REFUSE_ENTER);
			packet.PutByte((byte)errorCode);

			conn.Send(packet);
		}

		/// <summary>
		/// Accepts login request and makes client transition to the
		/// character selection.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="characters"></param>
		public static void HC_ACCEPT_ENTER(CharConnection conn, IEnumerable<Character> characters)
		{
			using var packet = Packet.Rent(Op.HC_ACCEPT_ENTER);

			if (Game.IsNostalRo)
			{
				// PACKET_HC_ACCEPT_ENTER / neo-union at 20111102.
				packet.PutByte(9);
				packet.PutByte(0);
				packet.PutByte(0);
				packet.PutByte(0);
				packet.PutInt(0);
				packet.PutInt(0);
				packet.PutInt(0);
				packet.PutEmpty(7);
			}
			else if (Game.IsSabine)
			{
				// rA PACKET_HC_ACCEPT_ENTER at PACKETVER >= 20100413
				// (packets.hpp:240, char_clif.cpp chclif_mmo_send0819).
				// Header carries slot-config triple (total/premium_start/
				// premium_end) followed by 20-byte extension padding.
				// Per-char body keeps the EP12/PV23 108-byte CHARACTER_INFO
				// layout for now — the modern rA 155-175 byte widening
				// is deferred. The open-midgard client must match by
				// parsing 108-byte char entries after this 23-byte header.
				packet.PutByte(9);    // total (char slots)
				packet.PutByte(0);    // premium_start
				packet.PutByte(0);    // premium_end
				packet.PutEmpty(20);  // extension
			}
			else if (Game.Version >= Versions.S600 && Game.Version < Versions.S2500)
			{
				// The client calculates the length of the character
				// portion for this version as '(len - 4) / 106', and a
				// remainder/offset of '(len - 4) % 106 + 2' for where the
				// characters start. Due to the +2, we need at least 2
				// extra bytes here, with unknown purpose.
				packet.PutShort(0);
			}
			else if (!Game.IsNostalRo && Game.Version >= Versions.S2500)
			{
				// Huge upgrade! Instead of 2 pointless bytes we have 20
				// now =) Oh, and characters need 108 bytes instead of 106.
				packet.PutEmpty(20);
			}

			foreach (var character in characters)
				packet.AddCharacter(character);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends zone server connection information to the client,
		/// making it connect to it.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="characterId"></param>
		/// <param name="mapStringId"></param>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		public static void HC_NOTIFY_ZONESVR(CharConnection conn, int characterId, string mapStringId, string ip, int port)
		{
			var mapFileName = mapStringId + ".gat";

			using var packet = Packet.Rent(Op.HC_NOTIFY_ZONESVR);

			packet.PutInt(characterId);
			packet.PutString(mapFileName, 16);
			packet.PutInt(IPAddress.Parse(ip));
			packet.PutShort((short)port);

			if (Game.IsSabine)
			{
				// rA PACKET_HC_NOTIFY_ZONESVR at PACKETVER >= 20170315
				// (packets.hpp:291, char_clif.cpp chclif_send_map_data).
				// Trailing 128-byte domain field — empty when zone routing
				// uses IP only, non-empty when the server publishes a
				// hostname. Sabine leaves it empty.
				packet.PutString("", 128);
			}

			conn.Send(packet);
		}

		/// <summary>
		/// Shows error message about why the character creation request
		/// was refused.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="error"></param>
		public static void HC_REFUSE_MAKECHAR(CharConnection conn, CharCreateError error)
		{
			using var packet = Packet.Rent(Op.HC_REFUSE_MAKECHAR);
			packet.PutByte((byte)error);

			conn.Send(packet);
		}

		/// <summary>
		/// Accepts character creation request, adding the character to
		/// the selectable ones.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="character"></param>
		public static void HC_ACCEPT_MAKECHAR(CharConnection conn, Character character)
		{
			using var packet = Packet.Rent(Op.HC_ACCEPT_MAKECHAR);
			packet.AddCharacter(character);

			conn.Send(packet);
		}

		/// <summary>
		/// Accepts character deletion request, removing it from the
		/// selectable characters on the client.
		/// </summary>
		/// <param name="conn"></param>
		public static void HC_ACCEPT_DELETECHAR(CharConnection conn)
		{
			using var packet = Packet.Rent(Op.HC_ACCEPT_DELETECHAR);
			conn.Send(packet);
		}

		/// <summary>
		/// Shows an error message for why a character deletion request
		/// was refused.
		/// </summary>
		/// <param name="conn"></param>
		public static void HC_REFUSE_DELETECHAR(CharConnection conn)
		{
			using var packet = Packet.Rent(Op.HC_REFUSE_DELETECHAR);
			packet.PutByte(0); // Doesn't seem to do anything.

			conn.Send(packet);
		}

		/// <summary>
		/// Notifies the client that the character selection request was
		/// refused, showing a reason popup.
		/// </summary>
		/// <remarks>
		/// rAthena: <c>clif_authrefuse</c> family / 0x02CA at PACKETVER 23.
		/// </remarks>
		/// <param name="conn"></param>
		/// <param name="reason">
		/// 0 = "Server is closed", 1 = "Server is overpopulated",
		/// 2 = "Login info incorrect", 3 = "Account expired".
		/// </param>
		public static void HC_REFUSE_SELECTCHAR(CharConnection conn, byte reason)
		{
			using var packet = Packet.Rent(Op.HC_REFUSE_SELECTCHAR);
			packet.PutByte(reason);

			conn.Send(packet);
		}

		/// <summary>
		/// Notifies the client that no character was actually selected,
		/// returning the UI to the character list.
		/// </summary>
		/// <remarks>
		/// rAthena: 0x028B, variable-length empty body.
		/// </remarks>
		/// <param name="conn"></param>
		public static void HC_CHARNOTBEENSELECTED(CharConnection conn)
		{
			using var packet = Packet.Rent(Op.HC_CHARNOTBEENSELECTED);
			conn.Send(packet);
		}
	}
}
