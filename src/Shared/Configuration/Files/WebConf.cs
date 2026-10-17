using System;
using Yggdrasil.Configuration;

namespace Sabine.Shared.Configuration.Files
{
	/// <summary>
	/// Represents auth.conf.
	/// </summary>
	public class WebConf : ConfFile
	{
		public string BindIp { get; set; }
		public int[] BindPorts { get; set; }
		public string[] PatchFolders { get; set; } = [];
		public bool SmtpEnabled { get; set; }
		public string SmtpHost { get; set; }
		public int SmtpPort { get; set; }
		public string SmtpUsername { get; set; }
		public string SmtpPassword { get; set; }
		public bool SmtpSsl { get; set; }
		public string SmtpFrom { get; set; }
		public string PasswordResetBaseUrl { get; set; }

		/// <summary>
		/// Loads the conf file and its options from the given path.
		/// </summary>
		/// <param name="filePath"></param>
		public void Load(string filePath)
		{
			this.Require(filePath);

			this.BindIp = this.GetString("web_bind_ip", "0.0.0.0");
			this.BindPorts = this.GetBindPorts("web_bind_ports", [5614]);
			this.PatchFolders = this.GetString("patch_folders", "user/patches").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			this.SmtpEnabled = this.GetBool("smtp_enabled", false);
			this.SmtpHost = this.GetString("smtp_host", "");
			this.SmtpPort = this.GetInt("smtp_port", 25);
			this.SmtpUsername = this.GetString("smtp_username", "");
			this.SmtpPassword = this.GetString("smtp_password", "");
			this.SmtpSsl = this.GetBool("smtp_ssl", false);
			this.SmtpFrom = this.GetString("smtp_from", "noreply@localhost");
			this.PasswordResetBaseUrl = this.GetString("password_reset_base_url", "http://127.0.0.1:5614/password-reset");
		}

		private int[] GetBindPorts(string option, int[] defaultValue)
		{
			var bindPorts = this.GetString(option, null);
			if (bindPorts == null)
				return defaultValue;

			var split = bindPorts.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (split.Length == 0)
				return defaultValue;

			var result = new int[split.Length];

			for (var i = 0; i < split.Length; ++i)
			{
				var portStr = split[i];

				if (!int.TryParse(portStr, out var port))
					throw new FormatException($"Invalid port: {portStr}");

				result[i] = port;
			}

			return result;
		}
	}
}
