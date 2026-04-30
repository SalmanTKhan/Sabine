using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Chats
{
	/// <summary>
	/// Bare-bones global chat channel registry. Channels are addressable
	/// by lowercase name; a few well-known channels are pre-registered.
	/// </summary>
	public class ChatChannelService
	{
		public static ChatChannelService Instance { get; } = new ChatChannelService();

		private readonly ConcurrentDictionary<string, ChatChannel> _channels = new(StringComparer.OrdinalIgnoreCase);

		public ChatChannelService()
		{
			this.GetOrCreate("main");
			this.GetOrCreate("trade");
			this.GetOrCreate("support");
		}

		public ChatChannel GetOrCreate(string name)
			=> _channels.GetOrAdd(name.ToLowerInvariant(), n => new ChatChannel(n));

		public bool TryGet(string name, out ChatChannel channel)
			=> _channels.TryGetValue(name.ToLowerInvariant(), out channel);

		public IEnumerable<ChatChannel> All => _channels.Values;
	}

	public class ChatChannel
	{
		public string Name { get; }
		private readonly HashSet<int> _members = new();

		public ChatChannel(string name) => this.Name = name;

		public bool Join(PlayerCharacter character)
		{
			lock (_members) return _members.Add(character.Id);
		}

		public bool Leave(PlayerCharacter character)
		{
			lock (_members) return _members.Remove(character.Id);
		}

		public bool Contains(PlayerCharacter character)
		{
			lock (_members) return _members.Contains(character.Id);
		}

		public int Count
		{
			get { lock (_members) return _members.Count; }
		}

		public IReadOnlyList<int> Snapshot()
		{
			lock (_members) return _members.ToList();
		}

		public void Broadcast(string text)
		{
			List<int> ids;
			lock (_members) ids = _members.ToList();

			foreach (var id in ids)
			{
				if (ZoneServer.Instance.World.Maps.TryGetPlayerById(id, out var p))
					Send.ZC_NOTIFY_CHAT(p, 0, text);
			}
		}
	}
}
