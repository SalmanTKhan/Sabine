using System.Net;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Database;
using Sabine.Shared.Network;

namespace Sabine.Auth.Network
{
	/// <summary>
	/// Packet senders.
	/// </summary>
	public static class Send
	{
		/// <summary>
		/// Shows error message on the client about why the login request
		/// was refused.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="errorCode"></param>
		/// <param name="errorMessage">Error message to display for certain errors (not support by all versions).</param>
		public static void AC_REFUSE_LOGIN(AuthConnection conn, LoginConnectError errorCode, string errorMessage = null)
		{
			using var packet = Packet.Rent(Op.AC_REFUSE_LOGIN);

			// rA's PACKET_AC_REFUSE_LOGIN at PACKETVER >= 20120000 widens
			// the error code from u8 to u32 (loginclif.cpp logclif_auth_failed,
			// packets.hpp:225). Sabine profile follows that contract.
			if (Game.IsSabine)
			{
				packet.PutInt((int)errorCode);
				packet.PutString(errorMessage, 20);
			}
			else
			{
				packet.PutByte((byte)errorCode);

				if (Game.Version >= Versions.Beta2)
					packet.PutString(errorMessage, 20);
			}

			conn.Send(packet);
		}

		/// <summary>
		/// Accepts login request, making the client display a list of
		/// available char servers.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="account"></param>
		public static void AC_ACCEPT_LOGIN(AuthConnection conn, Account account)
		{
			using var packet = Packet.Rent(Op.AC_ACCEPT_LOGIN);

			if (Game.IsSabine)
			{
				// rA's PACKET_AC_ACCEPT_LOGIN at PACKETVER >= 20170315
				// (packets.hpp:186, loginclif.cpp logclif_auth_ok).
				// Header: login_id1, AID, login_id2, last_ip, last_login[26],
				// sex, web_auth_token[17]. Per-server: ip, port, name[20],
				// users, type, new_, unknown[128].
				packet.PutInt(account.SessionId);          // login_id1
				packet.PutInt(account.Id);                 // AID
				packet.PutInt(account.SessionId + 1);      // login_id2
				packet.PutInt(0);                          // last_ip (unused)
				packet.PutString("", 26);                  // last_login (unused)
				packet.PutByte((byte)account.Sex);         // sex
				packet.PutString("", 17);                  // web_auth_token (unused)
			}
			else if (Game.Version < Versions.Beta1)
			{
				packet.PutInt(account.SessionId);
				packet.PutByte((byte)account.Sex);
				packet.PutInt(account.Id); // This becomes the player character's handle
			}
			else
			{
				packet.PutInt(account.SessionId);
				packet.PutInt(account.Id); // This becomes the player character's handle
				packet.PutInt(account.SessionId + 1);

				if (Game.Version >= Versions.Beta2)
				{
					packet.PutInt(0); // IP?
					packet.PutString("", 26); // Last Server?
				}

				packet.PutByte((byte)account.Sex);
			}

			var charConf = AuthServer.Instance.Conf.Char;

			// Primary char server is always advertised; ExtraServers is empty
			// by default so single-server installs are unchanged. Multi-server
			// installs populate it via the 'extra_char_servers' conf option.
			WriteCharServerEntry(packet, charConf.ServerIp, charConf.BindPort, charConf.Name);
			foreach (var extra in charConf.ExtraServers)
				WriteCharServerEntry(packet, extra.ServerIp, extra.BindPort, extra.Name);

			conn.Send(packet);
		}

		/// <summary>
		/// Appends one char-server descriptor to the AC_ACCEPT_LOGIN body.
		/// Layout matches the per-server sub-record consumed by every era
		/// from Beta1 onward; Beta2+ adds the 6 trailing user/type/new
		/// bytes, Sabine/PV23 adds the 128-byte unknown padding.
		/// </summary>
		private static void WriteCharServerEntry(Packet packet, string ip, int port, string name)
		{
			packet.PutInt(IPAddress.Parse(ip));
			packet.PutShort((short)port);
			packet.PutString(name + " ", 20);

			if (Game.Version >= Versions.Beta2)
			{
				packet.PutShort(0); // PlayerCount / users
				packet.PutShort(0); // IsMaintenance / type
				packet.PutShort(0); // IsNew / new_
			}

			if (Game.IsSabine)
				packet.PutEmpty(128); // PACKET_AC_ACCEPT_LOGIN_sub.unknown
		}

		/// <summary>
		/// Tells the client whether a second password (PIN) prompt is
		/// required. Sabine has no PIN storage, so flag=0 is sent to
		/// short-circuit the client-side PIN dialog on PV23 builds that
		/// require this packet before transitioning to char select.
		/// </summary>
		/// <remarks>
		/// rAthena: <c>logclif_pincode_*</c> (loginclif.cpp). Opcode 0x02AD,
		/// size 8: [u16 op][u16 flag][u32 seed]. flag=0 means "PIN ok /
		/// not required".
		/// </remarks>
		/// <param name="conn"></param>
		/// <param name="accountId">Account id (echoed by client; unused server-side).</param>
		/// <param name="seed">PIN obfuscation seed (irrelevant when flag=0).</param>
		public static void AC_REQUEST_SECOND_PASSWORD(AuthConnection conn, int accountId, int seed)
		{
			using var packet = Packet.Rent(Op.AC_REQUEST_SECOND_PASSWORD);
			packet.PutShort(0);     // flag (0 = PIN ok, no prompt)
			packet.PutInt(seed);    // PIN obfuscation seed

			conn.Send(packet);
		}

		/// <summary>
		/// Sends a salt to hash passwords with to the client.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="salt"></param>
		public static void AC_ACK_HASH(AuthConnection conn, string salt)
		{
			using var packet = Packet.Rent(Op.AC_ACK_HASH);

			// Make sure to not null-terminate the string, as the client
			// uses any potential null-terminators as part of the hash,
			// which can lead to unexpected hashing results. Especially
			// if you want to send an empty salt. "\0" != "".
			packet.PutString(salt, false);

			conn.Send(packet);
		}
	}
}
