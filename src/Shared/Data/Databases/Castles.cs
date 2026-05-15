using Newtonsoft.Json.Linq;
using Yggdrasil.Data.JSON;
using Yggdrasil.Extensions;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// Static template for one WoE castle. Sourced from
	/// <c>system/data/castles.txt</c>; mirrors eAthena's
	/// <c>db/castle_db.txt</c>. Runtime ownership state (current
	/// guild, economy/defense investment, treasure timers) lives on
	/// the matching <c>Sabine.Zone.World.Woe.Castle</c> instance,
	/// not here.
	/// </summary>
	public class CastleData
	{
		/// <summary>
		/// The castle's stable id (the index in eAthena's
		/// <c>castle_db.txt</c>). Used as the script-side
		/// <c>GetCastleData(map, 0)</c> return.
		/// </summary>
		public int Id { get; set; }

		/// <summary>String id of the castle map, e.g. <c>"aldeg_cas01"</c>.</summary>
		public string MapStringId { get; set; }

		/// <summary>Display name shown in dialogs and on guardian name tags.</summary>
		public string Name { get; set; }

		/// <summary>
		/// NPC unique-name to invoke <c>::OnGuildBreak</c> on when the
		/// castle is abandoned during a guild break. Stored verbatim;
		/// dispatch happens via <c>ScriptEvents.RaiseAsync</c>.
		/// </summary>
		public string OnGuildBreakEvent { get; set; }

		/// <summary>
		/// Monster id used for the castle's treasure boxes. Mirrors
		/// rAthena's per-castle treasure assignment (each castle gets a
		/// unique <c>mob_db</c> row so its drop pool can differ). 0 =
		/// use the shared <c>TreasureSpawnService</c> rotation across
		/// <c>TREASURE_BOX1..8</c>.
		/// </summary>
		public int TreasureBoxId { get; set; }
	}

	/// <summary>
	/// Castle template database. Indexed by map string id so the
	/// script-side <c>getcastlename(<map>)</c> / <c>getcastledata(<map>, ...)</c>
	/// helpers can resolve in one lookup.
	/// </summary>
	public class CastleDb : DatabaseJsonIndexed<string, CastleData>
	{
		/// <inheritdoc/>
		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("id", "map", "name");

			var data = new CastleData
			{
				Id = entry.ReadInt("id"),
				MapStringId = entry.ReadString("map"),
				Name = entry.ReadString("name"),
				OnGuildBreakEvent = entry.ReadString("onGuildBreak", string.Empty),
				TreasureBoxId = entry.ReadInt("treasureBoxId", 0),
			};

			this.AddOrReplace(data.MapStringId, data);
		}
	}
}
