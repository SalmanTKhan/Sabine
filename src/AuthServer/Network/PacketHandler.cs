﻿using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Yggdrasil.Logging;
using Yggdrasil.Security.Hashing;

namespace Sabine.Auth.Network
{
	public class PacketHandler : PacketHandler<AuthConnection>
	{
		[PacketHandler(Op.CA_LOGIN)]
		public void CA_LOGIN(AuthConnection conn, Packet packet)
		{
			var username = packet.GetString(16);
			var password = packet.GetString(16);

			var db = AuthServer.Instance.Database;

			// If account doesn't exist, try to create it
			var account = db.GetAccountByUsername(username);
			if (account == null)
			{
				var creationAllowed = AuthServer.Instance.Conf.Auth.AllowAccountCreation;
				var hasSuffix = (username.EndsWith("_M") || username.EndsWith("_F"));

				if (!creationAllowed || !hasSuffix)
				{
					Send.AC_REFUSE_LOGIN(conn, LoginConnectError.UserNotFound);
					conn.Close(2);
					return;
				}

				var sex = username.EndsWith("_M") ? Sex.Male : Sex.Female;
				username = username.Substring(0, username.Length - 2);

				if (db.UsernameExists(username))
				{
					Send.AC_REFUSE_LOGIN(conn, LoginConnectError.UserNotFound);
					conn.Close(2);
					return;
				}

				var hashedPassword = BCrypt.HashPassword(password, BCrypt.GenerateSalt());
				account = db.CreateAccount(username, hashedPassword, sex, 0);
			}

			var passwordCorrect = BCrypt.CheckPassword(password, account.Password);
			if (!passwordCorrect)
			{
				Send.AC_REFUSE_LOGIN(conn, LoginConnectError.PasswordIncorrect);
				conn.Close(2);
				return;
			}

			db.UpdateSessionId(account);

			Send.AC_ACCEPT_LOGIN(conn, account);

			Log.Debug("User '{0}' logged in.", username);
		}
	}
}
