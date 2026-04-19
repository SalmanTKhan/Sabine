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
		Ground,
		Self,
		Trap
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
	/// Flags that define a skill's fundamental type (e.g., Attack, Support).
	/// Corresponds to rAthena's 'inf'.
	/// </summary>
	[Flags]
	public enum SkillTypeFlags : ushort
	{
		Passive = 0x00,
		Attack = 0x01,
		Ground = 0x02,
		Self = 0x04,
		// 0x08 is unused
		Support = 0x10,
		Trap = 0x20,
	}

	/// <summary>
	/// Flags that define a skill's special properties.
	/// Corresponds to rAthena's 'inf2'.
	/// </summary>
	[Flags]
	public enum SkillPropertyFlags : ulong
	{
		IsQuest = 1 << 0,
		IsNpc = 1 << 1,
		IsWedding = 1 << 2,
		IsSpirit = 1 << 3,
		IsGuild = 1 << 4,
		IsSong = 1 << 5,
		IsEnsemble = 1 << 6,
		IsTrap = 1 << 7,
		TargetSelf = 1 << 8,
		NoTargetSelf = 1 << 9,
		PartyOnly = 1 << 10,
		GuildOnly = 1 << 11,
		NoTargetEnemy = 1 << 12,
		IsAutoShadowSpell = 1 << 13,
		IsChorus = 1 << 14,
		IgnoreBgReduction = 1 << 15,
		IgnoreGvgReduction = 1 << 16,
		DisableNearNpc = 1 << 17,
		TargetTrap = 1 << 18,
		IgnoreLandProtector = 1 << 19,
		AllowWhenHidden = 1 << 20,
		AllowWhenPerforming = 1 << 21,
		TargetEmperium = 1 << 22,
		IgnoreKagehumi = 1 << 23,
		AlterRangeVulture = 1 << 24,
		AlterRangeSnakeEye = 1 << 25,
		AlterRangeShadowJump = 1 << 26,
		AlterRangeRadius = 1 << 27,
		AlterRangeResearchTrap = 1 << 28,
		IgnoreHovering = 1 << 29,
		AllowOnWarg = 1 << 30,
		AllowOnMado = 1u << 31,
		TargetManhole = 1ul << 32,
		TargetHidden = 1ul << 33,
		IncreaseDanceWithWugDamage = 1ul << 34,
		IgnoreWugBite = 1ul << 35,
		IgnoreAutoGuard = 1ul << 36,
		IgnoreCicada = 1ul << 37,
		ShowScale = 1ul << 38,
		IgnoreGtb = 1ul << 39,
		Toggleable = 1ul << 40,
	}

	/// <summary>
	/// Flags that define a skill's damage properties.
	/// Corresponds to rAthena's 'nk'.
	/// </summary>
	[Flags]
	public enum SkillDamageFlags : ushort
	{
		NoDamage = 1 << 0,
		Splash = 1 << 1,
		SplashSplit = 1 << 2,
		IgnoreAtkCard = 1 << 3,
		IgnoreElement = 1 << 4,
		IgnoreDefense = 1 << 5,
		IgnoreFlee = 1 << 6,
		IgnoreDefCard = 1 << 7,
		Critical = 1 << 8,
		IgnoreLongCard = 1 << 9,
		SimpleDefense = 1 << 10,
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
		public int[] HpRate { get; set; }
		public int[] SpRate { get; set; }
		public int[] Zeny { get; set; }
		public int[] Spiritball { get; set; }
		public List<SkillItemCostData> Items { get; set; } = new();
	}

	/// <summary>
	/// Represents data related to the casting of a skill, including cast times, cooldowns, and delays after casting.
	/// </summary>
	public class SkillCastData
	{
		public int[] CastTime { get; set; }
		public int[] FixedCast { get; set; }
		public int[] Cooldown { get; set; }
		public int[] AfterCastActDelay { get; set; }
		public int[] WalkDelay { get; set; }
		public int[] Duration1 { get; set; }
		public int[] Duration2 { get; set; }
	}

	/// <summary>
	/// Represents various behavioral properties of a skill.
	/// </summary>
	public class SkillBehaviorData
	{
		public bool CastCancel { get; set; }
		public int[] SplashArea { get; set; }
		public int[] Knockback { get; set; }
	}

	/// <summary>
	/// Represents an entry in the skill database.
	/// </summary>
	public class SkillData
	{
		public SkillId Id { get; set; }
		public string SkillId { get; set; }
		public string StringId
		{
			get => this.SkillId;
			set => this.SkillId = value;
		}
		public SkillId ClassId
		{
			get => this.Id;
			set => this.Id = value;
		}
		public string Name { get; set; }
		public string KoreanName { get; set; }
		public int MaxLevel { get; set; }
		public SkillType Type { get; set; }
		public SkillTargetType TargetType { get; set; } = SkillTargetType.Passive;
		public int[] Range { get; set; }
		public SkillElement[] Element { get; set; }
		public SkillTypeFlags TypeFlags { get; set; }
		public SkillPropertyFlags PropertyFlags { get; set; }
		public SkillDamageFlags DamageFlags { get; set; }
		public SkillCostData Costs { get; set; } = new();
		public SkillBehaviorData Behavior { get; set; } = new();
		public SkillCastData Cast { get; set; } = new();

		public int GetSpCost(int level)
			=> this.GetValue(this.Costs.Sp, level);

		public int GetHpCost(int level)
			=> this.GetValue(this.Costs.Hp, level);

		public int GetRange(int level)
			=> this.GetValue(this.Range, level);

		private int GetValue(int[] values, int level)
		{
			if (values == null || values.Length == 0)
				return 0;

			return values[Math.Clamp(level - 1, 0, values.Length - 1)];
		}
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
			data.Type = entry.ReadEnum("type", SkillType.Passive);

			if (entry.ContainsKey("target"))
				data.TargetType = entry.ReadEnum<SkillTargetType>("target");

			data.TypeFlags |= data.Type switch
			{
				SkillType.Attack => SkillTypeFlags.Attack,
				SkillType.Support => SkillTypeFlags.Support,
				SkillType.Ground => SkillTypeFlags.Ground,
				SkillType.Self => SkillTypeFlags.Self,
				SkillType.Trap => SkillTypeFlags.Trap,
				_ => SkillTypeFlags.Passive
			};

			if (data.TargetType == SkillTargetType.Ground)
				data.TypeFlags |= SkillTypeFlags.Ground;

			// Simplified parsing for single values, extended to arrays to match structure
			data.Range = new[] { entry.ReadInt("range", 0) };
			var element = entry.ReadEnum("element", SkillElement.Neutral);
			data.Element = Array.ConvertAll(new int[data.MaxLevel], _ => element);

			if (entry.ContainsKey("costs"))
			{
				var costsObj = (JObject)entry["costs"];
				data.Costs.Sp = costsObj.ReadArray<int>("sp");
				data.Costs.Hp = costsObj.ReadArray<int>("hp");
				data.Costs.HpRate = costsObj.ReadArray<int>("hpRate");
				data.Costs.SpRate = costsObj.ReadArray<int>("spRate");
				data.Costs.Zeny = costsObj.ReadArray<int>("zeny");
				data.Costs.Spiritball = costsObj.ReadArray<int>("spiritball");

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
				data.Behavior.CastCancel = flagsObj.ReadBool("castCancel", true);
				if (flagsObj.ReadBool("splash", false))
					data.DamageFlags |= SkillDamageFlags.Splash;

				data.Behavior.SplashArea = new[] { flagsObj.ReadInt("splashArea", 0) };
				data.Behavior.Knockback = new[] { flagsObj.ReadInt("knockback", 0) };
			}

			if (entry.ContainsKey("cast"))
			{
				var castObj = (JObject)entry["cast"];
				data.Cast.CastTime = castObj.ReadArray<int>("castTime");
				data.Cast.FixedCast = castObj.ReadArray<int>("fixedCast");
				data.Cast.Cooldown = castObj.ReadArray<int>("cooldown");
				data.Cast.AfterCastActDelay = castObj.ReadArray<int>("afterCastActDelay");
				data.Cast.WalkDelay = castObj.ReadArray<int>("walkDelay");
				data.Cast.Duration1 = castObj.ReadArray<int>("duration1");
				data.Cast.Duration2 = castObj.ReadArray<int>("duration2");
			}

			this.AddOrReplace(data.Id, data);
		}
	}
}
