using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EmbedIO;
using Sabine.Shared.Configuration.Files;
using Sabine.Shared.Const;
using Sabine.Web.Database;
using Yggdrasil.Security.Hashing;

namespace Sabine.Web.Api
{
	public sealed class AccountApi
	{
		private static readonly Regex UsernamePattern = new("^[A-Za-z0-9_]{4,24}$", RegexOptions.Compiled);
		private readonly WebDb _db;
		private readonly WebConf _conf;

		public AccountApi(WebDb db, WebConf conf)
		{
			_db = db;
			_conf = conf;
		}

		public async Task CheckUsername(IHttpContext context)
		{
			var username = context.Request.QueryString["username"] ?? "";
			await SendJson(context, HttpStatusCode.OK, new { available = IsValidUsername(username) && !_db.UsernameExists(username) });
		}

		public async Task Create(IHttpContext context)
		{
			var request = await ReadJson<CreateAccountRequest>(context);
			var error = ValidateAccountRequest(request);
			if (error != null)
			{
				await SendJson(context, HttpStatusCode.BadRequest, new { error });
				return;
			}

			var username = request.Username.Trim();
			var email = request.Email.Trim();
			if (_db.UsernameExists(username))
			{
				await SendJson(context, HttpStatusCode.Conflict, new { error = "username_taken" });
				return;
			}

			if (_db.EmailExists(email))
			{
				await SendJson(context, HttpStatusCode.Conflict, new { error = "email_taken" });
				return;
			}

			var sex = string.Equals(request.Sex, "Female", StringComparison.OrdinalIgnoreCase) || string.Equals(request.Sex, "F", StringComparison.OrdinalIgnoreCase)
				? Sex.Female
				: Sex.Male;
			var passwordHash = HashLoginPassword(request.Password1);
			var account = _db.CreateAccount(username, passwordHash, email, sex);
			await SendJson(context, HttpStatusCode.OK, new { accountId = account.Id, username = account.Username });
		}

		public async Task RequestPasswordReset(IHttpContext context)
		{
			var request = await ReadJson<PasswordResetRequest>(context);
			var email = request.Email?.Trim() ?? "";
			var account = string.IsNullOrWhiteSpace(email) ? null : _db.GetAccountByEmail(email);
			if (account == null)
			{
				await SendJson(context, HttpStatusCode.OK, new { sent = false });
				return;
			}

			var token = CreateToken();
			_db.CreatePasswordResetToken(account.Id, HashToken(token), DateTime.UtcNow.AddHours(1));

			if (_conf.SmtpEnabled)
			{
				SendResetEmail(account.Email, token);
				await SendJson(context, HttpStatusCode.OK, new { sent = true });
			}
			else
			{
				await SendJson(context, HttpStatusCode.OK, new { sent = false, token });
			}
		}

		public async Task ValidatePasswordReset(IHttpContext context)
		{
			var request = await ReadJson<TokenRequest>(context);
			await SendJson(context, HttpStatusCode.OK, new { valid = IsUsableToken(request.Token) });
		}

		public async Task ConfirmPasswordReset(IHttpContext context)
		{
			var request = await ReadJson<PasswordResetConfirmRequest>(context);
			if (!IsStrongPassword(request.Password1))
			{
				await SendJson(context, HttpStatusCode.BadRequest, new { error = "invalid_password" });
				return;
			}

			if (request.Password1 != request.Password2)
			{
				await SendJson(context, HttpStatusCode.BadRequest, new { error = "password_mismatch" });
				return;
			}

			var token = GetUsableToken(request.Token);
			if (token == null)
			{
				await SendJson(context, HttpStatusCode.BadRequest, new { error = "invalid_token" });
				return;
			}

			_db.ConfirmPasswordReset(token.Id, token.AccountId, HashLoginPassword(request.Password1));
			await SendJson(context, HttpStatusCode.OK, new { updated = true });
		}

		private string ValidateAccountRequest(CreateAccountRequest request)
		{
			if (!IsValidUsername(request.Username))
				return "invalid_username";
			if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
				return "invalid_email";
			if (!IsStrongPassword(request.Password1))
				return "invalid_password";
			if (request.Password1 != request.Password2)
				return "password_mismatch";
			return null;
		}

		private bool IsUsableToken(string token) => GetUsableToken(token) != null;

		private PasswordResetToken GetUsableToken(string token)
		{
			if (string.IsNullOrWhiteSpace(token))
				return null;

			var row = _db.GetPasswordResetToken(HashToken(token));
			if (row == null || row.UsedAt != null || row.ExpiresAt <= DateTime.UtcNow)
				return null;

			return row;
		}

		private void SendResetEmail(string email, string token)
		{
			using var message = new MailMessage(_conf.SmtpFrom, email);
			message.Subject = "HookCatRO password reset";
			message.Body = $"{_conf.PasswordResetBaseUrl}?token={Uri.EscapeDataString(token)}";

			using var client = new SmtpClient(_conf.SmtpHost, _conf.SmtpPort)
			{
				EnableSsl = _conf.SmtpSsl,
			};

			if (!string.IsNullOrWhiteSpace(_conf.SmtpUsername))
				client.Credentials = new NetworkCredential(_conf.SmtpUsername, _conf.SmtpPassword);

			client.Send(message);
		}

		private static string HashLoginPassword(string password)
		{
			return BCrypt.HashPassword(Yggdrasil.Security.Hashing.MD5.Encode(password), BCrypt.GenerateSalt());
		}

		private static string CreateToken()
		{
			return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
		}

		private static string HashToken(string token)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(token.Trim());
			return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
		}

		private static bool IsValidUsername(string username)
		{
			return !string.IsNullOrWhiteSpace(username) && UsernamePattern.IsMatch(username);
		}

		private static bool IsStrongPassword(string password)
		{
			return !string.IsNullOrEmpty(password) && password.Length >= 6 && password.Length <= 64;
		}

		private static async Task<T> ReadJson<T>(IHttpContext context) where T : new()
		{
			using var reader = new StreamReader(context.Request.InputStream);
			var json = await reader.ReadToEndAsync();
			return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new T();
		}

		private static async Task SendJson(IHttpContext context, HttpStatusCode statusCode, object data)
		{
			context.Response.StatusCode = (int)statusCode;
			context.Response.ContentType = "application/json";
			await context.SendStringAsync(JsonSerializer.Serialize(data), "application/json", System.Text.Encoding.UTF8);
		}
	}

	public sealed class CreateAccountRequest
	{
		public string Username { get; set; } = "";
		public string Password1 { get; set; } = "";
		public string Password2 { get; set; } = "";
		public string Email { get; set; } = "";
		public string Sex { get; set; } = "Male";
	}

	public sealed class PasswordResetRequest
	{
		public string Email { get; set; } = "";
	}

	public sealed class TokenRequest
	{
		public string Token { get; set; } = "";
	}

	public sealed class PasswordResetConfirmRequest
	{
		public string Token { get; set; } = "";
		public string Password1 { get; set; } = "";
		public string Password2 { get; set; } = "";
	}
}
