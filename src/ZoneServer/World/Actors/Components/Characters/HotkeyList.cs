using System.Globalization;
using System.Linq;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// One hotkey bar slot. Mirrors rAthena's
	/// <c>map_session_data.status.hotkeys[i]</c> shape — type (0=item,
	/// 1=skill), id (item or skill id), and lv (skill level or item
	/// stack count, depending on type).
	/// </summary>
	public struct HotkeyEntry
	{
		public byte IsSkill;
		public int Id;
		public short Count;

		public bool IsEmpty => this.Id == 0;
	}

	/// <summary>
	/// Tracks a player's 27-slot hotkey bar. Persistence rides
	/// character vars (same approach as <see cref="QuestList"/>) so
	/// no schema migration is required; promote to SQL when
	/// consumers warrant.
	/// </summary>
	public class HotkeyList
	{
		/// <summary>
		/// Number of slots in the S2500 hotkey packet
		/// (<c>ZC_SHORTCUT_KEY_LIST</c>, 0x02B9/191). Mirrors
		/// rAthena's <c>MAX_HOTKEYS_PACKET</c> at this era.
		/// </summary>
		public const int SlotCount = 27;

		private const string ListKey = "__hotkeys";

		private readonly HotkeyEntry[] _entries = new HotkeyEntry[SlotCount];
		private bool _loaded;

		public PlayerCharacter Character { get; }

		public HotkeyList(PlayerCharacter character)
		{
			this.Character = character;
		}

		private void EnsureLoaded()
		{
			if (_loaded) return;
			this.LoadAll();
			_loaded = true;
		}

		/// <summary>Returns the entry for the given slot, or an empty entry when out-of-range.</summary>
		public HotkeyEntry Get(int slot)
		{
			this.EnsureLoaded();
			if (slot < 0 || slot >= SlotCount) return default;
			return _entries[slot];
		}

		/// <summary>Replaces the entry for the given slot and persists.</summary>
		public void Set(int slot, HotkeyEntry entry)
		{
			this.EnsureLoaded();
			if (slot < 0 || slot >= SlotCount) return;
			_entries[slot] = entry;
			this.SaveAll();
		}

		/// <summary>Returns the full 27-slot view. Read-only snapshot is fine — the array shape never changes.</summary>
		public HotkeyEntry[] GetAll()
		{
			this.EnsureLoaded();
			var copy = new HotkeyEntry[SlotCount];
			System.Array.Copy(_entries, copy, SlotCount);
			return copy;
		}

		private void LoadAll()
		{
			for (var i = 0; i < SlotCount; i++)
				_entries[i] = default;

			var packed = this.Character.Vars.Perm.GetString(ListKey, "");
			if (string.IsNullOrEmpty(packed)) return;

			foreach (var part in packed.Split('|'))
			{
				if (string.IsNullOrEmpty(part)) continue;
				var fields = part.Split(',');
				if (fields.Length < 4) continue;
				if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var slot)) continue;
				if (slot < 0 || slot >= SlotCount) continue;
				byte.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var isSkill);
				int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id);
				short.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count);

				_entries[slot] = new HotkeyEntry { IsSkill = isSkill, Id = id, Count = count };
			}
		}

		private void SaveAll()
		{
			var occupied = _entries
				.Select((e, i) => (Entry: e, Slot: i))
				.Where(t => !t.Entry.IsEmpty)
				.Select(t => string.Join(",",
					t.Slot.ToString(CultureInfo.InvariantCulture),
					t.Entry.IsSkill.ToString(CultureInfo.InvariantCulture),
					t.Entry.Id.ToString(CultureInfo.InvariantCulture),
					t.Entry.Count.ToString(CultureInfo.InvariantCulture)));

			this.Character.Vars.Perm.Set(ListKey, string.Join("|", occupied));
		}
	}
}
