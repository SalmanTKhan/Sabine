using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// One friend entry. Mirrors rAthena's <c>s_friend</c> shape:
	/// account id, char id, and the friend's last-known display name.
	/// </summary>
	public struct FriendEntry
	{
		public int AccountId;
		public int CharId;
		public string Name;

		public bool IsEmpty => this.CharId == 0;
	}

	/// <summary>
	/// Tracks a player's friends. Persistence rides character vars
	/// (same approach as <see cref="HotkeyList"/>) so no schema
	/// migration is needed; promote to a dedicated SQL table if
	/// friend graphs ever grow beyond ~50 entries per player.
	/// </summary>
	public class FriendsList
	{
		/// <summary>
		/// rAthena's <c>MAX_FRIENDS</c>. Matches the client's add-popup
		/// "Friend List is full." reject path; the client never asks
		/// for more than 40 entries in the initial-dump packet.
		/// </summary>
		public const int MaxFriends = 40;

		private const string ListKey = "__friends";
		private const char EntrySep = '|';
		private const char FieldSep = ',';

		private readonly List<FriendEntry> _entries = new();
		private bool _loaded;
		private readonly object _syncLock = new();

		public PlayerCharacter Character { get; }

		public FriendsList(PlayerCharacter character)
		{
			this.Character = character;
		}

		private void EnsureLoaded()
		{
			if (_loaded) return;
			lock (_syncLock)
			{
				if (_loaded) return;
				this.LoadAll();
				_loaded = true;
			}
		}

		/// <summary>Snapshot copy of all entries. Caller is free to mutate the returned list.</summary>
		public List<FriendEntry> GetAll()
		{
			this.EnsureLoaded();
			lock (_syncLock)
				return new List<FriendEntry>(_entries);
		}

		/// <summary>Total friend count.</summary>
		public int Count
		{
			get
			{
				this.EnsureLoaded();
				lock (_syncLock)
					return _entries.Count;
			}
		}

		/// <summary>True when the player's list is at capacity.</summary>
		public bool IsFull => this.Count >= MaxFriends;

		/// <summary>True when the given char id is in the list.</summary>
		public bool Contains(int charId)
		{
			this.EnsureLoaded();
			lock (_syncLock)
				return _entries.Any(e => e.CharId == charId);
		}

		/// <summary>
		/// Adds a friend entry. Returns true when the entry was added,
		/// false when the list is full or the friend is already present.
		/// </summary>
		public bool Add(FriendEntry entry)
		{
			if (entry.CharId == 0) return false;

			this.EnsureLoaded();
			lock (_syncLock)
			{
				if (_entries.Any(e => e.CharId == entry.CharId))
					return false;
				if (_entries.Count >= MaxFriends)
					return false;
				_entries.Add(entry);
				this.SaveAll();
				return true;
			}
		}

		/// <summary>
		/// Removes the friend with the given char id, returning true if
		/// the entry existed.
		/// </summary>
		public bool Remove(int charId)
		{
			this.EnsureLoaded();
			lock (_syncLock)
			{
				var idx = _entries.FindIndex(e => e.CharId == charId);
				if (idx < 0) return false;
				_entries.RemoveAt(idx);
				this.SaveAll();
				return true;
			}
		}

		private void LoadAll()
		{
			_entries.Clear();

			var packed = this.Character.Vars.Perm.GetString(ListKey, "");
			if (string.IsNullOrEmpty(packed)) return;

			foreach (var part in packed.Split(EntrySep))
			{
				if (string.IsNullOrEmpty(part)) continue;
				var fields = part.Split(FieldSep);
				if (fields.Length < 3) continue;
				if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var aid)) continue;
				if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var cid)) continue;
				if (cid == 0) continue;

				// Names may contain ',' or '|' literals; we url-encode the
				// reserved delimiters on save and decode here. ASCII-only
				// after the encode, so no Unicode pitfalls.
				var name = fields[2].Replace("%2C", ",").Replace("%7C", "|").Replace("%25", "%");
				_entries.Add(new FriendEntry { AccountId = aid, CharId = cid, Name = name });
			}
		}

		private void SaveAll()
		{
			var packed = string.Join(EntrySep.ToString(), _entries.Select(static e =>
			{
				var safeName = (e.Name ?? string.Empty)
					.Replace("%", "%25").Replace(",", "%2C").Replace("|", "%7C");
				return string.Join(FieldSep.ToString(),
					e.AccountId.ToString(CultureInfo.InvariantCulture),
					e.CharId.ToString(CultureInfo.InvariantCulture),
					safeName);
			}));

			this.Character.Vars.Perm.Set(ListKey, packed);
		}
	}
}
