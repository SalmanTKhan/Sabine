using System.Collections.Generic;
using Sabine.Shared.World;
using Yggdrasil.Configuration;
using Yggdrasil.Logging;

namespace Sabine.Shared.Configuration.Files
{
	/// <summary>
	/// A single advertisable char-server entry sent to the client by the
	/// auth server's login-accept packet. Multi-server installs add extra
	/// entries via the <c>extra_char_servers</c> conf option.
	/// </summary>
	public class CharServerEntry
	{
		public string Name { get; set; }
		public string ServerIp { get; set; }
		public int BindPort { get; set; }
	}

	/// <summary>
	/// Represents char.conf.
	/// </summary>
	public class CharConf : ConfFile
	{
		public string BindIp { get; set; }
		public int BindPort { get; set; }
		public string ServerIp { get; set; }
		public string Name { get; set; }
		public string StartMapStringId { get; set; }
		public Position StartPosition { get; set; }

		/// <summary>
		/// Additional char-server entries advertised to the client alongside
		/// the primary one. Empty by default (single-server install).
		/// </summary>
		public List<CharServerEntry> ExtraServers { get; } = new();

		/// <summary>
		/// Loads the conf file and its options from the given path.
		/// </summary>
		public void Load(string filePath)
		{
			this.Require(filePath);

			this.BindIp = this.GetString("char_bind_ip", "0.0.0.0");
			this.BindPort = this.GetInt("char_bind_port", 7000);
			this.ServerIp = this.GetString("char_server_ip", "127.0.0.1");
			this.Name = this.GetString("char_server_name", "Sabine");

			this.ReadStartLocation("player_start_location", "prt_vilg02,99,81");
			this.ReadExtraServers("extra_char_servers", "");
		}

		/// <summary>
		/// Parses a semicolon-separated list of <c>name|ip|port</c> entries
		/// into <see cref="ExtraServers"/>. Whitespace and empty entries
		/// are ignored.
		/// </summary>
		private void ReadExtraServers(string name, string defaultValue)
		{
			this.ExtraServers.Clear();

			var raw = this.GetString(name, defaultValue);
			if (string.IsNullOrWhiteSpace(raw))
				return;

			foreach (var rawEntry in raw.Split(';'))
			{
				var entry = rawEntry.Trim();
				if (entry.Length == 0)
					continue;

				var parts = entry.Split('|');
				if (parts.Length != 3 || !int.TryParse(parts[2], out var port))
				{
					Log.Error("CharConf.Load: Invalid extra char-server entry '{0}'. Expected 'name|ip|port'.", entry);
					continue;
				}

				this.ExtraServers.Add(new CharServerEntry
				{
					Name = parts[0].Trim(),
					ServerIp = parts[1].Trim(),
					BindPort = port,
				});
			}
		}

		/// <summary>
		/// Reads the given option in the format "map,x,y" and sets the
		/// respective options.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		private void ReadStartLocation(string name, string defaultValue)
		{
			var startLocationStr = this.GetString(name, defaultValue);
			var split = startLocationStr.Split(',');
			if (split.Length != 3)
			{
				Log.Error("CharConf.Load: Invalid start location '{0}'.", startLocationStr);
			}
			else if (!int.TryParse(split[1], out var x) || !int.TryParse(split[2], out var y))
			{
				Log.Error("CharConf.Load: Invalid coordinates in start location '{0}'.", startLocationStr);
			}
			else
			{
				this.StartMapStringId = split[0];
				this.StartPosition = new Position(x, y);
				return;
			}

			// Fallback
			this.StartMapStringId = "prt_vilg02";
			this.StartPosition = new Position(99, 81);
		}
	}
}
