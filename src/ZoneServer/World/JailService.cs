using System;
using System.Collections.Concurrent;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World
{
	/// <summary>
	/// Tracks which account ids are jailed and where they are confined.
	/// On warp/login the player is forced to the jail location until the
	/// release time elapses.
	/// </summary>
	public class JailService
	{
		public static JailService Instance { get; } = new JailService();

		private readonly ConcurrentDictionary<int, JailEntry> _entries = new();

		/// <summary>
		/// Map StringId used when no other jail destination is given.
		/// </summary>
		public string DefaultJailMap { get; set; } = "prt_jail";

		public bool IsJailed(int accountId, out JailEntry entry)
		{
			if (!_entries.TryGetValue(accountId, out entry))
				return false;

			if (entry.ReleaseAt <= DateTime.UtcNow)
			{
				_entries.TryRemove(accountId, out _);
				return false;
			}

			return true;
		}

		public bool IsJailed(PlayerCharacter character, out JailEntry entry)
			=> this.IsJailed(character.Connection.Account.Id, out entry);

		public void Jail(int accountId, int mapId, Position position, TimeSpan duration)
		{
			_entries[accountId] = new JailEntry
			{
				AccountId = accountId,
				MapId = mapId,
				Position = position,
				ReleaseAt = DateTime.UtcNow + duration,
			};
		}

		public bool Release(int accountId)
			=> _entries.TryRemove(accountId, out _);

		public TimeSpan? RemainingTime(int accountId)
		{
			if (!_entries.TryGetValue(accountId, out var entry))
				return null;

			var remaining = entry.ReleaseAt - DateTime.UtcNow;
			return remaining > TimeSpan.Zero ? remaining : null;
		}
	}

	public class JailEntry
	{
		public int AccountId { get; set; }
		public int MapId { get; set; }
		public Position Position { get; set; }
		public DateTime ReleaseAt { get; set; }
	}
}
