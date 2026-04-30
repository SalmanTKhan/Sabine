using System;
using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Groups
{
	public class GuildManager
	{
		public static GuildManager Instance { get; } = new GuildManager();

		private readonly Dictionary<int, Guild> _guilds = new();
		private readonly Dictionary<string, Guild> _guildsByName = new(StringComparer.OrdinalIgnoreCase);

		public Guild Create(PlayerCharacter master, string name)
		{
			lock (_guilds)
			{
				if (master.Guild != null) return null;
				if (_guildsByName.ContainsKey(name)) return null;

				var guild = new Guild(master, name);
				_guilds[guild.Id] = guild;
				_guildsByName[name] = guild;
				return guild;
			}
		}

		public Guild Get(int guildId)
		{
			lock (_guilds)
			{
				_guilds.TryGetValue(guildId, out var g);
				return g;
			}
		}

		public Guild GetByName(string name)
		{
			lock (_guilds)
			{
				_guildsByName.TryGetValue(name, out var g);
				return g;
			}
		}

		public IReadOnlyList<Guild> All
		{
			get { lock (_guilds) return _guilds.Values.ToList(); }
		}

		public bool Disband(int guildId)
		{
			lock (_guilds)
			{
				if (!_guilds.TryGetValue(guildId, out var guild)) return false;
				_guilds.Remove(guildId);
				_guildsByName.Remove(guild.Name);

				foreach (var m in guild.Members)
					if (m.Player != null) m.Player.Guild = null;

				return true;
			}
		}
	}
}
