using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sabine.Shared.Const;
using Yggdrasil.Data.JSON;
using Yggdrasil.Extensions;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// One material requirement of a production recipe.
	/// </summary>
	public class ProduceMaterial
	{
		public int ItemId { get; set; }

		/// <summary>
		/// Amount consumed. <c>0</c> means the item must be present in
		/// the inventory but is not consumed (used by eAthena to model
		/// crafting "guides").
		/// </summary>
		public int Amount { get; set; }
	}

	/// <summary>
	/// A single production recipe.
	/// </summary>
	public class ProduceData
	{
		public int TargetItemId { get; set; }
		public int ItemLevel { get; set; }
		public SkillId RequiredSkillId { get; set; }
		public int RequiredSkillLevel { get; set; }
		public List<ProduceMaterial> Materials { get; set; } = new();
	}

	/// <summary>
	/// Production recipe database (<c>system/data/produce_db.txt</c>).
	/// Mirrors eAthena's <c>db/produce_db.txt</c>.
	/// </summary>
	public class ProduceDb : DatabaseJsonIndexed<int, ProduceData>
	{
		/// <summary>
		/// Returns every recipe that can be produced by the given skill at
		/// the given skill level (recipes whose required level is &lt;= the
		/// passed level). Order is the file's load order.
		/// </summary>
		public IEnumerable<ProduceData> FindBySkill(SkillId skillId, int skillLevel)
		{
			foreach (var entry in this.Entries.Values)
			{
				if (entry.RequiredSkillId == skillId && entry.RequiredSkillLevel <= skillLevel)
					yield return entry;
			}
		}

		/// <summary>
		/// Returns the recipe whose output is the given item, or null if
		/// no recipe produces it.
		/// </summary>
		public ProduceData FindByOutput(int targetItemId)
			=> this.Entries.TryGetValue(targetItemId, out var data) ? data : null;

		/// <inheritdoc/>
		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("targetItemId", "requiredSkillId", "requiredSkillLevel");

			var data = new ProduceData
			{
				TargetItemId = entry.ReadInt("targetItemId"),
				ItemLevel = entry.ReadInt("itemLevel", 1),
				RequiredSkillId = (SkillId)entry.ReadInt("requiredSkillId"),
				RequiredSkillLevel = entry.ReadInt("requiredSkillLevel", 1),
			};

			if (entry.ContainsKey("materials"))
			{
				foreach (var matEntry in entry.ForEachObject("materials"))
				{
					matEntry.AssertNotMissing("itemId", "amount");
					data.Materials.Add(new ProduceMaterial
					{
						ItemId = matEntry.ReadInt("itemId"),
						Amount = matEntry.ReadInt("amount"),
					});
				}
			}

			this.AddOrReplace(data.TargetItemId, data);
		}
	}
}
