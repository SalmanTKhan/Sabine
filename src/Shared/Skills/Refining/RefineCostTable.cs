using Sabine.Shared.Data.Databases;

namespace Sabine.Shared.Skills.Refining
{
	/// <summary>
	/// Per-attempt material and zeny costs for a single refine
	/// attempt. Mirrors eAthena classic Hollgrehenn pricing (see
	/// <c>npc/cities/prontera.txt</c>).
	/// </summary>
	public readonly struct RefineCost
	{
		public RefineCost(int materialItemId, int zeny)
		{
			this.MaterialItemId = materialItemId;
			this.Zeny = zeny;
		}

		/// <summary>
		/// Item id of the ore consumed per attempt (Phracon /
		/// Emveretarcon / Oridecon / Elunium).
		/// </summary>
		public int MaterialItemId { get; }

		/// <summary>
		/// Zeny consumed per attempt regardless of success.
		/// </summary>
		public int Zeny { get; }
	}

	/// <summary>
	/// Static cost lookups for refine attempts. Era-independent today;
	/// renewal switches Hollgrehenn over to per-level bracketed costs
	/// that we'll port when the renewal NPC pass lands.
	/// </summary>
	public static class RefineCostTable
	{
		// eAthena classic ore item ids.
		public const int Phracon = 1010;
		public const int Emveretarcon = 1011;
		public const int Oridecon = 984;
		public const int Elunium = 985;

		/// <summary>
		/// Returns the per-attempt cost to refine an item from the
		/// given group at the given weapon level. Armor passes
		/// <paramref name="weaponLevel"/> 0.
		/// </summary>
		public static RefineCost Get(RefineGroup group, int weaponLevel)
		{
			if (group == RefineGroup.Armor)
				return new RefineCost(Elunium, 2000);

			return weaponLevel switch
			{
				1 => new RefineCost(Phracon, 50),
				2 => new RefineCost(Emveretarcon, 200),
				3 => new RefineCost(Oridecon, 5000),
				4 => new RefineCost(Oridecon, 20000),
				_ => new RefineCost(0, 0),
			};
		}
	}
}
