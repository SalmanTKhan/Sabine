using System;
using MySqlConnector;
using Sabine.Shared.Const;
using Sabine.Shared.Database;
using Yggdrasil.Db.MySql;
using Yggdrasil.Db.MySql.SimpleCommands;
using Yggdrasil.Extensions;

namespace Sabine.Web.Database
{
	public class WebDb : Db
	{
		public bool EmailExists(string email)
		{
			using (var conn = this.GetConnection())
			using (var cmd = new MySqlCommand("SELECT 1 FROM `accounts` WHERE `email` = @email LIMIT 1", conn))
			{
				cmd.AddParameter("@email", email);
				using (var reader = cmd.ExecuteReader())
					return reader.Read();
			}
		}

		public Account CreateAccount(string username, string password, string email, Sex sex)
		{
			var account = new Account
			{
				Username = username,
				Password = password,
				Email = email,
				Sex = sex,
				Authority = 0,
				SessionId = 0,
			};

			using (var conn = this.GetConnection())
			using (var cmd = new InsertCommand("INSERT INTO `accounts` {parameters}", conn))
			{
				cmd.Set("username", account.Username);
				cmd.Set("password", account.Password);
				cmd.Set("email", account.Email);
				cmd.Set("sex", account.Sex);
				cmd.Set("authority", account.Authority);
				cmd.Set("sessionId", account.SessionId);
				cmd.Execute();
				account.Id = (int)cmd.LastId;
			}

			return account;
		}

		public Account GetAccountByEmail(string email)
		{
			using (var conn = this.GetConnection())
			using (var cmd = new MySqlCommand("SELECT * FROM `accounts` WHERE `email` = @email LIMIT 1", conn))
			{
				cmd.AddParameter("@email", email);
				using (var reader = cmd.ExecuteReader())
				{
					if (!reader.Read())
						return null;

					var account = new Account();
					account.Id = reader.GetInt32("accountId");
					account.Username = reader.GetStringSafe("username");
					account.Password = reader.GetStringSafe("password");
					account.Email = reader.GetStringSafe("email") ?? "";
					account.Sex = (Sex)reader.GetByte("sex");
					account.Authority = reader.GetByte("authority");
					account.SessionId = reader.GetInt32("sessionId");
					return account;
				}
			}
		}

		public void CreatePasswordResetToken(int accountId, string tokenHash, DateTime expiresAt)
		{
			using (var conn = this.GetConnection())
			using (var cmd = new InsertCommand("INSERT INTO `password_reset_tokens` {parameters}", conn))
			{
				cmd.Set("accountId", accountId);
				cmd.Set("tokenHash", tokenHash);
				cmd.Set("expiresAt", expiresAt);
				cmd.Execute();
			}
		}

		public PasswordResetToken GetPasswordResetToken(string tokenHash)
		{
			using (var conn = this.GetConnection())
			using (var cmd = new MySqlCommand("SELECT * FROM `password_reset_tokens` WHERE `tokenHash` = @tokenHash LIMIT 1", conn))
			{
				cmd.AddParameter("@tokenHash", tokenHash);
				using (var reader = cmd.ExecuteReader())
				{
					if (!reader.Read())
						return null;

					return new PasswordResetToken
					{
						Id = reader.GetInt32("tokenId"),
						AccountId = reader.GetInt32("accountId"),
						TokenHash = reader.GetStringSafe("tokenHash"),
						ExpiresAt = reader.GetDateTime("expiresAt"),
						UsedAt = reader.IsDBNull(reader.GetOrdinal("usedAt")) ? null : reader.GetDateTime("usedAt"),
					};
				}
			}
		}

		public void ConfirmPasswordReset(int tokenId, int accountId, string passwordHash)
		{
			using (var conn = this.GetConnection())
			using (var transaction = conn.BeginTransaction())
			{
				using (var cmd = new UpdateCommand("UPDATE `accounts` SET {parameters} WHERE `accountId` = @accountId", conn, transaction))
				{
					cmd.AddParameter("@accountId", accountId);
					cmd.Set("password", passwordHash);
					cmd.Execute();
				}

				using (var cmd = new UpdateCommand("UPDATE `password_reset_tokens` SET {parameters} WHERE `tokenId` = @tokenId", conn, transaction))
				{
					cmd.AddParameter("@tokenId", tokenId);
					cmd.Set("usedAt", DateTime.UtcNow);
					cmd.Execute();
				}

				transaction.Commit();
			}
		}
	}

	public sealed class PasswordResetToken
	{
		public int Id { get; set; }
		public int AccountId { get; set; }
		public string TokenHash { get; set; }
		public DateTime ExpiresAt { get; set; }
		public DateTime? UsedAt { get; set; }
	}
}
