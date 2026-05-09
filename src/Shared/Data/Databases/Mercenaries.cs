using Newtonsoft.Json.Linq;
using Yggdrasil.Data.JSON;
using Yggdrasil.Extensions;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// Mercenary faction. Mirrors eAthena's <c>db/mercenary_db.txt</c>
	/// grouping (Archer / Sword / Spearman / Lancer); per-faction Faith
	/// counters live on <see cref="Sabine.Zone.World.Actors.Components.Characters.MercenaryComponent"/>.
	/// </summary>
	public enum MercenaryFaction
	{
		None = 0,
		Archer = 1,
		Sword = 2,
		Spearman = 3,
		Lancer = 4,
	}

	/// <summary>
	/// Static mercenary definition. Stats come from the monster table
	/// (monsters.txt) — this row only carries the contract metadata
	/// (faction, rank, contract-item, duration, faith reward).
	/// </summary>
	public class MercenaryData
	{
		public int MobId { get; set; }
		public string Name { get; set; }
		public MercenaryFaction Faction { get; set; }

		/// <summary>1..10 within a faction. Higher rank costs more faith but has stronger stats (sourced from monsters.txt).</summary>
		public int Rank { get; set; } = 1;

		/// <summary>The contract-card item the player consumes to summon. Refunded on faithful contract end.</summary>
		public int ContractItemId { get; set; }

		/// <summary>Contract duration in seconds. Default eA: 1800s (30 min).</summary>
		public int ContractSeconds { get; set; } = 1800;

		/// <summary>Faith awarded when the contract ends without the merc dying.</summary>
		public int FaithRewardOnComplete { get; set; } = 1;

		/// <summary>Faith penalty if the merc dies. Negative.</summary>
		public int FaithPenaltyOnDeath { get; set; } = -1;
	}

	/// <summary>
	/// Mercenary template database. Indexed by mob id; reverse lookup
	/// by contract item id walks the dictionary (small set, no
	/// inverted index needed).
	/// </summary>
	public class MercenaryDb : DatabaseJsonIndexed<int, MercenaryData>
	{
		/// <inheritdoc/>
		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("mobId", "name", "faction", "contractItemId");

			var data = new MercenaryData
			{
				MobId = entry.ReadInt("mobId"),
				Name = entry.ReadString("name"),
				Faction = (MercenaryFaction)System.Enum.Parse(typeof(MercenaryFaction), entry.ReadString("faction")),
				Rank = entry.ReadInt("rank", 1),
				ContractItemId = entry.ReadInt("contractItemId"),
				ContractSeconds = entry.ReadInt("contractSeconds", 1800),
				FaithRewardOnComplete = entry.ReadInt("faithRewardOnComplete", 1),
				FaithPenaltyOnDeath = entry.ReadInt("faithPenaltyOnDeath", -1),
			};

			this.AddOrReplace(data.MobId, data);
		}
	}
}
