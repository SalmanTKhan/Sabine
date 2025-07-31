using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Sabine.Shared.Const;
using Yggdrasil.Data.JSON;
using Yggdrasil.Extensions;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// Defines the general category of a skill.
	/// </summary>
	public enum SkillType
	{
		Passive,
		Attack,
		Support,
	}

	/// <summary>
	/// Defines the type of target a skill can be used on.
	/// </summary>
	public enum SkillTarget
	{
		Self,
		Enemy,
		Ally,
		Ground,
	}

	/// <summary>
	/// Defines the elemental property of a skill.
	/// </summary>
	public enum SkillElement
	{
		Neutral,
		Water,
		Earth,
		Fire,
		Wind,
		Poison,
		Holy,
		Shadow,
		Ghost,
		Undead,
		Weapon,
	}

	/// <summary>
	/// Represents an item required to cast a skill.
	/// </summary>
	public class SkillItemCostData
	{
		public string Name { get; set; }
		public int Amount { get; set; }
	}

	/// <summary>
	/// Represents the resource costs to cast a skill.
	/// </summary>
	public class SkillCostData
	{
		public int[] Sp { get; set; }
		public int[] Hp { get; set; }
		public int[] Zeny { get; set; }
		public List<SkillItemCostData> Items { get; set; } = new();
	}

	/// <summary>
	/// Represents various boolean properties of a skill.
	/// </summary>
	public class SkillFlagsData
	{
		public bool CastCancel { get; set; }
		public bool Splash { get; set; }
		public int SplashArea { get; set; }
		public int Knockback { get; set; }
	}

	/// <summary>
	/// Represents an entry in the skill database.
	/// </summary>
	public class SkillData
	{
		public SkillId Id { get; set; }
		public string SkillId { get; set; }
		public string Name { get; set; }
		public string KoreanName { get; set; }
		public int MaxLevel { get; set; }
		public SkillType? Type { get; set; }
		public SkillTarget? Target { get; set; }
		public int Range { get; set; }
		public SkillElement Element { get; set; }
		public SkillCostData Costs { get; set; } = new();
		public SkillFlagsData Flags { get; set; } = new();
	}

	/// <summary>
	/// A skill database.
	/// </summary>
	public class SkillDb : DatabaseJsonIndexed<SkillId, SkillData>
	{
		/// <summary>
		/// Called to read an entry from the skill database file.
		/// </summary>
		/// <param name="entry"></param>
		protected override void ReadEntry(JObject entry)
		{
			var versionMin = entry.ReadInt("versionMin", 0);
			var versionMax = entry.ReadInt("versionMax", int.MaxValue);

			if (Game.Version < versionMin || Game.Version > versionMax)
				return;

			entry.AssertNotMissing("id", "skillId", "name", "maxLevel");

			var data = new SkillData();

			data.Id = (SkillId)entry.ReadInt("id");
			data.SkillId = entry.ReadString("skillId");
			data.Name = entry.ReadString("name");
			data.KoreanName = entry.ReadString("koreanName", null);
			data.MaxLevel = entry.ReadInt("maxLevel");

			if (entry.ContainsKey("type"))
				data.Type = entry.ReadEnum<SkillType>("type");

			if (entry.ContainsKey("target"))
				data.Target = entry.ReadEnum<SkillTarget>("target");

			data.Range = entry.ReadInt("range", 0);
			data.Element = entry.ReadEnum("element", SkillElement.Neutral);

			if (entry.ContainsKey("costs"))
			{
				var costsObj = (JObject)entry["costs"];
				data.Costs.Sp = costsObj.ReadArray<int>("sp");
				data.Costs.Hp = costsObj.ReadArray<int>("hp");
				data.Costs.Zeny = costsObj.ReadArray<int>("zeny");

				if (costsObj.ContainsKey("items"))
				{
					foreach (var itemEntry in costsObj.ForEachObject("items"))
					{
						itemEntry.AssertNotMissing("name", "amount");
						var itemCost = new SkillItemCostData
						{
							Name = itemEntry.ReadString("name"),
							Amount = itemEntry.ReadInt("amount")
						};
						data.Costs.Items.Add(itemCost);
					}
				}
			}

			if (entry.ContainsKey("flags"))
			{
				var flagsObj = (JObject)entry["flags"];
				data.Flags.CastCancel = flagsObj.ReadBool("castCancel", true);
				data.Flags.Splash = flagsObj.ReadBool("splash", false);
				data.Flags.SplashArea = flagsObj.ReadInt("splashArea", 0);
				data.Flags.Knockback = flagsObj.ReadInt("knockback", 0);
			}

			this.AddOrReplace(data.Id, data);
		}
	}
}
