using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Yggdrasil.Data.JSON;
using Yggdrasil.Extensions;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// Static template for an instance run. Loaded from
	/// <c>system/data/instances.txt</c>; resolved by name when a script
	/// calls <c>instance_create("name")</c>. The runtime instance state
	/// (id, attached map clones, owner) lives in
	/// <c>Sabine.Zone.World.Instances.Instance</c>.
	/// </summary>
	public class InstanceData
	{
		/// <summary>
		/// Lookup name passed to <c>instance_create</c>.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// String ids of every map this instance carves clones of when
		/// it spawns. The first entry is conventionally the entry map.
		/// </summary>
		public List<string> SourceMaps { get; set; } = new();

		/// <summary>
		/// How many minutes the instance runs before
		/// <see cref="Sabine.Zone.World.Instances.InstanceManager"/>
		/// auto-tears it down. Defaults to 60 (rAthena's classic
		/// one-hour cap); templates can shorten or extend.
		/// </summary>
		public int TtlMinutes { get; set; } = 60;
	}

	/// <summary>
	/// Instance template database. Indexed by template name (case-
	/// sensitive, matching the rAthena <c>db/re/instance_db.yml</c>
	/// convention).
	/// </summary>
	public class InstanceDb : DatabaseJsonIndexed<string, InstanceData>
	{
		/// <inheritdoc/>
		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("name", "sourceMaps");

			var data = new InstanceData
			{
				Name = entry.ReadString("name"),
				TtlMinutes = entry.ReadInt("ttlMinutes", 60),
			};

			if (entry["sourceMaps"] is JArray maps)
			{
				foreach (var token in maps)
					data.SourceMaps.Add(token.ToString());
			}

			this.AddOrReplace(data.Name, data);
		}
	}
}
