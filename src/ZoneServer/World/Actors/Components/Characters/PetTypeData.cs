using Sabine.Shared.Data.Databases;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// Zone-side facade over the shared <see cref="PetDb"/>. Lookup by
	/// mob id is a single dictionary hit; egg-item-id and lure-item-id
	/// reverse-lookups iterate the table (the catalog is small enough
	/// that an inverted index isn't worth the maintenance burden).
	///
	/// The legacy hard-coded entries that lived here moved to
	/// <c>system/data/pets.txt</c> alongside the rest of Sabine's data
	/// files. The schema is unchanged — see <see cref="PetData"/>.
	/// </summary>
	public static class PetTypeData
	{
		public static PetData GetByMobId(int mobId)
			=> Sabine.Shared.Game.CurrentData?.Pets?.Entries.TryGetValue(mobId, out var e) == true ? e : null;

		public static PetData GetByEggItemId(int eggItemId)
		{
			var pets = Sabine.Shared.Game.CurrentData?.Pets;
			if (pets == null) return null;
			foreach (var entry in pets.Entries.Values)
				if (entry.EggItemId == eggItemId) return entry;
			return null;
		}

		public static PetData GetByLureItemId(int lureItemId)
		{
			var pets = Sabine.Shared.Game.CurrentData?.Pets;
			if (pets == null) return null;
			foreach (var entry in pets.Entries.Values)
				if (entry.LureItemId == lureItemId) return entry;
			return null;
		}
	}
}
